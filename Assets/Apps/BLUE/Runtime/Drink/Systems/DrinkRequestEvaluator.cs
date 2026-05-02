using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkEvalContext
    {
        public bool artheonAdded;
        public DrinkData producedDrink;
        public IReadOnlyDictionary<string, int> producedIngredients;
    }

    public sealed class DrinkRequestEvaluator
    {
        private readonly DrinkDatabase database;

        public DrinkRequestEvaluator(DrinkDatabase database)
        {
            this.database = database;
        }

        

        public string Evaluate(string drinkId, DrinkRequest request, string requestIdInput = null)
        {
            return Evaluate(drinkId, request, null, requestIdInput);
        }

        public string Evaluate(string drinkId, DrinkRequest request, DrinkEvalContext context, string requestIdInput = null)
        {
            if (request == null)
                return "fail";

            var producedDrink = context?.producedDrink ?? database?.FindDrink(drinkId);

            if (IsGreatCandidate(drinkId, request, producedDrink) &&
                MeetsExtendedConstraints(isGreat: true, request, producedDrink, context))
            {
                return "great";
            }

            if (request.noSuccess)
                return "fail";

            switch (request.type)
            {
                case DrinkRequestType.EXACT_DRINK:
                    return "fail";

                case DrinkRequestType.CATEGORY_REQUEST:
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";

                    var categoryDrink = producedDrink;
                    if (categoryDrink == null)
                        return "fail";

                    bool categoryMatch = HasAnyCategoryMatch(categoryDrink, request);
                    bool extendedSuccess = MatchesAnyDrinkId(drinkId, request.successDrinkIds)
                                           || HasAnyTagMatch(categoryDrink, request.successTags)
                                           || HasAnyCategoryMatch(categoryDrink, request.successCategories);
                    bool successCandidate = categoryMatch || extendedSuccess;
                    string categoryResult = successCandidate && MeetsExtendedConstraints(false, request, categoryDrink, context)
                        ? "success"
                        : "fail";

                    LogCategoryRequest(requestIdInput, request, categoryDrink, categoryMatch, categoryResult);

                    return categoryResult;


                case DrinkRequestType.TAG_REQUEST:
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";

                    var tagDrink = producedDrink;
                    if (tagDrink == null)
                        return "fail";
                    bool tagBaseSuccess = request.tags != null && request.tags.Count > 0 && HasAnyTag(tagDrink, request);
                    bool tagExtendedSuccess = MatchesAnyDrinkId(drinkId, request.successDrinkIds)
                                              || HasAnyTagMatch(tagDrink, request.successTags)
                                              || HasAnyCategoryMatch(tagDrink, request.successCategories);
                    bool tagSuccessCandidate = tagBaseSuccess || tagExtendedSuccess;
                    return tagSuccessCandidate && MeetsExtendedConstraints(false, request, tagDrink, context) ? "success" : "fail";

                case DrinkRequestType.ANY_DRINK:
                    if (!string.IsNullOrEmpty(request.dislikedDrink) &&
                        string.Equals(drinkId, request.dislikedDrink, StringComparison.OrdinalIgnoreCase))
                    {
                        return "fail";
                    }
                    if (string.IsNullOrEmpty(drinkId))
                        return "fail";
                    bool anySuccessCandidate = true
                                               || MatchesAnyDrinkId(drinkId, request.successDrinkIds)
                                               || HasAnyTagMatch(producedDrink, request.successTags)
                                               || HasAnyCategoryMatch(producedDrink, request.successCategories);
                    return anySuccessCandidate && MeetsExtendedConstraints(false, request, producedDrink, context) ? "success" : "fail";

                case DrinkRequestType.INTENTIONAL_FAIL:
                    return string.Equals(drinkId, request.drinkID, StringComparison.OrdinalIgnoreCase) ? "fail" : "success";

                default:
                    return "fail";
            }
        }

        private static bool IsGreatCandidate(string drinkId, DrinkRequest request, DrinkData producedDrink)
        {
            if (string.IsNullOrEmpty(drinkId) || request == null)
                return false;

            if (string.Equals(drinkId, request.drinkID, StringComparison.OrdinalIgnoreCase))
                return true;

            if (MatchesAnyDrinkId(drinkId, request.likedDrink) || MatchesAnyDrinkId(drinkId, request.greatDrinkIds))
                return true;

            if (HasAnyTagMatch(producedDrink, request.greatTags))
                return true;

            if (HasAnyCategoryMatch(producedDrink, request.greatCategories))
                return true;

            return false;
        }

        private static bool MeetsExtendedConstraints(bool isGreat, DrinkRequest request, DrinkData producedDrink, DrinkEvalContext context)
        {
            bool requiresArtheon = isGreat ? request.greatRequiresArtheon : request.successRequiresArtheon;
            if (requiresArtheon && (context == null || !context.artheonAdded))
                return false;

            var requiredIngredients = isGreat ? request.greatRequiredIngredients : request.successRequiredIngredients;
            var forbiddenIngredients = isGreat ? request.greatForbiddenIngredients : request.successForbiddenIngredients;
            var producedIngredients = context?.producedIngredients ?? producedDrink?.ingredients;

            if (!HasRequiredIngredients(producedIngredients, requiredIngredients))
                return false;

            if (HasForbiddenIngredients(producedIngredients, forbiddenIngredients))
                return false;

            return true;
        }

        private static bool MatchesAnyDrinkId(string drinkId, List<string> candidates)
        {
            if (string.IsNullOrEmpty(drinkId) || candidates == null || candidates.Count == 0)
                return false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], drinkId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool HasAnyTagMatch(DrinkData drink, List<string> tags)
        {
            if (drink == null || drink.tags == null || drink.tags.Count == 0 || tags == null || tags.Count == 0)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                for (int j = 0; j < drink.tags.Count; j++)
                {
                    if (string.Equals(tags[i], drink.tags[j], StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static bool HasAnyCategoryMatch(DrinkData drink, List<string> categories)
        {
            if (drink == null || drink.category == null || drink.category.Count == 0 || categories == null || categories.Count == 0)
                return false;

            for (int i = 0; i < drink.category.Count; i++)
            {
                string drinkCategory = drink.category[i];
                if (string.IsNullOrWhiteSpace(drinkCategory))
                    continue;

                for (int j = 0; j < categories.Count; j++)
                {
                    if (string.Equals(categories[j], drinkCategory, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static bool HasRequiredIngredients(IReadOnlyDictionary<string, int> producedIngredients, Dictionary<string, int> requiredIngredients)
        {
            if (requiredIngredients == null || requiredIngredients.Count == 0)
                return true;

            if (producedIngredients == null)
                return false;

            foreach (var pair in requiredIngredients)
            {
                if (!producedIngredients.TryGetValue(pair.Key, out int amount))
                    return false;

                if (amount < pair.Value)
                    return false;
            }

            return true;
        }

        private static bool HasForbiddenIngredients(IReadOnlyDictionary<string, int> producedIngredients, List<string> forbiddenIngredients)
        {
            if (forbiddenIngredients == null || forbiddenIngredients.Count == 0)
                return false;

            if (producedIngredients == null)
                return false;

            for (int i = 0; i < forbiddenIngredients.Count; i++)
            {
                string forbidden = forbiddenIngredients[i];
                if (string.IsNullOrWhiteSpace(forbidden))
                    continue;

                if (producedIngredients.TryGetValue(forbidden, out int amount) && amount >= 1)
                    return true;
            }

            return false;
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
