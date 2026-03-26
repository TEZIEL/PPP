# Recipe App UI 구현 가이드 (기존 `P_Content_Recipe` 프리팹 기준)

## 1) 전체 구조 요약
- 기존 프리팹 `Assets/UI/OSApps/Recipe/P_Content_Recipe.prefab`을 베이스로 사용.
- 데이터는 `Assets/Data/Drink/Ingredients`, `Assets/Data/Drink/Drinks` 경로에서 우선 로딩.
- `DrinkListItem`은 최종적으로 아래를 표시:
  - 이미지, 이름, 카테고리, 설명, 재료, 태그, 우측 버튼
- 아르테온(`INGREDIENT_ARTHEON`)은 일반 재료 키 검사 대신 `artheon_addable`로 필터.

## 2) DrinkListItem 권장 구조
```text
DrinkListItem
 ├─ DrinkImage
 ├─ NameText
 ├─ CategoryText
 ├─ DescriptionText
 ├─ IngredientsText
 ├─ TagsText
 └─ ActionButton
```

> 기존 `New Text`가 있다면 재활용하고, 부족하면 TMP 2개만 추가해서 맞춰도 됩니다.

## 3) 하이어라키 (현재 프리팹에 맞춘 연결)
```text
P_Content_Recipe
 └─ BG_Full
    ├─ Upper
    │  └─ ButtonRow (재료 버튼 8개)
    ├─ listView (ScrollRect)
    │  └─ Viewport
    │     └─ Content (DrinkListItem 생성 위치)
    └─ BottomBar (상세 영역)
```

## 4) JSON 경로
`RecipeDataLoader` 기본값:
- `ingredientsFolderFromAssets = Data/Drink/Ingredients`
- `ingredientsFileName = ingredients.json`
- `drinksFolderFromAssets = Data/Drink/Drinks`
- `drinksFileName = drinks.json`

## 5) 인스펙터 연결 핵심
1. `P_Content_Recipe` 루트(또는 자식)에 `RecipeAppController`, `RecipeDataLoader` 부착.
2. `RecipeAppController` 연결:
   - `dataLoader` → `RecipeDataLoader`
   - `fixedIngredientButtons` → 상단 8개 `IngredientFilterButtonUI`
   - `drinkListScrollRect` → `listView`의 ScrollRect
   - `drinkListContent` → `listView/Viewport/Content`
   - `drinkListItemPrefab` → `DrinkListItem` 프리팹
   - `emptyStateText` / `detailRoot` / 상세 TMP, Image 연결
3. `DrinkListItemUI` 연결:
   - `drinkImage`, `nameText`, `categoryText`, `descriptionText`, `ingredientsText`, `tagsText`, `actionButton`

## 6) 동작 규칙
- 최대 3개 선택, 3개 선택 시 미선택 버튼 비활성화
- AND 필터 유지
- 아르테온 선택 시 `artheon_addable == true`인 음료만 통과
- 같은 음료 재클릭 시 상세 닫힘
- 필터 후 스크롤 맨 위 초기화

## 7) 테스트 체크리스트
- 재료 1/2/3개 선택 필터
- 3개 선택 후 버튼 비활성화/해제 복구
- 아르테온 단독/복합 필터
- 필터 결과 0개 문구
- 상세 토글(같은 항목 재클릭)
- 스크롤 초기화
