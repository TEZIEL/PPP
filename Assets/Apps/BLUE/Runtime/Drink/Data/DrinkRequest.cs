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
        public List<string> tags = new List<string>();
        public string likedDrink;
        public string dislikedDrink;
    }
}
