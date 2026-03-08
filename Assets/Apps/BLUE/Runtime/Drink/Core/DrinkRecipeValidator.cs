using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkRecipeValidator
    {
        private readonly DrinkDatabase database;

        public DrinkRecipeValidator(DrinkDatabase database)
        {
            this.database = database;
        }

        public string ValidateRecipe(Dictionary<string, int> playerIngredients, bool artheonEnabled, out bool blockedByArtheon)
        {
            blockedByArtheon = false;

            if (database == null || playerIngredients == null)
                return null;

            for (int i = 0; i < database.drinks.Count; i++)
            {
                DrinkData drink = database.drinks[i];
                if (drink == null)
                    continue;

                if (MatchRecipe(playerIngredients, drink.ingredients))
                {
                    if (artheonEnabled && !drink.artheon_addable)
                    {
                        blockedByArtheon = true;
                        return null;
                    }

                    return drink.id;
                }
            }

            return null;
        }

        public bool MatchRecipe(Dictionary<string, int> player, Dictionary<string, int> recipe)
        {
            if (player == null || recipe == null)
                return false;

            if (player.Count != recipe.Count)
                return false;

            foreach (var pair in recipe)
            {
                if (!player.TryGetValue(pair.Key, out int value))
                    return false;

                if (value != pair.Value)
                    return false;
            }

            return true;
        }
    }
}
