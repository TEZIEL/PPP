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
        public string name;
        public string english_name;
        public string color;
        public string taste;
        public string description;

        public string DisplayName => string.IsNullOrWhiteSpace(name) ? id : name;
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

        // JSON 원본의 ingredients(객체)를 key 집합으로 정규화해서 저장한다.
        public HashSet<string> ingredientKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool ContainsIngredient(string ingredientId)
        {
            if (string.IsNullOrWhiteSpace(ingredientId))
                return false;

            return ingredientKeys.Contains(ingredientId);
        }
    }
}
