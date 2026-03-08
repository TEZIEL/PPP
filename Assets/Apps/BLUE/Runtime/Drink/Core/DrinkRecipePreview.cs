using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkRecipePreview
    {
        private readonly DrinkRecipeValidator validator;

        public DrinkRecipePreview(DrinkRecipeValidator validator)
        {
            this.validator = validator;
        }

        public string PredictDrinkName(Dictionary<string, int> currentIngredients)
        {
            if (validator == null)
                return "Unknown Drink";

            var closest = validator.FindClosestRecipe(currentIngredients);
            if (closest == null || string.IsNullOrEmpty(closest.name))
                return "Unknown Drink";

            return closest.name;
        }
    }
}
