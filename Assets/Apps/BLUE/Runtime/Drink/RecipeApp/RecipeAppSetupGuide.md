# Recipe App UI 구현 가이드 (기존 `P_Content_Recipe` 프리팹 기준)

## 1) 전체 구조 요약
- 기존 프리팹 `Assets/UI/OSApps/Recipe/P_Content_Recipe.prefab`을 베이스로 사용.
- 데이터는 **Resources가 아니라 실제 파일 경로**(`Assets/Data/Drink/Ingredients`, `Assets/Data/Drink/Drinks`)에서 우선 로딩.
- 스크립트 구성:
  - `RecipeAppController`: 필터/리스트/상세 토글 전체 제어
  - `RecipeDataLoader`: 지정된 Assets 폴더의 JSON 로딩
  - `IngredientFilterButtonUI`: 상단 필터 버튼 1개
  - `DrinkListItemUI`: 리스트 아이템 1개
  - `RecipeAppModels`: JSON 모델

## 2) 하이어라키 (현재 프리팹에 맞춘 연결)
```text
P_Content_Recipe
 └─ BG_Full
    ├─ Upper
    │  └─ ButtonRow (여기에 재료 버튼 8개 배치/연결)
    ├─ listView (ScrollRect)
    │  └─ Viewport
    │     └─ Content (DrinkListItem 생성 위치)
    └─ BottomBar (상세 영역 루트로 사용 권장)
       ├─ (좌) DrinkImage, DrinkName
       └─ (우) Ingredients/Tags/Description Text
```

## 3) JSON 경로
`RecipeDataLoader` 기본값:
- `ingredientsFolderFromAssets = Data/Drink/Ingredients`
- `ingredientsFileName = ingredients.json`
- `drinksFolderFromAssets = Data/Drink/Drinks`
- `drinksFileName = drinks.json`

즉 실제 파일은 아래 경로를 읽습니다.
- `Assets/Data/Drink/Ingredients/ingredients.json`
- `Assets/Data/Drink/Drinks/drinks.json`

## 4) 프리팹/오브젝트 연결 핵심
1. `P_Content_Recipe` 루트(또는 자식 컨트롤러 오브젝트)에 `RecipeAppController` 부착.
2. 같은 위치에 `RecipeDataLoader` 부착.
3. `RecipeAppController` 연결:
   - `dataLoader` → `RecipeDataLoader`
   - `fixedIngredientButtons` → 상단 8개 버튼의 `IngredientFilterButtonUI` 컴포넌트들
   - `drinkListContent` → `listView/Viewport/Content`
   - `drinkListItemPrefab` → `DrinkListItem` 프리팹
   - `emptyStateText` → 빈 상태 메시지 TMP
   - `detailRoot` → 상세 패널 루트(`BottomBar` 권장)
   - `detailImage`, `detailNameText`, `detailIngredientsText`, `detailTagsText`, `detailDescriptionText` 연결
4. `IngredientFilterButtonUI` 각 버튼에 연결:
   - `button`: 해당 버튼 컴포넌트
   - `labelText`: 버튼 내부 `Text (TMP)`
   - `selectionBackground`: 버튼 배경 Image

## 5) 동작 규칙
- 최대 3개 선택, 3개 선택 시 나머지 버튼 비활성화.
- 선택 재료 기준 AND 필터.
- 같은 음료 재클릭 시 상세 닫힘.
- 다른 음료 클릭 시 상세 교체.
- 결과 0개면 “조건에 맞는 음료가 없습니다.” 출력.

## 6) 테스트 체크리스트
- 재료 1/2/3개 선택 시 결과 확인
- 3개 선택 후 미선택 버튼 비활성화 확인
- 선택 해제 시 버튼 복구 확인
- 필터 결과 0개 문구 확인
- 같은 음료 재클릭 상세 닫힘 확인
- 다른 음료 클릭 상세 갱신 확인
- 리스트 스크롤 확인
