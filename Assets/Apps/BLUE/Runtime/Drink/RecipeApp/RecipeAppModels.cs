using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// ingredients.json 최상위 래퍼
    /// </summary>
    [Serializable]
    public sealed class IngredientRoot
    {
        public List<IngredientEntry> ingredients = new List<IngredientEntry>();
    }

    /// <summary>
    /// 재료 데이터 1개
    /// </summary>
    [Serializable]
    public sealed class IngredientEntry
    {
        public string id;
        public string nameKey;
        public string name;
        public string english_name;
        public string color;
        public string colorHex;
        public string taste;
        public string description;

        public string DisplayName => string.IsNullOrWhiteSpace(name) ? id : name;
        public string DisplayNameKey => string.IsNullOrWhiteSpace(nameKey) ? id : nameKey;
        public string DisplayColorHex => string.IsNullOrWhiteSpace(colorHex) ? color : colorHex;
    }

    /// <summary>
    /// drinks.json 최상위 래퍼
    /// </summary>
    [Serializable]
    public sealed class DrinkRoot
    {
        public List<DrinkEntry> drinks = new List<DrinkEntry>();
    }

    /// <summary>
    /// 음료 데이터 1개
    /// </summary>
    [Serializable]
    public sealed class DrinkEntry
    {
        public string id;
        public string name;
        public string description;
        public string imageKey;
        public string category;
        public bool artheon_addable;
        public int total;
        public List<string> tags = new List<string>();

        // 재료 표시용: 재료 ID -> 수량
        public Dictionary<string, int> ingredientAmounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 필터용: Contains 검사 최적화
        public HashSet<string> ingredientKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool ContainsIngredient(string ingredientId)
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
                return false;

            return ingredientKeys.Contains(ingredientId);
        }
    }
}
