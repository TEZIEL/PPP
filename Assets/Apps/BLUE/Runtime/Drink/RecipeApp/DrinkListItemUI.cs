using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// 스크롤 리스트의 음료 아이템 1개를 담당.
    /// 이름/카테고리/설명/재료/태그를 모두 출력한다.
    /// </summary>
    public sealed class DrinkListItemUI : MonoBehaviour
    {
        [SerializeField] private Image drinkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ingredientsText;
        [SerializeField] private TMP_Text tagsText;
        [SerializeField] private Button actionButton;

        private DrinkEntry current;
        private Action<DrinkEntry> onClicked;

        private void Awake()
        {
            if (actionButton != null)
                actionButton.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// 아이템 표시 데이터 초기화
        /// </summary>
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

            if (categoryText != null)
                categoryText.text = string.IsNullOrWhiteSpace(drink?.category) ? "카테고리 없음" : drink.category;

            if (descriptionText != null)
                descriptionText.text = FormatDescription(drink?.description);

            if (ingredientsText != null)
                ingredientsText.text = FormatIngredients(drink, ingredientDisplayNames);

            if (tagsText != null)
                tagsText.text = FormatTags(drink?.tags);

            if (drinkImage != null)
            {
                drinkImage.sprite = image;
                drinkImage.enabled = image != null;
            }
        }

        // 설명 포맷
        private static string FormatDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "설명: 설명 없음";

            return $"설명: {description}";
        }

        // 태그 포맷
        private static string FormatTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
                return "태그: 태그 없음";

            return $"태그: {string.Join(", ", tags)}";
        }

        // 재료 포맷 (가능하면 ingredients.json 표시 이름 사용)
        private static string FormatIngredients(DrinkEntry drink, IReadOnlyDictionary<string, string> ingredientDisplayNames)
        {
            if (drink == null || drink.ingredientAmounts == null || drink.ingredientAmounts.Count == 0)
                return "재료: 재료 없음";

            var sb = new StringBuilder();
            sb.Append("재료: ");

            int index = 0;
            foreach (var pair in drink.ingredientAmounts)
            {
                if (index > 0)
                    sb.Append(", ");

                string displayName = pair.Key;
                if (ingredientDisplayNames != null && ingredientDisplayNames.TryGetValue(pair.Key, out var mappedName) && !string.IsNullOrWhiteSpace(mappedName))
                    displayName = mappedName;

                sb.Append(displayName);
                sb.Append(" x");
                sb.Append(pair.Value);
                index++;
            }

            return sb.ToString();
        }

        private void HandleClick()
        {
            if (current == null)
                return;

            onClicked?.Invoke(current);
        }
    }
}
