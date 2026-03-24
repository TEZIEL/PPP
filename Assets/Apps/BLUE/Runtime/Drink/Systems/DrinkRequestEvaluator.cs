using System;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkRequestEvaluator
    {
        private readonly DrinkDatabase database;

        public DrinkRequestEvaluator(DrinkDatabase database)
        {
            this.database = database;
        }

        public string Evaluate(string drinkId, DrinkRequest request)
        {
            if (request == null)
                return "fail";

            switch (request.type)
            {
                case DrinkRequestType.EXACT_DRINK:
                    return string.Equals(drinkId, request.drinkID, StringComparison.OrdinalIgnoreCase) ? "great" : "fail";

                case DrinkRequestType.CATEGORY_REQUEST:
                case DrinkRequestType.TAG_REQUEST:
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";

                    var drink = database?.FindDrink(drinkId);
                    if (drink == null)
                        return "fail";

                    if (!string.IsNullOrEmpty(request.likedDrink) &&
                        string.Equals(drinkId, request.likedDrink, StringComparison.OrdinalIgnoreCase))
                    {
                        return "great";
                    }

                    bool categoryMatch = !string.IsNullOrEmpty(request.category) && drink.category.Contains(request.category);
                    bool tagMatch = request.tags != null && request.tags.Count > 0 && HasAnyTag(drink, request);
                    return (categoryMatch || tagMatch) ? "success" : "fail";

                case DrinkRequestType.ANY_DRINK:
                    if (!string.IsNullOrEmpty(request.dislikedDrink) &&
                        string.Equals(drinkId, request.dislikedDrink, StringComparison.OrdinalIgnoreCase))
                    {
                        return "fail";
                    }

                    if (!string.IsNullOrEmpty(request.likedDrink) &&
                        string.Equals(drinkId, request.likedDrink, StringComparison.OrdinalIgnoreCase))
                    {
                        return "great";
                    }

                    return string.IsNullOrEmpty(drinkId) ? "fail" : "success";

                case DrinkRequestType.INTENTIONAL_FAIL:
                    return string.Equals(drinkId, request.drinkID, StringComparison.OrdinalIgnoreCase) ? "fail" : "success";

                default:
                    return "fail";
            }
        }

        private static bool HasAnyTag(DrinkData drink, DrinkRequest request)
        {
            for (int i = 0; i < request.tags.Count; i++)
            {
                if (drink.tags.Contains(request.tags[i]))
                    return true;
            }

            return false;
        }
    }
}
