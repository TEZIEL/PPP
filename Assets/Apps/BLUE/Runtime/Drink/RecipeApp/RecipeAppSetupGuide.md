# Recipe App UI 구현 가이드 (Unity uGUI + TMP)

## 1) 전체 구조 요약
- 핵심 오브젝트: `RecipeAppRoot`, `TopFilterArea`, `DrinkListArea(Scroll View)`, `DetailArea`.
- 핵심 프리팹: `IngredientFilterButton`, `DrinkListItem`.
- 핵심 스크립트:
  - `RecipeAppController`: 선택/필터/리스트/상세 토글 전체 흐름.
  - `RecipeDataLoader`: `Resources`의 `ingredients.json`, `drinks.json` 로딩.
  - `IngredientFilterButtonUI`: 재료 버튼 1개 UI.
  - `DrinkListItemUI`: 리스트 아이템 1개 UI.
  - `RecipeAppModels`: JSON 모델.

## 2) 하이어라키
```text
Canvas
└─ RecipeAppRoot
   ├─ TopFilterArea
   │  ├─ HeaderText (TMP_Text)
   │  └─ FilterButtonContainer (GridLayoutGroup)
   ├─ DrinkListArea
   │  ├─ Scroll View
   │  │  ├─ Viewport (Mask + Image)
   │  │  │  └─ Content (VerticalLayoutGroup + ContentSizeFitter)
   │  └─ EmptyStateText (TMP_Text)
   └─ DetailArea
      ├─ Left
      │  ├─ DrinkImage (Image)
      │  └─ DrinkNameText (TMP_Text)
      └─ Right
         ├─ IngredientsTitle / IngredientsText
         ├─ TagsTitle / TagsText
         └─ DescriptionTitle / DescriptionText
```

## 3) JSON 로딩 권장
- **간단함 우선** 기준으로 `Resources` 사용.
- 배치 경로:
  - `Assets/Resources/DrinkData/ingredients.json`
  - `Assets/Resources/DrinkData/drinks.json`
- 로더의 기본 경로는 `DrinkData/ingredients`, `DrinkData/drinks`.

## 4) 필수 동작
- 재료는 최대 3개 선택.
- 3개 선택 시 미선택 버튼 비활성화.
- 음료 필터는 선택 재료의 **AND** 조건.
- 같은 음료 재클릭 시 상세 닫힘.
- 다른 음료 클릭 시 상세 갱신 후 열림.
- 결과 0개면 빈 상태 문구 표시.

## 5) 인스펙터 연결 핵심
1. `RecipeAppRoot`에 `RecipeAppController` 추가.
2. `RecipeAppRoot` 또는 하위에 `RecipeDataLoader` 추가.
3. `RecipeAppController` 필드 연결:
   - `dataLoader`: `RecipeDataLoader`
   - `ingredientButtonParent`: `TopFilterArea/FilterButtonContainer`
   - `ingredientButtonPrefab`: `IngredientFilterButton` 프리팹
   - `drinkListContent`: `Scroll View/Viewport/Content`
   - `drinkListItemPrefab`: `DrinkListItem` 프리팹
   - `emptyStateText`: `DrinkListArea/EmptyStateText`
   - `detailRoot`: `DetailArea`
   - `detailImage`, `detailNameText`, `detailIngredientsText`, `detailTagsText`, `detailDescriptionText`
4. 프리팹 내부의 스크립트 필드(`Button`, `TMP_Text`, `Image`)도 빠짐없이 연결.

## 6) 테스트 체크리스트
- 재료 1/2/3개 선택 각각 필터 확인.
- 3개 선택 후 미선택 버튼 비활성화 확인.
- 선택 해제 시 버튼 interactable 복구 확인.
- 필터 0개 시 EmptyStateText 표시 확인.
- 같은 음료 재클릭 시 상세 닫힘 확인.
- 다른 음료 클릭 시 상세 변경 확인.
- ScrollRect 스크롤 정상 동작 확인.
