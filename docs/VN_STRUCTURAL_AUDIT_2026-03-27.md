# VN 구조적 결함 코드 감사 (2026-03-27)

대상: `VNRunner`, `VNDialogueView`, `VNBacklogManager`, `VNBacklogView`, Save/Load 경로

## 결함 요약

1. **HIGH**: `VNBacklogManager.BeginOrGetEntry`가 기존 엔트리를 재조회할 때마다 `isFinal=false`로 되돌려 최종화 보호가 깨짐.
2. **HIGH**: `FinalizeEntry`가 내부적으로 `BeginOrGetEntry`를 호출해 같은 키에 대해 상태 변경 이벤트를 최소 2회 발생시키며 비원자 상태 전이를 만듦.
3. **MEDIUM**: 로드 후 같은 `lineId`가 재방출되면 `VNDialogueView.HandleSay` dedupe에 걸려 타이핑/세이브 게이트 복구가 누락될 수 있음.
4. **CRITICAL**: External call 대기 차단 조건(`pointer == waitPointer`)이 `Call` 처리 후 즉시 깨져, `ReturnFromCall` 전에도 `NextInternal`이 진행될 수 있음.

## 상세 분석

### 1) 백로그 idempotency 붕괴 (HIGH)
- `BeginOrGetEntry`는 key가 이미 있을 때도 항상 `existing.isFinal = false`를 수행한다.
- `UpdateEntryText`는 `BeginOrGetEntry` 호출 후 `if (entry.isFinal)`을 검사하므로, 검사 시점에는 이미 `false`가 되어 보호 로직이 무력화된다.
- 결과적으로 한 번 `FinalizeEntry`된 엔트리도 늦게 도착한 partial update에 다시 덮일 수 있다.

### 2) Finalize 경로의 이중 이벤트/비원자 상태 (HIGH)
- `FinalizeEntry`가 `BeginOrGetEntry`를 먼저 호출하며, 기존 키일 때 `RaiseChanged`가 한 번 발생한다.
- 그 후 `isFinal=true` 설정 뒤 `RaiseChanged`가 다시 발생한다.
- 동일 key에 대한 finalize 1회가 UI 기준으로는 2회 갱신으로 관측되며, 중간 상태(`isFinal=false`)가 노출된다.

### 3) 로드 후 dedupe로 상태 복구 누락 (MEDIUM)
- 복원 시 `VNRunner.RestoreState -> EmitCurrent -> EmitSay -> OnSay(HandleSay)` 순으로 현재 줄을 재방출한다.
- `HandleSay`는 `lastHandledLineId == lineId`면 즉시 return한다.
- 이 경우 `currentLineBacklogKey` 재바인딩, `MarkSaveAllowed(false, "Typing Start")`, typer 콜백 연결 등이 건너뛰어진다.
- 즉, "같은 줄 재표시" 시나리오(로드 직후 동일 줄 복기)에서 UI와 러너 내부 상태 동기화가 약해진다.

### 4) External call 대기 차단 조건 결함 (CRITICAL)
- `StartExternalCall`에서 `waitPointer = pointer`로 설정하고 callStack에 frame을 push한다.
- `NextInternal`의 대기 차단은 `callStack.Count > 0 && pointer == waitPointer`일 때만 동작한다.
- 그러나 `Call` 노드 처리 직후 `pointer++` 후 return하므로, 곧바로 `pointer != waitPointer`가 된다.
- 이후 사용자가 `Next()`를 누르면 callStack이 남아 있어도 대기 차단이 해제된 상태로 본문 노드 진행 가능 경로가 열린다.

## 코드 수정 권장안

1. `BeginOrGetEntry`는 **조회 시 상태를 변형하지 않도록** 수정.
   - `existing.isFinal = false` 제거.
   - 조회 시 `RaiseChanged`도 기본적으로 하지 말고, 실제 변경이 있을 때만 호출.
2. `FinalizeEntry`/`UpdateEntryText`는 공용 조회 함수를 분리.
   - 예: `TryGetEntry` + `CreateEntryIfMissing`.
   - Finalize는 최종 상태가 이미 동일하면 no-op.
3. `HandleSay` dedupe 키를 `lineId` 단독이 아닌 `(lineId, backlogKey.CompositeKey)` 또는 러너 epoch와 결합.
   - 최소한 Load 직후에는 dedupe cache 초기화.
4. External call 차단 조건을 `callStack.Count > 0` 단독으로 강화하거나, call 대기 상태 플래그를 별도로 두어 `ReturnFromCall` 전 진행 금지.

