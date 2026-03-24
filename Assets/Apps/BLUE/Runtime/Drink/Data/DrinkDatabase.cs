using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkDatabase
    {
        public List<DrinkData> drinks = new List<DrinkData>();
        public Dictionary<string, DrinkRequest> requests = new Dictionary<string, DrinkRequest>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ingredients = new HashSet<string>();
        public Dictionary<string, IngredientData> ingredientInfos = new Dictionary<string, IngredientData>(StringComparer.OrdinalIgnoreCase);

        public DrinkRequest FindRequest(string requestId)
        {
            return GetRequest(requestId);
        }

        public DrinkRequest GetRequest(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return null;

            return requests.TryGetValue(requestId, out var request) ? request : null;
        }

        public DrinkData FindDrink(string drinkId)
        {
            if (string.IsNullOrEmpty(drinkId))
                return null;

            for (int i = 0; i < drinks.Count; i++)
            {
                if (string.Equals(drinks[i].id, drinkId, StringComparison.OrdinalIgnoreCase))
                    return drinks[i];
            }

            return null;
        }

        public IngredientData FindIngredient(string ingredientId)
        {
            if (string.IsNullOrEmpty(ingredientId))
                return null;

            return ingredientInfos.TryGetValue(ingredientId, out var info) ? info : null;
        }
    }
}
