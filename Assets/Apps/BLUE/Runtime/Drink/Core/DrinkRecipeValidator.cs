using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkRecipeValidator
    {
        private const string ArtheonIngredient = "INGREDIENT_ARTHEON";
        private readonly DrinkDatabase database;

        public DrinkRecipeValidator(DrinkDatabase database)
        {
            this.database = database;
        }

        public string ValidateRecipe(Dictionary<string, int> playerIngredients)
        {
            if (database == null || playerIngredients == null)
                return null;

            var checkIngredients = new Dictionary<string, int>(playerIngredients, StringComparer.Ordinal);
            bool hasArtheon = checkIngredients.TryGetValue(ArtheonIngredient, out int artheonCount) && artheonCount > 0;
            checkIngredients.Remove(ArtheonIngredient);

            for (int i = 0; i < database.drinks.Count; i++)
            {
                DrinkData drink = database.drinks[i];
                if (drink == null)
                    continue;

                if (hasArtheon && !drink.artheon_addable)
                    continue;

                if (MatchRecipe(checkIngredients, drink.ingredients))
                    return drink.id;
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

        public DrinkData FindClosestRecipe(Dictionary<string, int> playerIngredients)
        {
            if (database == null || playerIngredients == null || database.drinks.Count == 0)
                return null;

            var checkIngredients = new Dictionary<string, int>(playerIngredients, StringComparer.Ordinal);
            bool hasArtheon = checkIngredients.TryGetValue(ArtheonIngredient, out int artheonCount) && artheonCount > 0;
            checkIngredients.Remove(ArtheonIngredient);

            DrinkData best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < database.drinks.Count; i++)
            {
                var drink = database.drinks[i];
                if (drink == null)
                    continue;

                if (hasArtheon && !drink.artheon_addable)
                    continue;

                int score = CalculateSimilarityScore(checkIngredients, drink.ingredients);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = drink;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static int CalculateSimilarityScore(Dictionary<string, int> player, Dictionary<string, int> recipe)
        {
            int score = 0;
            foreach (var entry in recipe)
            {
                if (!player.TryGetValue(entry.Key, out int playerCount))
                    continue;

                score += Math.Min(playerCount, entry.Value);
                if (playerCount == entry.Value)
                    score += 2;
            }
            return score;
        }
    }
}
