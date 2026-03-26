using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkRequestEvaluator
    {
        private readonly DrinkDatabase database;

        public DrinkRequestEvaluator(DrinkDatabase database)
        {
            this.database = database;
        }

        

        public string Evaluate(string drinkId, DrinkRequest request, string requestIdInput = null)
        {
            if (request == null)
                return "fail";

            switch (request.type)
            {
                case DrinkRequestType.EXACT_DRINK:
                    return string.Equals(drinkId, request.drinkID, StringComparison.OrdinalIgnoreCase) ? "great" : "fail";

                case DrinkRequestType.CATEGORY_REQUEST:
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";

                    var categoryDrink = database?.FindDrink(drinkId);
                    if (categoryDrink == null)
                        return "fail";

                    // ✅ GREAT 먼저 체크
                    if (request.likedDrink != null &&
                        request.likedDrink.Contains(drinkId))
                    {
                        return "great";
                    }

                    bool categoryMatch = HasAnyCategoryMatch(categoryDrink, request);

                    string categoryResult = categoryMatch ? "success" : "fail";

                    LogCategoryRequest(requestIdInput, request, categoryDrink, categoryMatch, categoryResult);

                    return categoryResult;


                case DrinkRequestType.TAG_REQUEST:
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";

                    var tagDrink = database?.FindDrink(drinkId);
                    if (tagDrink == null)
                        return "fail";

                    // 🔥 수정된 부분
                    if (request.likedDrink != null &&
                        request.likedDrink.Contains(drinkId))
                    {
                        return "great";
                    }

                    return request.tags != null && request.tags.Count > 0 && HasAnyTag(tagDrink, request)
                        ? "success"
                        : "fail";

                case DrinkRequestType.ANY_DRINK:
                    if (!string.IsNullOrEmpty(request.dislikedDrink) &&
                        string.Equals(drinkId, request.dislikedDrink, StringComparison.OrdinalIgnoreCase))
                    {
                        return "fail";
                    }

                    // 🔥 수정된 부분
                    if (request.likedDrink != null &&
                        request.likedDrink.Contains(drinkId))
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

        private static bool HasAnyCategoryMatch(DrinkData drink, DrinkRequest request)
        {
            if (drink == null || drink.category == null || drink.category.Count == 0 || request == null)
                return false;

            List<string> requestCategories = request.categories;
            if (requestCategories == null || requestCategories.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(request.category))
                    return false;

                requestCategories = new List<string> { request.category };
            }

            for (int i = 0; i < drink.category.Count; i++)
            {
                string drinkCategory = drink.category[i];
                if (string.IsNullOrWhiteSpace(drinkCategory))
                    continue;

                for (int j = 0; j < requestCategories.Count; j++)
                {
                    if (string.Equals(requestCategories[j], drinkCategory, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static void LogCategoryRequest(string requestIdInput, DrinkRequest request, DrinkData drink, bool categoryMatch, string result)
        {
            string requestCategories = request != null && request.categories != null && request.categories.Count > 0
                ? string.Join(",", request.categories)
                : (request?.category ?? string.Empty);
            string drinkCategories = drink != null && drink.category != null && drink.category.Count > 0
                ? string.Join(",", drink.category)
                : string.Empty;

            Debug.Log(
                $"[Drink][CATEGORY_REQUEST] requestIdInput={requestIdInput ?? string.Empty} " +
                $"resolvedRequestId={request?.requestID ?? string.Empty} requestType={request?.type.ToString() ?? string.Empty} " +
                $"requestCategory={requestCategories} resolvedDrinkId={drink?.id ?? string.Empty} " +
                $"drinkCategory={drinkCategories} categoryMatch={categoryMatch} result={result}"
            );
        }
    }
}
