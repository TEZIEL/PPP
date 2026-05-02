using System;
using System.Collections.Generic;


namespace PPP.BLUE.VN.DrinkSystem
{
    public enum DrinkRequestType
    {
        EXACT_DRINK,
        CATEGORY_REQUEST,
        TAG_REQUEST,
        ANY_DRINK,
        INTENTIONAL_FAIL
    }

    [Serializable]
    public sealed class DrinkRequest
    {
        public string requestID;
        public DrinkRequestType type;
        public string drinkID;
        public string category;
        public List<string> categories = new List<string>();
        public List<string> tags = new List<string>();
        public List<string> likedDrink;
        public string dislikedDrink;

        // Phase 1 extensions
        public List<string> greatDrinkIds;
        public List<string> successDrinkIds;
        public List<string> greatTags;
        public List<string> successTags;
        public List<string> greatCategories;
        public List<string> successCategories;
        public bool noSuccess;

        // Phase 2 extensions
        public bool greatRequiresArtheon;
        public bool successRequiresArtheon;
        public Dictionary<string, int> greatRequiredIngredients;
        public Dictionary<string, int> successRequiredIngredients;
        public List<string> greatForbiddenIngredients;
        public List<string> successForbiddenIngredients;
    }
}
