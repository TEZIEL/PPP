using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 스크롤 리스트의 음료 아이템 1개를 담당.
    /// - drinks.json 의 category/tags key를 기반으로 메타 문자열 생성
    /// - 로컬라이징 키-값 테이블(인스펙터) 기반으로 표시 텍스트 변환
    /// - 줄바꿈은 TMP 기본 동작 사용
    /// </summary>
    public sealed class DrinkListItemUI : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        [Serializable]
        private struct LocalizedEntry
        {
            public string key;
            public string value;
        }

        [SerializeField] private Image drinkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text categoryText;     // 통합 메타 라인
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ingredientsText;
        [SerializeField] private TMP_Text tagsText;         // 호환용(현재는 미사용)
        [SerializeField] private Button actionButton;
        [SerializeField] private BlueprintListItemThemeApplier themeApplier;
        [SerializeField] private Image backgroundImage;

        [Header("Visual State Colors")]
        [SerializeField] private Color defaultBackgroundColor = new Color32(221, 234, 232, 255);
        [SerializeField] private Color hoverBackgroundColor = new Color32(112, 140, 158, 255);
        [SerializeField] private Color selectedBackgroundColor = new Color32(81, 108, 132, 255);
        [SerializeField] private Color defaultTextColor = Color.black;
        [SerializeField] private Color activeTextColor = Color.white;

        [Header("Localization (Inspector)")]
        [SerializeField] private LocalizedEntry[] localizedEntries = Array.Empty<LocalizedEntry>();
        [SerializeField] private string[] hiddenRawTagKeys = { "TAG_SIMPLE", "TAG_COMPLEX", "TAG_LIGHT", "TAG_STRONG", "TAG_STIMULATING", "TAG_BALANCED", "TAG_VELTRINE_NONE", "TAG_BELTRINE_NONE", "TAG_NONE" };

        private const string DerivedStimulating = "DERIVED_STIMULATING";
        private const string DerivedBalanced = "DERIVED_BALANCED";
        private const string DerivedSimple = "DERIVED_SIMPLE";
        private const string DerivedComplex = "DERIVED_COMPLEX";
        private const string DerivedLight = "DERIVED_LIGHT";
        private const string DerivedStrong = "DERIVED_STRONG";
        private const string MetaArtheonAddable = "META_ARTHEON_ADDABLE";

        private readonly Dictionary<string, string> localizedByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> hiddenTagKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private DrinkEntry current;
        private Action<DrinkEntry> onClicked;
        private Sprite currentSprite;
        private bool isHovered;
        private bool isSelected;

        public string DrinkId => current != null ? current.id : string.Empty;

        private void Awake()
        {

            if (ingredientsText != null)
                ingredientsText.richText = true;

            if (themeApplier == null)
                themeApplier = GetComponent<BlueprintListItemThemeApplier>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (backgroundImage == null)
                Debug.LogWarning($"[DrinkListItemUI] backgroundImage is not assigned on {name}. Hover/selected background color will be skipped.");

            RebuildLocalizationTable();
            ApplyVisualState();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLocalizationTable();
        }
#endif

        public void Setup(
            DrinkEntry drink,
            Sprite image,
            IReadOnlyDictionary<string, string> ingredientDisplayNames,
            Action<DrinkEntry> clickHandler)
        {
            current = drink;
            onClicked = clickHandler;

            SetText(nameText, drink?.name ?? "(이름 없음)");
            SetText(ingredientsText, BuildIngredientsText(drink, ingredientDisplayNames));
            SetText(descriptionText, BuildDescriptionText(drink));
            SetText(categoryText, BuildMetaText(drink));

            if (tagsText != null)
                tagsText.text = string.Empty;

            if (drinkImage != null)
            {
                currentSprite = image;
                ApplyUnlockVisual(image != null);
            }

            isHovered = false;

            themeApplier?.ApplyCurrentTheme();
            ApplyVisualState();
        }


        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ApplyVisualState();
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isHovered = true;
            ApplyVisualState();
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isHovered = false;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            bool active = isSelected || isHovered;

            if (backgroundImage != null)
            {
                if (isSelected)
                    backgroundImage.color = selectedBackgroundColor;
                else if (isHovered)
                    backgroundImage.color = hoverBackgroundColor;
                else
                    backgroundImage.color = defaultBackgroundColor;
            }

            var textColor = active ? activeTextColor : defaultTextColor;
            if (nameText != null)
                nameText.color = textColor;
            if (descriptionText != null)
                descriptionText.color = textColor;
        }

        public void ApplyUnlockVisual(bool unlocked)
        {
            if (drinkImage == null)
                return;

            drinkImage.enabled = unlocked;
            drinkImage.sprite = unlocked ? currentSprite : null;
        }

        private string BuildIngredientsText(DrinkEntry drink, IReadOnlyDictionary<string, string> ingredientDisplayNames)
        {
            if (drink == null || drink.ingredientAmounts == null || drink.ingredientAmounts.Count == 0)
                return string.Empty;

            var ordered = drink.ingredientAmounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
            var parts = new List<string>();

            foreach (var pair in ordered)
            {
                string displayName = pair.Key;
                if (ingredientDisplayNames != null && ingredientDisplayNames.TryGetValue(pair.Key, out var mappedName) && !string.IsNullOrWhiteSpace(mappedName))
                    displayName = mappedName;

                parts.Add($"{displayName} x{pair.Value}");
            }

            return string.Join(", ", parts);
        }

        private static string BuildDescriptionText(DrinkEntry drink)
        {
            if (drink == null || string.IsNullOrWhiteSpace(drink.description))
                return string.Empty;

            return drink.description.Trim();
        }

        private string BuildMetaText(DrinkEntry drink)
        {
            if (drink == null)
                return string.Empty;

            var tokens = new List<string>();

            // 1) category key -> localized
            string categoryText = LocalizeCategory(drink.category);
            if (!string.IsNullOrWhiteSpace(categoryText))
                tokens.Add(categoryText);

            // 2) raw tag key -> localized
            if (drink.tags != null)
            {
                for (int i = 0; i < drink.tags.Count; i++)
                {
                    string translated = LocalizeTag(drink.tags[i]);
                    if (!string.IsNullOrWhiteSpace(translated))
                        tokens.Add(translated);
                }
            }

            // 3) derived tags (key 기반으로 계산 후 localized)
            foreach (string derivedKey in GetDerivedTagKeys(drink))
            {
                string localized = LocalizeKey(derivedKey);
                if (!string.IsNullOrWhiteSpace(localized))
                    tokens.Add(localized);
            }

            // 4) artheon 가능 여부
            if (drink.artheon_addable)
                tokens.Add(LocalizeKey(MetaArtheonAddable));

            // 5) dedupe + cleanup
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i]?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (!seen.Add(token))
                    continue;

                result.Add(token);
            }

            return string.Join(", ", result);
        }

        private string LocalizeCategory(string rawCategory)
        {
            if (string.IsNullOrWhiteSpace(rawCategory))
                return string.Empty;

            if (rawCategory.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
                return string.Empty;

            return LocalizeKey(rawCategory);
        }

        private string LocalizeTag(string rawTag)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
                return string.Empty;

            if (hiddenTagKeys.Contains(rawTag))
                return string.Empty;

            if (rawTag.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
                return string.Empty;

            // 직접 매핑(인스펙터/기본 fallback 사전에 실제 rawTag 키가 있을 때만 사용)
            if (localizedByKey.TryGetValue(rawTag, out var direct) && !string.IsNullOrWhiteSpace(direct))
                return direct;

            // TAG_xxx_PLUS -> 재료명+
            if (rawTag.EndsWith("_PLUS", StringComparison.OrdinalIgnoreCase))
            {
                string token = rawTag
                    .Replace("TAG_", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("_PLUS", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string ingredientKey = $"INGREDIENT_{token}";
                string ingredientName = LocalizeKey(ingredientKey);
                if (!string.IsNullOrWhiteSpace(ingredientName) && !ingredientName.Equals(ingredientKey, StringComparison.OrdinalIgnoreCase))
                    return ingredientName + "+";
            }

            return string.Empty;
        }

        private IEnumerable<string> GetDerivedTagKeys(DrinkEntry drink)
        {
            if (drink == null || drink.ingredientAmounts == null || drink.ingredientAmounts.Count == 0)
                yield break;

            int ingredientKindCount = drink.ingredientAmounts.Count;
            int totalAmount = 0;
            int atLeastFiveCount = 0;

            foreach (var pair in drink.ingredientAmounts)
            {
                totalAmount += pair.Value;
                if (pair.Value >= 5)
                    atLeastFiveCount++;
            }

            if (atLeastFiveCount >= 2) yield return DerivedStimulating;
            if (atLeastFiveCount == 0) yield return DerivedBalanced;
            if (ingredientKindCount == 3) yield return DerivedSimple;
            if (ingredientKindCount >= 5) yield return DerivedComplex;
            if (totalAmount <= 11) yield return DerivedLight;
            if (totalAmount >= 15) yield return DerivedStrong;
        }

        private string LocalizeKey(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return string.Empty;

            if (localizedByKey.TryGetValue(rawKey, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
                return mapped;

            // fallback: key를 보기 좋은 문자열로 단순 정리
            return rawKey.Replace("CATEGORY_", string.Empty)
                         .Replace("TAG_", string.Empty)
                         .Replace("DERIVED_", string.Empty)
                         .Replace('_', ' ')
                         .Trim();
        }

        private void RebuildLocalizationTable()
        {
            localizedByKey.Clear();
            hiddenTagKeys.Clear();

            // 인스펙터 입력 우선
            if (localizedEntries != null)
            {
                for (int i = 0; i < localizedEntries.Length; i++)
                {
                    var row = localizedEntries[i];
                    if (string.IsNullOrWhiteSpace(row.key))
                        continue;

                    localizedByKey[row.key.Trim()] = row.value ?? string.Empty;
                }
            }

            if (hiddenRawTagKeys != null)
            {
                for (int i = 0; i < hiddenRawTagKeys.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(hiddenRawTagKeys[i]))
                        hiddenTagKeys.Add(hiddenRawTagKeys[i].Trim());
                }
            }

            // 기본 한국어 fallback (인스펙터에 값이 없을 때만)
            AddDefault("CATEGORY_JUICE", "주스");
            AddDefault("CATEGORY_DAIRY", "유제품");
            AddDefault("CATEGORY_SODA", "탄산음료");
            AddDefault("CATEGORY_TRADITIONAL_TEA", "전통차");
            AddDefault("CATEGORY_PLANT_DRINK", "식물성음료");
            AddDefault("CATEGORY_PLANT_MILK", "식물성음료");
            AddDefault("CATEGORY_NONE", string.Empty);

            AddDefault("TAG_CHOCOLATE", "초콜릿");
            AddDefault("TAG_COFFEE", "커피");
            AddDefault("TAG_TEA", "차");
            AddDefault("TAG_YOGURT", "요거트");
            AddDefault("TAG_COLA", "콜라");
            AddDefault("TAG_ENERGY_DRINK", "에너지드링크");
            AddDefault("TAG_MILK", "우유");

            AddDefault("INGREDIENT_VELTRINE", "벨트린");
            AddDefault("INGREDIENT_ZYPHRATE", "자이프레이트");
            AddDefault("INGREDIENT_REDULINE", "레듈린");
            AddDefault("INGREDIENT_MORVION", "모르비온");
            AddDefault("INGREDIENT_KRATYLEN", "크라틸렌");
            AddDefault("INGREDIENT_CYMENTOL", "사이멘톨");
            AddDefault("INGREDIENT_BRAXIUM", "브랙시움");

            AddDefault(DerivedStimulating, "자극적");
            AddDefault(DerivedBalanced, "무난함");
            AddDefault(DerivedSimple, "단순함");
            AddDefault(DerivedComplex, "복잡함");
            AddDefault(DerivedLight, "가벼움");
            AddDefault(DerivedStrong, "강렬함");
            AddDefault(MetaArtheonAddable, "아르테온 추가 가능");
        }

        private void AddDefault(string key, string value)
        {
            if (!localizedByKey.ContainsKey(key))
                localizedByKey[key] = value;
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;

            // 텍스트 줄바꿈은 TMP 기본 동작에 맡긴다.
            text.enableWordWrapping = true;
            text.text = value ?? string.Empty;
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData != null && eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                return;

            if (current == null)
                return;

            onClicked?.Invoke(current);
        }
    }
}
