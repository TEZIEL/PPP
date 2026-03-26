# Recipe App UI 표시 규칙 가이드 (`P_Content_Recipe` 기준)

## 핵심 변경
- 리스트 아이템 표시 규칙:
  - 접두사 제거(재료:/설명:/태그:)
  - 카테고리 + 태그 + 파생 태그 + 아르테온 가능 여부를 메타 한 줄로 통합
- 로컬라이징 친화 구조:
  - `drinks.json`의 `category`, `tags` raw key를 그대로 사용
  - `DrinkListItemUI.localizedEntries`(키-값 테이블)에서 표시 언어를 해석
  - 언어 변경 시 코드 수정 대신 테이블 교체/수정
- 줄바꿈:
  - 강제 줄바꿈 로직 없이 TMP 기본 줄바꿈 사용
  - 마우스 드래그 스크롤은 막고, 휠 + 버튼 스크롤 사용

## DrinkListItem 표시 필드
- 이름: 음료명
- 재료: `벨트린 x5, 레듈린 x4, ...`
- 설명: 원문 설명 본문
- 메타: `주스, 벨트린+, 복잡함, 강렬함, 아르테온 추가 가능`

## 메타 생성 순서
1. category key 로컬라이즈
2. tag key 로컬라이즈(+ 숨김 태그 제외)
3. 파생 태그 계산 후 로컬라이즈
4. `artheon_addable` true면 메타 문구 추가
5. 중복 제거 후 join

## 파생 태그 기준
- 자극적: 수량 5 이상 재료 2개 이상
- 무난함: 수량 5 이상 재료 0개
- 단순함: 재료 종류 3개
- 복잡함: 재료 종류 5개 이상
- 가벼움: 총합 11 이하
- 강렬함: 총합 15 이상

## 인스펙터 연결 포인트
- `DrinkListItemUI.localizedEntries`:
  - 예) `CATEGORY_JUICE -> 주스`
  - 예) `TAG_VELTRINE_PLUS -> 벨트린+`
  - 예) `DERIVED_COMPLEX -> 복잡함`
  - 예) `META_ARTHEON_ADDABLE -> 아르테온 추가 가능`
- `DrinkListItemUI.hiddenRawTagKeys`:
  - 내부 태그(`TAG_SIMPLE`, `TAG_COMPLEX` 등) 숨김 키 설정
- Scroll View의 `ScrollRect` 컴포넌트:
  - `WheelOnlyScrollRect`로 교체해 드래그 스크롤 비활성화
  - 휠 스크롤은 그대로 사용
- `RecipeAppController`:
  - `scrollUpButton`, `scrollDownButton`에 위/아래 이동 버튼 연결
  - `buttonScrollStep`으로 버튼 1회 이동량 조절
