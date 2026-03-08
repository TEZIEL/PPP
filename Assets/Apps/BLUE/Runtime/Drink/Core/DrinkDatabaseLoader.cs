using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkDatabaseLoader : MonoBehaviour
    {
        [Header("JSON Assets")]
        [SerializeField] private TextAsset drinksJson;
        [SerializeField] private TextAsset requestsJson;
        [SerializeField] private TextAsset ingredientsJson;

        public DrinkDatabase LoadDatabase()
        {
            var db = new DrinkDatabase();
            TryLoadDrinks(db);
            LoadRequests(db);
            TryLoadIngredients(db);
            return db;
        }

        private void TryLoadDrinks(DrinkDatabase db)
        {
            if (drinksJson == null || string.IsNullOrWhiteSpace(drinksJson.text))
                return;

            if (!(MiniJson.Deserialize(drinksJson.text) is Dictionary<string, object> root) ||
                !root.TryGetValue("drinks", out object drinksObj) || !(drinksObj is List<object> drinks))
                return;

            for (int i = 0; i < drinks.Count; i++)
            {
                if (!(drinks[i] is Dictionary<string, object> rawDrink))
                    continue;

                var drink = new DrinkData
                {
                    id = GetString(rawDrink, "id"),
                    name = GetString(rawDrink, "name"),
                    artheon_addable = GetBool(rawDrink, "artheon_addable"),
                    total = GetInt(rawDrink, "total")
                };

                if (rawDrink.TryGetValue("ingredients", out object ingredientsObj) && ingredientsObj is Dictionary<string, object> rawIngredients)
                {
                    foreach (var pair in rawIngredients)
                        drink.ingredients[pair.Key] = ToInt(pair.Value);
                }

                if (rawDrink.TryGetValue("category", out object categoryObj) && categoryObj is List<object> categories)
                {
                    for (int c = 0; c < categories.Count; c++)
                        drink.category.Add(categories[c]?.ToString() ?? string.Empty);
                }

                if (rawDrink.TryGetValue("tags", out object tagsObj) && tagsObj is List<object> tags)
                {
                    for (int t = 0; t < tags.Count; t++)
                        drink.tags.Add(tags[t]?.ToString() ?? string.Empty);
                }

                if (!string.IsNullOrEmpty(drink.id))
                    db.drinks.Add(drink);
            }
        }

        private void LoadRequests(DrinkDatabase db)
        {
            if (requestsJson == null || string.IsNullOrWhiteSpace(requestsJson.text))
                return;

            if (!(MiniJson.Deserialize(requestsJson.text) is Dictionary<string, object> root) ||
                !root.TryGetValue("requests", out object requestsObj) || !(requestsObj is List<object> requests))
                return;

            for (int i = 0; i < requests.Count; i++)
            {
                if (!(requests[i] is Dictionary<string, object> rawRequest))
                    continue;

                var request = new DrinkRequest
                {
                    requestID = GetString(rawRequest, "requestID"),
                    drinkID = GetString(rawRequest, "drinkID"),
                    category = GetString(rawRequest, "category"),
                    likedDrink = GetString(rawRequest, "likedDrink"),
                    dislikedDrink = GetString(rawRequest, "dislikedDrink")
                };

                if (Enum.TryParse(GetString(rawRequest, "type"), true, out DrinkRequestType parsedType))
                    request.type = parsedType;

                if (rawRequest.TryGetValue("tags", out object tagsObj) && tagsObj is List<object> tags)
                {
                    for (int t = 0; t < tags.Count; t++)
                        request.tags.Add(tags[t]?.ToString() ?? string.Empty);
                }

                if (!string.IsNullOrEmpty(request.requestID))
                    db.requests[request.requestID] = request;
            }

            Debug.Log($"[DrinkDB] Loaded {db.requests.Count} requests");
        }

        private void TryLoadIngredients(DrinkDatabase db)
        {
            if (ingredientsJson == null || string.IsNullOrWhiteSpace(ingredientsJson.text))
                return;

            if (!(MiniJson.Deserialize(ingredientsJson.text) is Dictionary<string, object> root) ||
                !root.TryGetValue("ingredients", out object ingredientsObj) || !(ingredientsObj is List<object> ingredients))
                return;

            for (int i = 0; i < ingredients.Count; i++)
            {
                var value = ingredients[i]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    db.ingredients.Add(value);
            }
        }

        private static string GetString(Dictionary<string, object> data, string key)
            => data.TryGetValue(key, out object value) ? value?.ToString() ?? string.Empty : string.Empty;

        private static bool GetBool(Dictionary<string, object> data, string key)
            => data.TryGetValue(key, out object value) && ToBool(value);

        private static int GetInt(Dictionary<string, object> data, string key)
            => data.TryGetValue(key, out object value) ? ToInt(value) : 0;

        private static int ToInt(object value)
        {
            if (value is long l) return (int)l;
            if (value is int i) return i;
            if (value is double d) return Mathf.RoundToInt((float)d);
            if (value == null) return 0;
            return int.TryParse(value.ToString(), out int parsed) ? parsed : 0;
        }

        private static bool ToBool(object value)
        {
            if (value is bool b) return b;
            if (value is string s && bool.TryParse(s, out bool parsed)) return parsed;
            return false;
        }
    }
}
