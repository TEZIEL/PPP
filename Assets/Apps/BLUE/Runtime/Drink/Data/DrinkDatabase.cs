using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkDatabase
    {
        public List<DrinkData> drinks = new List<DrinkData>();
        public List<DrinkRequest> requests = new List<DrinkRequest>();
        public HashSet<string> ingredients = new HashSet<string>();

        public DrinkRequest FindRequest(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return null;

            for (int i = 0; i < requests.Count; i++)
            {
                if (string.Equals(requests[i].requestID, requestId, System.StringComparison.OrdinalIgnoreCase))
                    return requests[i];
            }

            return null;
        }

        public DrinkData FindDrink(string drinkId)
        {
            if (string.IsNullOrEmpty(drinkId))
                return null;

            for (int i = 0; i < drinks.Count; i++)
            {
                if (string.Equals(drinks[i].id, drinkId, System.StringComparison.OrdinalIgnoreCase))
                    return drinks[i];
            }

            return null;
        }
    }
}
