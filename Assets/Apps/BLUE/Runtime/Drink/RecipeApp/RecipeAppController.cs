using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 레시피 앱의 전체 UI 흐름을 담당한다.
    /// - 상단 재료 버튼 생성/선택
    /// - AND 필터로 리스트 갱신
    /// - 상세 패널 열기/닫기(같은 아이템 재클릭 토글)
    /// </summary>
    public sealed class RecipeAppController : MonoBehaviour
    {
        [Serializable]
        private struct DrinkImageEntry
        {
            public string imageKey;
            public Sprite sprite;
        }

        private const int MaxSelectedIngredients = 3;

        [Header("Data")]
        [SerializeField] private RecipeDataLoader dataLoader;

        [Header("Top Filter")]
        [Tooltip("기존 프리팹(예: P_Content_Recipe) 안의 고정 버튼들을 직접 연결할 때 사용")]
        [SerializeField] private IngredientFilterButtonUI[] fixedIngredientButtons = Array.Empty<IngredientFilterButtonUI>();
        [Tooltip("고정 버튼을 쓰지 않을 때, 런타임으로 버튼을 생성할 부모")]
        [SerializeField] private Transform ingredientButtonParent;
        [SerializeField] private IngredientFilterButtonUI ingredientButtonPrefab;

        [Header("Drink List")]
        [SerializeField] private Transform drinkListContent;
        [SerializeField] private DrinkListItemUI drinkListItemPrefab;
        [SerializeField] private TMP_Text emptyStateText;
        [SerializeField] private ScrollRect drinkScrollRect;

        [Header("Detail Panel")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image detailImage;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailIngredientsText;
        [SerializeField] private TMP_Text detailTagsText;
        [SerializeField] private TMP_Text detailDescriptionText;

        [Header("Optional Image Mapping")]
        [SerializeField] private DrinkImageEntry[] drinkImages = Array.Empty<DrinkImageEntry>();

        private readonly List<IngredientFilterButtonUI> ingredientButtons = new List<IngredientFilterButtonUI>();
        private readonly List<DrinkListItemUI> drinkItems = new List<DrinkListItemUI>();
        private readonly Dictionary<string, Sprite> imageByKey = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> selectedIngredientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private List<IngredientEntry> allIngredients = new List<IngredientEntry>();
        private List<DrinkEntry> allDrinks = new List<DrinkEntry>();
        private DrinkEntry openedDetailDrink;

        private void Awake()
        {
            BuildImageMap();
            LoadData();
            BuildIngredientButtons();
            ApplyFilterAndRebuildList();
            ShowDetail(null);
        }

        private void LoadData()
        {
            if (dataLoader == null)
            {
                Debug.LogError("[RecipeApp] dataLoader is null");
                return;
            }

            var ingredientRoot = dataLoader.LoadIngredients();
            var drinkRoot = dataLoader.LoadDrinks();

            allIngredients = ingredientRoot?.ingredients ?? new List<IngredientEntry>();
            allDrinks = drinkRoot?.drinks ?? new List<DrinkEntry>();
        }

        private void BuildImageMap()
        {
            imageByKey.Clear();
            for (int i = 0; i < drinkImages.Length; i++)
            {
                var row = drinkImages[i];
                if (string.IsNullOrWhiteSpace(row.imageKey) || row.sprite == null)
                    continue;

                imageByKey[row.imageKey] = row.sprite;
            }
        }

        private void BuildIngredientButtons()
        {
            ClearCreatedButtons();

            // 1) 기존 프리팹에 이미 버튼이 배치된 경우: 해당 버튼들을 재사용한다.
            if (fixedIngredientButtons != null && fixedIngredientButtons.Length > 0)
            {
                int bindCount = Mathf.Min(fixedIngredientButtons.Length, allIngredients.Count);
                for (int i = 0; i < bindCount; i++)
                {
                    var ui = fixedIngredientButtons[i];
                    if (ui == null)
                        continue;

                    ui.gameObject.SetActive(true);
                    ui.Setup(allIngredients[i], OnIngredientButtonClicked);
                    ingredientButtons.Add(ui);
                }

                // 남는 버튼은 숨긴다(데이터 수와 버튼 수가 다를 때 안전 처리)
                for (int i = bindCount; i < fixedIngredientButtons.Length; i++)
                {
                    if (fixedIngredientButtons[i] != null)
                        fixedIngredientButtons[i].gameObject.SetActive(false);
                }

                UpdateIngredientButtonInteractableState();
                return;
            }

            // 2) 고정 버튼이 없으면 프리팹을 런타임 생성한다.
            if (ingredientButtonParent == null || ingredientButtonPrefab == null)
                return;

            for (int i = 0; i < allIngredients.Count; i++)
            {
                var ingredient = allIngredients[i];
                var ui = Instantiate(ingredientButtonPrefab, ingredientButtonParent);
                ui.Setup(ingredient, OnIngredientButtonClicked);
                ingredientButtons.Add(ui);
            }

            UpdateIngredientButtonInteractableState();
        }

        private void OnIngredientButtonClicked(string ingredientId)
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
                return;

            if (selectedIngredientIds.Contains(ingredientId))
            {
                selectedIngredientIds.Remove(ingredientId);
            }
            else
            {
                // 최대 3개 선택 제한
                if (selectedIngredientIds.Count >= MaxSelectedIngredients)
                    return;

                selectedIngredientIds.Add(ingredientId);
            }

            SyncIngredientButtonSelectionState();
            UpdateIngredientButtonInteractableState();
            ApplyFilterAndRebuildList();
        }

        private void SyncIngredientButtonSelectionState()
        {
            for (int i = 0; i < ingredientButtons.Count; i++)
            {
                var ui = ingredientButtons[i];
                if (ui == null)
                    continue;

                ui.SetSelected(selectedIngredientIds.Contains(ui.IngredientId));
            }
        }

        private void UpdateIngredientButtonInteractableState()
        {
            bool reachedMax = selectedIngredientIds.Count >= MaxSelectedIngredients;

            for (int i = 0; i < ingredientButtons.Count; i++)
            {
                var ui = ingredientButtons[i];
                if (ui == null)
                    continue;

                bool isSelected = selectedIngredientIds.Contains(ui.IngredientId);
                bool interactable = isSelected || !reachedMax;
                ui.SetInteractable(interactable);
            }
        }

        private void ApplyFilterAndRebuildList()
        {
            ClearCreatedListItems();

            List<DrinkEntry> filtered = FilterDrinksBySelectedIngredients();

            if (drinkListContent != null && drinkListItemPrefab != null)
            {
                for (int i = 0; i < filtered.Count; i++)
                {
                    var drink = filtered[i];
                    var item = Instantiate(drinkListItemPrefab, drinkListContent);
                    item.Setup(drink, FindDrinkSprite(drink.imageKey), OnDrinkClicked);
                    drinkItems.Add(item);
                }
            }

            bool hasAny = filtered.Count > 0;
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(!hasAny);
                emptyStateText.text = "조건에 맞는 음료가 없습니다.";
            }

            // 현재 열린 상세가 필터 결과에 없으면 상세를 닫는다.
            if (openedDetailDrink != null && !filtered.Contains(openedDetailDrink))
                ShowDetail(null);

            Canvas.ForceUpdateCanvases();

            if (drinkListContent is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            if (drinkScrollRect != null)
            {
                drinkScrollRect.StopMovement();
                drinkScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private List<DrinkEntry> FilterDrinksBySelectedIngredients()
        {
            if (selectedIngredientIds.Count == 0)
                return allDrinks.ToList();

            var result = new List<DrinkEntry>();

            foreach (var drink in allDrinks)
            {
                if (drink == null)
                    continue;

                // AND 조건: 선택된 재료가 모두 포함되어야 한다.
                bool matchAll = true;
                foreach (var selectedId in selectedIngredientIds)
                {
                    if (!drink.ContainsIngredient(selectedId))
                    {
                        matchAll = false;
                        break;
                    }
                }

                if (matchAll)
                    result.Add(drink);
            }

            return result;
        }

        private void OnDrinkClicked(DrinkEntry clicked)
        {
            if (clicked == null)
                return;

            // 같은 아이템 재클릭 시 토글 닫기
            if (openedDetailDrink != null && string.Equals(openedDetailDrink.id, clicked.id, StringComparison.OrdinalIgnoreCase))
            {
                ShowDetail(null);
                return;
            }

            ShowDetail(clicked);
        }

        private void ShowDetail(DrinkEntry drink)
        {
            openedDetailDrink = drink;

            bool show = drink != null;
            if (detailRoot != null)
                detailRoot.SetActive(show);

            if (!show)
                return;

            if (detailNameText != null)
                detailNameText.text = drink.name ?? string.Empty;

            if (detailDescriptionText != null)
                detailDescriptionText.text = drink.description ?? string.Empty;

            if (detailIngredientsText != null)
                detailIngredientsText.text = string.Join(", ", drink.ingredientKeys.OrderBy(x => x));

            if (detailTagsText != null)
                detailTagsText.text = drink.tags != null ? string.Join(", ", drink.tags) : string.Empty;

            if (detailImage != null)
            {
                var sprite = FindDrinkSprite(drink.imageKey);
                detailImage.sprite = sprite;
                detailImage.enabled = sprite != null;
            }
        }

        private Sprite FindDrinkSprite(string imageKey)
        {
            if (string.IsNullOrWhiteSpace(imageKey))
                return null;

            return imageByKey.TryGetValue(imageKey, out var sprite) ? sprite : null;
        }

        private void ClearCreatedButtons()
        {
            for (int i = 0; i < ingredientButtons.Count; i++)
            {
                var button = ingredientButtons[i];
                if (button == null)
                    continue;

                // 고정 버튼 배열에 포함된 버튼은 삭제하지 않고 재사용한다.
                bool isFixed = fixedIngredientButtons != null && Array.IndexOf(fixedIngredientButtons, button) >= 0;
                if (!isFixed)
                    Destroy(button.gameObject);
            }

            ingredientButtons.Clear();
        }

        private void ClearCreatedListItems()
        {
            for (int i = 0; i < drinkItems.Count; i++)
            {
                if (drinkItems[i] != null)
                    Destroy(drinkItems[i].gameObject);
            }

            drinkItems.Clear();
        }
    }
}
