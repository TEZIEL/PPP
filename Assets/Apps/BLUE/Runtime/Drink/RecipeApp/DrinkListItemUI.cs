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
    /// 표시 규칙:
    /// - 접두사(재료:, 설명:, 태그:) 제거
    /// - 카테고리 + 태그 + 파생 태그를 통합 문자열로 출력
    /// - 아르테온은 일반 재료가 아닌 특수 문구로 처리
    /// </summary>
    public sealed class DrinkListItemUI : MonoBehaviour
    {
        [SerializeField] private Image drinkImage;
        [SerializeField] private TMP_Text nameText;

        // 기존 프리팹의 CategoryText 슬롯을 메타(카테고리+태그) 통합 라인으로 재활용한다.
        [SerializeField] private TMP_Text categoryText;

        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ingredientsText;

        // 기존 프리팹 호환을 위해 남겨둔다. (통합 메타를 categoryText로 쓰므로 비워둔다)
        [SerializeField] private TMP_Text tagsText;

        [SerializeField] private Button actionButton;

        private DrinkEntry current;
        private Action<DrinkEntry> onClicked;

        private static readonly Dictionary<string, string> CategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CATEGORY_JUICE", "주스" },
            { "CATEGORY_DAIRY", "유제품" },
            { "CATEGORY_SODA", "탄산음료" },
            { "CATEGORY_CHOCOLATE", "초콜릿" },
            { "CATEGORY_TEA", "차" },
            { "CATEGORY_COFFEE", "커피" },
            { "CATEGORY_YOGURT", "요거트" },
            { "CATEGORY_ENERGY", "에너지드링크" },
            { "CATEGORY_ENERGY_DRINK", "에너지드링크" },
            { "CATEGORY_TRADITIONAL_TEA", "전통차" },
            { "CATEGORY_PLANT_MILK", "식물성음료" },
            { "CATEGORY_PLANT_DRINK", "식물성음료" },
            { "CATEGORY_NONE", string.Empty },
            { "NONE", string.Empty }
        };

        private static readonly Dictionary<string, string> TagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TAG_VELTRINE_PLUS", "벨트린+" },
            { "TAG_ZYPHRATE_PLUS", "자이프레이트+" },
            { "TAG_REDULINE_PLUS", "레듈린+" },
            { "TAG_MORVION_PLUS", "모르비온+" },
            { "TAG_KRATYLEN_PLUS", "크라틸렌+" },
            { "TAG_CYMENTOL_PLUS", "사이멘톨+" },
            { "TAG_BRAXIUM_PLUS", "브랙시움+" },

            { "TAG_CHOCOLATE", "초콜릿" },
            { "TAG_COFFEE", "커피" },
            { "TAG_TEA", "차" },
            { "TAG_YOGURT", "요거트" },
            { "TAG_COLA", "콜라" },
            { "TAG_ENERGY_DRINK", "에너지드링크" },

            // 아래는 파생 태그와 의미가 겹치므로 내부 태그는 숨긴다.
            { "TAG_SIMPLE", string.Empty },
            { "TAG_COMPLEX", string.Empty },
            { "TAG_LIGHT", string.Empty },
            { "TAG_STRONG", string.Empty },
            { "TAG_STIMULATING", string.Empty },
            { "TAG_BALANCED", string.Empty },

            // none 계열은 출력 제외
            { "TAG_VELTRINE_NONE", string.Empty },
            { "TAG_BELTRINE_NONE", string.Empty },
            { "TAG_NONE", string.Empty }
        };

        private void Awake()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(HandleClick);
        }

        public void Setup(
            DrinkEntry drink,
            Sprite image,
            IReadOnlyDictionary<string, string> ingredientDisplayNames,
            Action<DrinkEntry> clickHandler)
        {
            current = drink;
            onClicked = clickHandler;

            if (nameText != null)
                nameText.text = drink?.name ?? "(이름 없음)";

            if (ingredientsText != null)
                ingredientsText.text = BuildIngredientsText(drink, ingredientDisplayNames);

            if (descriptionText != null)
                descriptionText.text = BuildDescriptionText(drink);

            string metaText = BuildMetaText(drink);
            if (categoryText != null)
                categoryText.text = metaText;

            // 카테고리/태그 분리 출력 대신 통합 출력으로 전환
            if (tagsText != null)
                tagsText.text = string.Empty;

            if (drinkImage != null)
            {
                drinkImage.sprite = image;
                drinkImage.enabled = image != null;
            }
        }

        private static string BuildIngredientsText(DrinkEntry drink, IReadOnlyDictionary<string, string> ingredientDisplayNames)
        {
            if (drink == null || drink.ingredientAmounts == null || drink.ingredientAmounts.Count == 0)
                return string.Empty;

            var ordered = drink.ingredientAmounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
            var parts = new List<string>();

            foreach (var pair in ordered)
            {
                string name = pair.Key;
                if (ingredientDisplayNames != null && ingredientDisplayNames.TryGetValue(pair.Key, out var mappedName) && !string.IsNullOrWhiteSpace(mappedName))
                    name = mappedName;

                parts.Add($"{name} x{pair.Value}");
            }

            if (drink.artheon_addable)
                parts.Add("아르테온 추가 가능");

            return string.Join(", ", parts);
        }

        private static string BuildDescriptionText(DrinkEntry drink)
        {
            if (drink == null || string.IsNullOrWhiteSpace(drink.description))
                return string.Empty;

            return drink.description.Trim();
        }

        private static string BuildMetaText(DrinkEntry drink)
        {
            if (drink == null)
                return string.Empty;

            var tokens = new List<string>();

            // 1) 카테고리
            string translatedCategory = TranslateCategory(drink.category);
            if (!string.IsNullOrWhiteSpace(translatedCategory))
                tokens.Add(translatedCategory);

            // 2) 기존 태그 번역
            if (drink.tags != null)
            {
                for (int i = 0; i < drink.tags.Count; i++)
                {
                    string translatedTag = TranslateTag(drink.tags[i]);
                    if (!string.IsNullOrWhiteSpace(translatedTag))
                        tokens.Add(translatedTag);
                }
            }

            // 3) 파생 태그
            tokens.AddRange(GetDerivedTags(drink));

            // 4) 후처리 필터 + 중복 제거
            var unique = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i]?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                // none/none계열 방어
                if (token.Equals("none", StringComparison.OrdinalIgnoreCase) || token.Equals("CATEGORY_NONE", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!seen.Add(token))
                    continue;

                unique.Add(token);
            }

            return string.Join(", ", unique);
        }

        private static string TranslateCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return string.Empty;

            string raw = category.Trim();

            if (CategoryMap.TryGetValue(raw, out var mapped))
                return mapped;

            // 유사 키 대응
            if (raw.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
                return string.Empty;
            if (raw.IndexOf("JUICE", StringComparison.OrdinalIgnoreCase) >= 0)
                return "주스";
            if (raw.IndexOf("DAIRY", StringComparison.OrdinalIgnoreCase) >= 0)
                return "유제품";
            if (raw.IndexOf("SODA", StringComparison.OrdinalIgnoreCase) >= 0)
                return "탄산음료";
            if (raw.IndexOf("TRADITIONAL_TEA", StringComparison.OrdinalIgnoreCase) >= 0)
                return "전통차";
            if (raw.IndexOf("TEA", StringComparison.OrdinalIgnoreCase) >= 0)
                return "차";
            if (raw.IndexOf("COFFEE", StringComparison.OrdinalIgnoreCase) >= 0)
                return "커피";
            if (raw.IndexOf("YOGURT", StringComparison.OrdinalIgnoreCase) >= 0)
                return "요거트";
            if (raw.IndexOf("PLANT", StringComparison.OrdinalIgnoreCase) >= 0)
                return "식물성음료";
            if (raw.IndexOf("ENERGY", StringComparison.OrdinalIgnoreCase) >= 0)
                return "에너지드링크";
            if (raw.IndexOf("CHOCOLATE", StringComparison.OrdinalIgnoreCase) >= 0)
                return "초콜릿";

            return raw;
        }

        private static string TranslateTag(string rawTag)
        {
            if (string.IsNullOrWhiteSpace(rawTag))
                return string.Empty;

            string tag = rawTag.Trim();
            if (TagMap.TryGetValue(tag, out var mapped))
                return mapped;

            // 안전 장치: none 계열은 표시 금지
            if (tag.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
                return string.Empty;

            // PLUS 키 자동 변환 (플러스 -> +)
            if (tag.IndexOf("PLUS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var baseName = tag
                    .Replace("TAG_", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("_PLUS", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string translated = TranslateIngredientToken(baseName);
                if (!string.IsNullOrWhiteSpace(translated))
                    return translated + "+";
            }

            return string.Empty;
        }

        private static string TranslateIngredientToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            if (token.Equals("VELTRINE", StringComparison.OrdinalIgnoreCase)) return "벨트린";
            if (token.Equals("ZYPHRATE", StringComparison.OrdinalIgnoreCase)) return "자이프레이트";
            if (token.Equals("REDULINE", StringComparison.OrdinalIgnoreCase)) return "레듈린";
            if (token.Equals("MORVION", StringComparison.OrdinalIgnoreCase)) return "모르비온";
            if (token.Equals("KRATYLEN", StringComparison.OrdinalIgnoreCase)) return "크라틸렌";
            if (token.Equals("CYMENTOL", StringComparison.OrdinalIgnoreCase)) return "사이멘톨";
            if (token.Equals("BRAXIUM", StringComparison.OrdinalIgnoreCase)) return "브랙시움";
            return token;
        }

        private static IEnumerable<string> GetDerivedTags(DrinkEntry drink)
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

            // 자극적: 5개 이상 재료가 2개 이상
            if (atLeastFiveCount >= 2)
                yield return "자극적";

            // 무난함: 5개 이상 재료가 없음
            if (atLeastFiveCount == 0)
                yield return "무난함";

            // 단순함: 재료 종류 3개
            if (ingredientKindCount == 3)
                yield return "단순함";

            // 복잡함: 재료 종류 5개 이상
            if (ingredientKindCount >= 5)
                yield return "복잡함";

            // 가벼움: 총합 11 이하
            if (totalAmount <= 11)
                yield return "가벼움";

            // 강렬함: 총합 15 이상
            if (totalAmount >= 15)
                yield return "강렬함";
        }

        private void HandleClick()
        {
            if (current == null)
                return;

            onClicked?.Invoke(current);
        }
    }
}
