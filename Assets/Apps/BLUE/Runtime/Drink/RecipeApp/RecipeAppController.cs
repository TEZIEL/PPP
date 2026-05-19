    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TMPro;
    using PPP.OS.Save;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

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

        [Serializable]
        private struct DefaultSpriteEntry
        {
            public string imageKey;
            public Sprite sprite;
        }

            private const int MaxSelectedIngredients = 3;
            private const string ArtheonIngredientId = "INGREDIENT_ARTHEON";
            private const string CategoryDairyKey = "CATEGORY_DAIRY";
            private const string CategoryNoneKey = "CATEGORY_NONE";
            private const string TagMilkKey = "TAG_MILK";
            private static readonly string[] SpecialTagDisplayOrder =
            {
                "TAG_SIMPLE",
                "TAG_BALANCED",
                "TAG_LIGHT",
                "TAG_STRONG",
                "TAG_STIMULATING",
                "TAG_COMPLEX",
            };
            private static readonly string[] IngredientPlusTagDisplayOrder =
            {
                "TAG_VELTRINE_PLUS",
                "TAG_ZYPHRATE_PLUS",
                "TAG_KRATYLEN_PLUS",
                "TAG_MORVION_PLUS",
                "TAG_REDULINE_PLUS",
                "TAG_CYMENTOL_PLUS",
                "TAG_BRAXIUM_PLUS",
            };
            private static readonly string[] DetailIngredientDisplayOrder =
            {
                "INGREDIENT_VELTRINE",
                "INGREDIENT_ZYPHRATE",
                "INGREDIENT_KRATYLEN",
                "INGREDIENT_MORVION",
                "INGREDIENT_REDULINE",
                "INGREDIENT_CYMENTOL",
                "INGREDIENT_BRAXIUM",
            };

            [Header("Data")]
            [SerializeField] private RecipeDataLoader dataLoader;

            [Header("Top Filter")]
            [Tooltip("기존 프리팹(예: P_Content_Recipe) 안의 고정 버튼들을 직접 연결할 때 사용")]
            [SerializeField] private IngredientFilterButtonUI[] fixedIngredientButtons = Array.Empty<IngredientFilterButtonUI>();
            [Tooltip("고정 버튼을 쓰지 않을 때, 런타임으로 버튼을 생성할 부모")]
            [SerializeField] private Transform ingredientButtonParent;
            [SerializeField] private IngredientFilterButtonUI ingredientButtonPrefab;

            [Header("Drink List")]
            [SerializeField] private ScrollRect drinkListScrollRect;
            [SerializeField] private Transform drinkListContent;
            [SerializeField] private DrinkListItemUI drinkListItemPrefab;
            [SerializeField] private TMP_Text emptyStateText;
            [SerializeField] private Button scrollUpButton;
            [SerializeField] private Button scrollDownButton;
            [SerializeField, Range(0.01f, 1f)] private float buttonScrollStep = 0.2f;
            [SerializeField] private TMP_Dropdown classificationDropdown;
            [SerializeField] private Button resetButton;


            [Header("Detail Panel")]
            [SerializeField] private GameObject detailRoot;
            [SerializeField] private Image detailImage;
            [SerializeField] private TMP_Text detailNameText;
            [SerializeField] private TMP_Text detailIngredientsText;
            [SerializeField] private TMP_Text detailTagsText;
            [SerializeField] private TMP_Text detailDescriptionText;
            [SerializeField] private TMP_Text detailCategoryText;
            [SerializeField] private TMP_Text detailArtheonText;
            [SerializeField] private Image detailModalBlockerImage;
            [SerializeField] private Button detailCloseButton;


            [Header("Default Sprite Mapping")]
            [SerializeField] private DefaultSpriteEntry[] defaultSpriteMappings;

            [Header("Locked Default Sprites")]
            [SerializeField] private Sprite bottleDefaultSprite;
            [SerializeField] private Sprite canDefaultSprite;
            [SerializeField] private Sprite packDefaultSprite;
            [SerializeField] private Sprite strawDefaultSprite;


            [Header("Optional Image Mapping")]
            [SerializeField] private DrinkImageEntry[] drinkImages = Array.Empty<DrinkImageEntry>();

            private readonly List<IngredientFilterButtonUI> ingredientButtons = new List<IngredientFilterButtonUI>();
            private readonly List<DrinkListItemUI> drinkItems = new List<DrinkListItemUI>();
            private readonly Dictionary<string, Sprite> imageByKey = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> selectedIngredientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> ingredientRawDisplayNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> ingredientDisplayNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string> ingredientColorHexById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private HashSet<string> unlockedRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            private static RecipeAppController instance;
            private static WindowManager cachedWindowManager;

            private List<IngredientEntry> allIngredients = new List<IngredientEntry>();
            private List<DrinkEntry> allDrinks = new List<DrinkEntry>();
            private DrinkEntry openedDetailDrink;
            private string selectedDrinkId;
            private string selectedClassificationKey;
            private readonly List<string> classificationOptionKeys = new List<string>();
            private readonly Dictionary<string, Sprite> defaultSpriteByKey
            = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);


            private void Awake()
            {
                instance = this;
                InitializeUnlockState();
                BindScrollButtons();
                BuildImageMap();
                BuildDefaultSpriteMap();    
                LoadData();
                BuildIngredientButtons();
                InitializeClassificationDropdown();
                BindResetButton();
                ApplyFilterAndRebuildList();
                InitializeDetailModal();
                ShowDetail(null);
            }

            private void OnDestroy()
            {
                if (instance == this)
                    instance = null;

                UnbindScrollButtons();
                UnbindResetButton();
                UnbindClassificationDropdown();
            }

            private void InitializeUnlockState()
            {
                var servedIds = ResolveServedDrinkIds();
                unlockedRecipes = new HashSet<string>(servedIds, StringComparer.OrdinalIgnoreCase);
                var osData = OSSaveSystem.Load() ?? new OSSaveData();
                if (osData.unlockedRecipeIds != null)
                {
                    for (int i = 0; i < osData.unlockedRecipeIds.Count; i++)
                        unlockedRecipes.Add(osData.unlockedRecipeIds[i]);
                }
                Debug.Log($"[RECIPE_APP] Load servedCount={unlockedRecipes.Count}");
            }

            public static void UnlockRecipeFromServe(string recipeId)
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    return;

                if (instance != null)
                {
                    instance.UnlockRecipe(recipeId);
                    return;
                }

                PersistRecipeUnlock(recipeId);
            }

            public void UnlockRecipe(string recipeId)
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    return;

                if (unlockedRecipes.Contains(recipeId))
                    return;

                unlockedRecipes.Add(recipeId);
                PersistRecipeUnlock(recipeId);
                RefreshUI();
            }

            private static void PersistRecipeUnlock(string recipeId)
            {
                var wm = ResolveWindowManager();
                if (wm != null)
                {
                    wm.MarkRecipeDrinkServed(recipeId);
                    return;
                }
                Debug.LogWarning("[RECIPE_DISCOVERY] WindowManager not found; fallback save path used.");
                var osData = OSSaveSystem.Load() ?? new OSSaveData();
                osData.osState ??= new OSGlobalStateData();
                osData.osState.recipeState ??= new RecipeAppStateData();
                osData.osState.recipeState.servedDrinkIds ??= new List<string>();
                if (osData.osState.recipeState.servedDrinkIds.Contains(recipeId))
                    return;
                osData.osState.recipeState.servedDrinkIds.Add(recipeId);
                OSSaveSystem.Save(osData);
                Debug.Log($"[RECIPE_DISCOVERY] SaveOS servedCount={osData.osState.recipeState.servedDrinkIds.Count}");
            }

            private static IReadOnlyList<string> ResolveServedDrinkIds()
            {
                var wm = ResolveWindowManager();
                if (wm != null)
                    return wm.GetServedDrinkIds();

                var osData = OSSaveSystem.Load() ?? new OSSaveData();
                osData.osState ??= new OSGlobalStateData();
                osData.osState.recipeState ??= new RecipeAppStateData();
                osData.osState.recipeState.servedDrinkIds ??= new List<string>();
                return osData.osState.recipeState.servedDrinkIds;
            }

            private static WindowManager ResolveWindowManager()
            {
                if (cachedWindowManager == null)
                    cachedWindowManager = UnityEngine.Object.FindFirstObjectByType<WindowManager>(FindObjectsInactive.Include);
                return cachedWindowManager;
            }

           
            /// <summary>
            /// 인스펙터에서 OnClick에 직접 연결할 수 있는 헬퍼.
            /// </summary>
            public void ScrollListUp()
            {
                ScrollListByStep(+1f);
            }

            /// <summary>
            /// 인스펙터에서 OnClick에 직접 연결할 수 있는 헬퍼.
            /// </summary>
            public void ScrollListDown()
            {
                ScrollListByStep(-1f);
            }

            private void BuildDefaultSpriteMap()
            {
                defaultSpriteByKey.Clear();

                foreach (var row in defaultSpriteMappings)
                {
                    if (string.IsNullOrWhiteSpace(row.imageKey) || row.sprite == null)
                        continue;

                    defaultSpriteByKey[row.imageKey] = row.sprite;
                }
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

                ingredientRawDisplayNameById.Clear();
                ingredientDisplayNameById.Clear();
                ingredientColorHexById.Clear();
                for (int i = 0; i < allIngredients.Count; i++)
                {
                    var ingredient = allIngredients[i];
                    if (ingredient == null || string.IsNullOrWhiteSpace(ingredient.id))
                        continue;

                    ingredientRawDisplayNameById[ingredient.id] = ingredient.DisplayName;
                    ingredientColorHexById[ingredient.id] = ingredient.DisplayColorHex;
                    ingredientDisplayNameById[ingredient.id] = RecipeIngredientTextFormatter.FormatIngredientDisplayName(
                        ingredient.DisplayName,
                        ingredient.DisplayColorHex);
                }
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
                CloseDetailModal();
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

                List<DrinkEntry> filtered = FilterDrinksByCurrentFilters();

                if (drinkListContent != null && drinkListItemPrefab != null)
                {
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        var drink = filtered[i];
                        var item = Instantiate(drinkListItemPrefab, drinkListContent);
                        bool unlocked = IsRecipeUnlocked(drink.id);

                        Sprite sprite = unlocked
                            ? FindDrinkSprite(drink.imageKey)
                            : GetDefaultSprite(drink.imageKey);
                        Debug.Log($"[RECIPE_APP] ApplyImage drinkId={drink.id} served={unlocked}");

                        item.Setup(drink, sprite, ingredientDisplayNameById, OnDrinkClicked);
                        drinkItems.Add(item);
                        item.SetSelected(!string.IsNullOrWhiteSpace(selectedDrinkId) && string.Equals(selectedDrinkId, drink.id, StringComparison.OrdinalIgnoreCase));
                    }
                }

                bool hasAny = filtered.Count > 0;
                if (emptyStateText != null)
                {
                    emptyStateText.gameObject.SetActive(!hasAny);
                    emptyStateText.text = "조건에 맞는 음료가 없습니다.";
                }

                // 필터 후 스크롤을 맨 위로 초기화한다.
                if (drinkListScrollRect != null)
                    drinkListScrollRect.verticalNormalizedPosition = 1f;

                // 현재 열린 상세가 필터 결과에 없으면 상세를 닫는다.
                if (openedDetailDrink != null && !filtered.Contains(openedDetailDrink))
                    CloseDetailModal();

                if (!string.IsNullOrWhiteSpace(selectedDrinkId) && !filtered.Any(x => x != null && string.Equals(x.id, selectedDrinkId, StringComparison.OrdinalIgnoreCase)))
                    selectedDrinkId = null;

                SyncDrinkItemSelectedState();
            }

            private void BindScrollButtons()
            {
                if (scrollUpButton != null)
                    scrollUpButton.onClick.AddListener(ScrollListUp);

                if (scrollDownButton != null)
                    scrollDownButton.onClick.AddListener(ScrollListDown);
            }

            private void UnbindScrollButtons()
            {
                if (scrollUpButton != null)
                    scrollUpButton.onClick.RemoveListener(ScrollListUp);

                if (scrollDownButton != null)
                    scrollDownButton.onClick.RemoveListener(ScrollListDown);
            }

            private void ScrollListByStep(float direction)
            {
                if (drinkListScrollRect == null)
                    return;

                if (drinkListScrollRect.content == null || drinkListScrollRect.viewport == null)
                    return;

                // 스크롤 가능한 길이가 없으면 이동하지 않는다.
                float contentHeight = drinkListScrollRect.content.rect.height;
                float viewportHeight = drinkListScrollRect.viewport.rect.height;
                if (contentHeight <= viewportHeight + 0.01f)
                    return;

                SoundManager.Instance.PlayOS(OSSoundEvent.Scroll);


                float step = Mathf.Clamp01(buttonScrollStep);
                float next = drinkListScrollRect.verticalNormalizedPosition + (direction * step);
                drinkListScrollRect.verticalNormalizedPosition = Mathf.Clamp01(next);
            }

            private List<DrinkEntry> FilterDrinksByCurrentFilters()
            {
                var result = new List<DrinkEntry>();

                foreach (var drink in allDrinks)
                {
                    if (drink == null)
                        continue;

                    // AND 조건: 선택된 재료가 모두 포함되어야 한다.
                    bool matchAll = true;
                    foreach (var selectedId in selectedIngredientIds)
                    {
                        bool matchedThisIngredient;

                        if (string.Equals(selectedId, ArtheonIngredientId, StringComparison.OrdinalIgnoreCase))
                        {
                            // 아르테온은 ingredients 키가 아니라 artheon_addable 플래그로 판정
                            matchedThisIngredient = drink.artheon_addable;
                        }
                        else
                        {
                            matchedThisIngredient = drink.ContainsIngredient(selectedId);
                        }

                        if (!matchedThisIngredient)
                        {
                            matchAll = false;
                            break;
                        }
                    }

                    if (!matchAll)
                        continue;

                    if (!PassesClassificationFilter(drink))
                        continue;

                    result.Add(drink);
                }

                return result;
            }

            private bool PassesClassificationFilter(DrinkEntry drink)
            {
                if (drink == null)
                    return false;

                if (string.IsNullOrWhiteSpace(selectedClassificationKey))
                    return true;

                if (string.Equals(drink.category, selectedClassificationKey, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(selectedClassificationKey, CategoryDairyKey, StringComparison.Ordinal))
                {
                    if (drink.tags != null)
                    {
                        for (int i = 0; i < drink.tags.Count; i++)
                        {
                            if (string.Equals(drink.tags[i], TagMilkKey, StringComparison.Ordinal))
                                return true;
                        }
                    }
                }

                if (drink.tags == null)
                    return false;

                for (int i = 0; i < drink.tags.Count; i++)
                {
                    if (string.Equals(drink.tags[i], selectedClassificationKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            private void OnDrinkClicked(DrinkEntry clicked)
            {
                if (clicked == null)
                    return;

                ShowDetail(clicked);
            }


            private void InitializeDetailModal()
            {
                if (detailModalBlockerImage == null)
                    detailModalBlockerImage = EnsureModalBlocker();

                if (detailCloseButton == null && detailRoot != null)
                    detailCloseButton = detailRoot.GetComponentInChildren<Button>(true);

                if (detailCloseButton == null && detailRoot != null)
                    detailCloseButton = CreateRuntimeCloseButton(detailRoot.transform);

                if (detailCloseButton != null)
                {
                    detailCloseButton.onClick.RemoveListener(CloseDetailModal);
                    detailCloseButton.onClick.AddListener(CloseDetailModal);
                }

                SetModalBlockerVisible(false);
            }

            private Button CreateRuntimeCloseButton(Transform parent)
            {
                var go = new GameObject("DetailCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-12f, -12f);
                rect.sizeDelta = new Vector2(84f, 34f);

                var image = go.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.9f);

                var label = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
                label.transform.SetParent(go.transform, false);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var text = label.GetComponent<TMP_Text>();
                text.text = "Close";
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 18;
                text.color = Color.black;

                return go.GetComponent<Button>();
            }

            public void CloseDetailModal()
            {
                openedDetailDrink = null;
                if (detailRoot != null)
                    detailRoot.SetActive(false);
                SetModalBlockerVisible(false);
                selectedDrinkId = null;
                SyncDrinkItemSelectedState();
            }


            private Image EnsureModalBlocker()
            {
                if (detailRoot == null)
                    return null;

                var rootRect = GetComponent<RectTransform>();
                if (rootRect == null)
                    return null;

                var blockerGo = new GameObject("DetailModalBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blockerGo.transform.SetParent(transform, false);
                var rect = blockerGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = blockerGo.GetComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.08f);
                image.raycastTarget = true;
                blockerGo.SetActive(false);
                return image;
            }

            private void SetModalBlockerVisible(bool visible)
            {
                if (detailModalBlockerImage == null)
                    return;

                var go = detailModalBlockerImage.gameObject;
                go.SetActive(visible);
                if (visible)
                    go.transform.SetAsLastSibling();
            }



           

            private void InitializeClassificationDropdown()
            {
                if (classificationDropdown == null)
                {
                    classificationDropdown = GetComponentsInChildren<TMP_Dropdown>(true)
                        .FirstOrDefault(d => d != null && d.name.IndexOf("AmbientDropdownList", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                classificationOptionKeys.Clear();
                classificationOptionKeys.Add(string.Empty);

                var categories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                var normalTags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                var ingredientPlusTags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                var specialTags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < allDrinks.Count; i++)
                {
                    var drink = allDrinks[i];
                    if (drink == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(drink.category))
                    {
                        var category = drink.category.Trim();
                        if (!IsHiddenClassificationKey(category))
                            categories.Add(category);
                    }

                    if (drink.tags == null)
                        continue;

                    for (int t = 0; t < drink.tags.Count; t++)
                    {
                        var tag = drink.tags[t];
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            string normalized = tag.Trim();
                            if (IsHiddenClassificationKey(normalized))
                                continue;

                            if (IsSpecialTagKey(normalized))
                                specialTags.Add(normalized);
                            else if (IsIngredientPlusTagKey(normalized))
                                ingredientPlusTags.Add(normalized);
                            else
                                normalTags.Add(normalized);
                        }
                    }
                }

                foreach (var category in categories)
                    classificationOptionKeys.Add(category);
                foreach (var tag in normalTags)
                    classificationOptionKeys.Add(tag);
                foreach (var tag in GetOrderedIngredientPlusTags(ingredientPlusTags))
                    classificationOptionKeys.Add(tag);
                foreach (var tag in GetOrderedSpecialTags(specialTags))
                    classificationOptionKeys.Add(tag);

                var options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("전체") };
                for (int i = 1; i < classificationOptionKeys.Count; i++)
                    options.Add(new TMP_Dropdown.OptionData(ToClassificationDisplayName(classificationOptionKeys[i])));

                classificationDropdown.ClearOptions();
                classificationDropdown.AddOptions(options);
                classificationDropdown.SetValueWithoutNotify(0);
                selectedClassificationKey = string.Empty;
                classificationDropdown.onValueChanged.AddListener(OnClassificationDropdownChanged);
            }
        

            private void UnbindClassificationDropdown()
            {
                if (classificationDropdown != null)
                    classificationDropdown.onValueChanged.RemoveListener(OnClassificationDropdownChanged);
            }

            private void OnClassificationDropdownChanged(int index)
            {
                if (classificationOptionKeys.Count == 0)
                    return;

                int clamped = Mathf.Clamp(index, 0, classificationOptionKeys.Count - 1);
                selectedClassificationKey = classificationOptionKeys[clamped];
                CloseDetailModal();
                ApplyFilterAndRebuildList();
            }

            private static string ToClassificationDisplayName(string rawKey)
            {
                if (string.IsNullOrWhiteSpace(rawKey))
                    return "전체";

                return rawKey.Replace("CATEGORY_", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Replace("TAG_", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Replace('_', ' ')
                             .Trim();
            }

            private static bool IsHiddenClassificationKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return true;

                return string.Equals(key, TagMilkKey, StringComparison.Ordinal)
                    || string.Equals(key, CategoryNoneKey, StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsHiddenClassificationDisplayKey(string key)
            {
                if (IsHiddenClassificationKey(key))
                    return true;

                return key.EndsWith("_NONE", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsSpecialTagKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                for (int i = 0; i < SpecialTagDisplayOrder.Length; i++)
                {
                    if (string.Equals(key, SpecialTagDisplayOrder[i], StringComparison.Ordinal))
                        return true;
                }

                return false;
            }

            private static bool IsIngredientPlusTagKey(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                return key.StartsWith("TAG_", StringComparison.OrdinalIgnoreCase)
                    && key.EndsWith("_PLUS", StringComparison.OrdinalIgnoreCase);
            }

            private static IEnumerable<string> GetOrderedIngredientPlusTags(SortedSet<string> ingredientPlusTags)
            {
                for (int i = 0; i < IngredientPlusTagDisplayOrder.Length; i++)
                {
                    if (ingredientPlusTags.Contains(IngredientPlusTagDisplayOrder[i]))
                        yield return IngredientPlusTagDisplayOrder[i];
                }

                foreach (var key in ingredientPlusTags)
                {
                    if (!IngredientPlusTagDisplayOrder.Contains(key, StringComparer.OrdinalIgnoreCase))
                        yield return key;
                }
            }

            private static IEnumerable<string> GetOrderedSpecialTags(SortedSet<string> specialTags)
            {
                for (int i = 0; i < SpecialTagDisplayOrder.Length; i++)
                {
                    if (specialTags.Contains(SpecialTagDisplayOrder[i]))
                        yield return SpecialTagDisplayOrder[i];
                }

                foreach (var key in specialTags)
                {
                    if (!IsSpecialTagKey(key))
                        yield return key;
                }
            }

            private static int GetClassificationSortGroup(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return int.MaxValue;
                if (key.StartsWith("CATEGORY_", StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (IsIngredientPlusTagKey(key))
                    return 2;
                if (IsSpecialTagKey(key))
                    return 3;
                return 1;
            }

            private static int GetClassificationSortOrder(string key)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return int.MaxValue;

                if (key.StartsWith("CATEGORY_", StringComparison.OrdinalIgnoreCase))
                    return 0;

                for (int i = 0; i < IngredientPlusTagDisplayOrder.Length; i++)
                {
                    if (string.Equals(key, IngredientPlusTagDisplayOrder[i], StringComparison.OrdinalIgnoreCase))
                        return i;
                }

                for (int i = 0; i < SpecialTagDisplayOrder.Length; i++)
                {
                    if (string.Equals(key, SpecialTagDisplayOrder[i], StringComparison.Ordinal))
                        return i;
                }

                return int.MaxValue;
            }

            private void BindResetButton()
            {
                if (resetButton == null)
                {
                    resetButton = GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(b => b != null && b.name.IndexOf("RecipeReset", StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (resetButton == null)
                {
                    Debug.LogWarning("[RecipeApp] resetButton is not assigned.");
                    return;
                }

                resetButton.onClick.AddListener(HandleResetFilters);
            }

            private void UnbindResetButton()
            {
                if (resetButton != null)
                    resetButton.onClick.RemoveListener(HandleResetFilters);
            }

            private void HandleResetFilters()
            {
                selectedIngredientIds.Clear();
                selectedClassificationKey = string.Empty;
                selectedDrinkId = null;

                if (classificationDropdown != null)
                    classificationDropdown.SetValueWithoutNotify(0);

                SyncIngredientButtonSelectionState();
                UpdateIngredientButtonInteractableState();
                CloseDetailModal();
                ApplyFilterAndRebuildList();
            }

            private void ShowDetail(DrinkEntry drink)
            {
                openedDetailDrink = drink;
                selectedDrinkId = drink?.id;
                SyncDrinkItemSelectedState();

                bool show = drink != null;
                if (detailRoot != null)
                {
                    detailRoot.SetActive(show);
                    if (show)
                        detailRoot.transform.SetAsLastSibling();
                }

                SetModalBlockerVisible(show);

                if (detailRoot != null && show)
                    detailRoot.transform.SetAsLastSibling();

                if (!show)
                    return;

                if (detailNameText != null)
                    detailNameText.text = drink.name ?? string.Empty;

                if (detailDescriptionText != null)
                    detailDescriptionText.text = drink.description ?? string.Empty;

                if (detailIngredientsText != null)
                {
                    detailIngredientsText.richText = true;
                    detailIngredientsText.alignment = TextAlignmentOptions.Center;
                    detailIngredientsText.text = BuildIngredientAndArtheonDetailText(drink);
                }

                if (detailTagsText != null)
                    detailTagsText.text = BuildTagAndCategoryDetailText(drink);

                if (detailArtheonText != null)
                    detailArtheonText.gameObject.SetActive(false);

                if (detailCategoryText != null)
                    detailCategoryText.gameObject.SetActive(false);

                if (detailImage != null)
                {
                    bool unlocked = IsRecipeUnlocked(drink.id);

                    var sprite = unlocked
                        ? FindDrinkSprite(drink.imageKey)
                          : GetDefaultSprite(drink.imageKey);

                detailImage.sprite = sprite;
                    detailImage.enabled = sprite != null;

                }
            }

            private Sprite GetDefaultSprite(string imageKey)
            {
                if (string.IsNullOrWhiteSpace(imageKey))
                    return null;

                return defaultSpriteByKey.TryGetValue(imageKey, out var sprite)
                    ? sprite
                    : null;
            }

            private bool IsRecipeUnlocked(string recipeId)
                {
                    if (string.IsNullOrWhiteSpace(recipeId))
                    return false;

                return unlockedRecipes.Contains(recipeId);
            }

            private void RefreshUI()
            {
                for (int i = 0; i < drinkItems.Count; i++)
                {
                    var item = drinkItems[i];
                    if (item == null)
                        continue;

                    var drink = allDrinks.Find(x => x != null && string.Equals(x.id, item.DrinkId, StringComparison.OrdinalIgnoreCase));
                    bool unlocked = IsRecipeUnlocked(item.DrinkId);
                    Sprite sprite = unlocked
                        ? FindDrinkSprite(drink?.imageKey)
                        : GetDefaultSprite(drink?.imageKey);
                    item.Setup(drink, sprite, ingredientDisplayNameById, OnDrinkClicked);
                    item.SetSelected(!string.IsNullOrWhiteSpace(selectedDrinkId) && string.Equals(selectedDrinkId, item.DrinkId, StringComparison.OrdinalIgnoreCase));
                    Debug.Log($"[RECIPE_APP] ApplyImage drinkId={item.DrinkId} served={unlocked}");
                }

                if (openedDetailDrink != null)
                    ShowDetail(openedDetailDrink);
            }


            private void SyncDrinkItemSelectedState()
            {
                for (int i = 0; i < drinkItems.Count; i++)
                {
                    var item = drinkItems[i];
                    if (item == null)
                        continue;

                    bool selected = !string.IsNullOrWhiteSpace(selectedDrinkId)
                        && string.Equals(item.DrinkId, selectedDrinkId, StringComparison.OrdinalIgnoreCase);
                    item.SetSelected(selected);
                }
            }

            private Sprite FindDrinkSprite(string imageKey)
            {
                if (string.IsNullOrWhiteSpace(imageKey))
                    return null;

                return imageByKey.TryGetValue(imageKey, out var sprite) ? sprite : null;
            }

            private string BuildIngredientAndArtheonDetailText(DrinkEntry drink)
            {
                if (drink == null)
                    return string.Empty;

                var lines = new List<string>();
                foreach (var pair in GetOrderedIngredientPairs(drink))
                {
                    string name = GetIngredientDisplayName(pair.Key);
                    string colorHex = GetIngredientColorHex(pair.Key);
                    string coloredName = RecipeIngredientTextFormatter.FormatIngredientDisplayName(name, colorHex);
                    lines.Add($"{coloredName} × {pair.Value}");
                }

                if (drink.artheon_addable)
                    lines.Add("아르테온 추가 가능");

                return string.Join("\n", lines);
            }

            private IEnumerable<KeyValuePair<string, int>> GetOrderedIngredientPairs(DrinkEntry drink)
            {
                if (drink?.ingredientAmounts == null || drink.ingredientAmounts.Count == 0)
                    yield break;

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < DetailIngredientDisplayOrder.Length; i++)
                {
                    var ingredientId = DetailIngredientDisplayOrder[i];
                    if (!drink.ingredientAmounts.TryGetValue(ingredientId, out int amount) || amount <= 0)
                        continue;

                    seen.Add(ingredientId);
                    yield return new KeyValuePair<string, int>(ingredientId, amount);
                }

                var fallback = drink.ingredientAmounts
                    .Where(x => x.Value > 0 && !seen.Contains(x.Key))
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in fallback)
                    yield return pair;
            }

            private string BuildTagAndCategoryDetailText(DrinkEntry drink)
            {
                if (drink == null)
                    return "Tag : ";

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(drink.category))
                {
                    var category = drink.category.Trim();
                    if (!IsHiddenClassificationDisplayKey(category))
                        keys.Add(category);
                }

                if (drink.tags != null)
                {
                    for (int i = 0; i < drink.tags.Count; i++)
                    {
                        var raw = drink.tags[i];
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;

                        var tag = raw.Trim();
                        if (string.Equals(tag, TagMilkKey, StringComparison.Ordinal))
                        {
                            keys.Add(CategoryDairyKey);
                            continue;
                        }

                        if (IsHiddenClassificationDisplayKey(tag))
                            continue;

                        keys.Add(tag);
                    }
                }

                var ordered = keys
                    .OrderBy(GetClassificationSortGroup)
                    .ThenBy(GetClassificationSortOrder)
                    .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .Select(ToClassificationDisplayName)
                    .ToArray();

                return ordered.Length == 0
                    ? "Tag : "
                    : $"Tag : {string.Join(", ", ordered)}";
            }

            private string GetIngredientDisplayName(string ingredientId)
            {
                if (string.IsNullOrWhiteSpace(ingredientId))
                    return string.Empty;

                if (ingredientRawDisplayNameById.TryGetValue(ingredientId, out var displayName) && !string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                Debug.LogWarning($"[RecipeApp] ingredient display name missing: {ingredientId}");
                return ingredientId;
            }

            private string GetIngredientColorHex(string ingredientId)
            {
                if (string.IsNullOrWhiteSpace(ingredientId))
                    return string.Empty;

                if (ingredientColorHexById.TryGetValue(ingredientId, out var colorHex))
                    return colorHex ?? string.Empty;

                Debug.LogWarning($"[RecipeApp] ingredient color missing: {ingredientId}");
                return string.Empty;
            }


        private Sprite GetDefaultSpriteByImageKey(string imageKey)
        {
            if (string.IsNullOrWhiteSpace(imageKey))
                return null;

            imageKey = imageKey.ToLower();

            if (imageKey.Contains("bottle"))
                return bottleDefaultSprite;

            if (imageKey.Contains("can"))
                return canDefaultSprite;

            if (imageKey.Contains("pack"))
                return packDefaultSprite;

            if (imageKey.Contains("straw"))
                return strawDefaultSprite;

            return null;
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
