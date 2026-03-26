using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkDatabaseLoader : MonoBehaviour
    {
        private const string DataRelativePath = AppDataPaths.DRINK_ROOT;
        private const string DrinksFileName = "Drinks/drinks.json";
        private const string RequestsFileName = "Requests/requests.json";
        private const string IngredientsFileName = "Ingredients/ingredients.json";

        public DrinkDatabase LoadDatabase()
        {
            var db = new DrinkDatabase();

            string basePath = Path.Combine(Application.dataPath, DataRelativePath);
            TryLoadDrinks(db, Path.Combine(basePath, DrinksFileName));
            LoadRequests(db, Path.Combine(basePath, RequestsFileName));
            TryLoadIngredients(db, Path.Combine(basePath, IngredientsFileName));

            return db;
        }

        List<string> GetStringList(Dictionary<string, object> raw, string key)
        {
            if (!raw.TryGetValue(key, out var value) || value == null)
                return null;

            var list = value as List<object>;
            if (list == null)
                return null;

            List<string> result = new List<string>();
            foreach (var item in list)
            {
                if (item != null)
                    result.Add(item.ToString());
            }

            return result;
        }

        private void TryLoadDrinks(DrinkDatabase db, string drinksPath)
        {
            string json = ReadJsonFile(drinksPath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root) ||
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
                    imageKey = FirstNonEmpty(GetString(rawDrink, "IMAGE_KEY"), GetString(rawDrink, "imageKey")),
                    artheon_addable = GetBool(rawDrink, "artheon_addable"),
                    total = GetInt(rawDrink, "total")
                };

                if (rawDrink.TryGetValue("ingredients", out object ingredientsObj) && ingredientsObj is Dictionary<string, object> rawIngredients)
                {
                    foreach (var pair in rawIngredients)
                        drink.ingredients[pair.Key] = ToInt(pair.Value);
                }

                if (rawDrink.TryGetValue("category", out object categoryObj))
                {
                    if (categoryObj is List<object> categories)
                    {
                        for (int c = 0; c < categories.Count; c++)
                            drink.category.Add(categories[c]?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        drink.category.Add(categoryObj?.ToString() ?? string.Empty);
                    }
                }

                if (rawDrink.TryGetValue("tags", out object tagsObj) && tagsObj is List<object> tags)
                {
                    for (int t = 0; t < tags.Count; t++)
                        drink.tags.Add(tags[t]?.ToString() ?? string.Empty);
                }

                if (!string.IsNullOrEmpty(drink.id))
                    db.drinks.Add(drink);
            }

            Debug.Log($"[DrinkDB] drinks loaded = {db.drinks.Count}");
        }

        private void LoadRequests(DrinkDatabase db, string requestsPath)
        {
            string json = ReadJsonFile(requestsPath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root) ||
                !root.TryGetValue("requests", out object requestsObj) || !(requestsObj is List<object> requests))
                return;

            for (int i = 0; i < requests.Count; i++)
            {
                if (!(requests[i] is Dictionary<string, object> rawRequest))
                    continue;

                var request = new DrinkRequest
                {
                    drinkID = GetString(rawRequest, "drinkID"),
                    category = "",
                    likedDrink = GetStringList(rawRequest, "likedDrink"),   // ⭕
                    dislikedDrink = GetString(rawRequest, "dislikedDrink")
                };

                request.requestID = GetString(rawRequest, "id");
                if (string.IsNullOrEmpty(request.requestID))
                    request.requestID = GetString(rawRequest, "ID");
                if (string.IsNullOrEmpty(request.requestID))
                    request.requestID = GetString(rawRequest, "requestID");

                string typeStr = GetString(rawRequest, "type");
                // 🔴 CATEGORY_OR_TAG alias 처리
                if (string.Equals(typeStr, "CATEGORY_OR_TAG", StringComparison.OrdinalIgnoreCase))
                {
                    bool hasCategory = rawRequest.TryGetValue("category", out object categoryCheck) && categoryCheck != null;
                    bool hasTags = rawRequest.TryGetValue("tags", out object tagsCheck) && tagsCheck != null;

                    if (hasCategory)
                        typeStr = nameof(DrinkRequestType.CATEGORY_REQUEST);
                    else if (hasTags)
                        typeStr = nameof(DrinkRequestType.TAG_REQUEST);
                }

                if (Enum.TryParse(typeStr, true, out DrinkRequestType parsedType))
                    request.type = parsedType;

                if (rawRequest.TryGetValue("tags", out object tagsObj2) && tagsObj2 is List<object> tags)
                {
                    for (int t = 0; t < tags.Count; t++)
                        request.tags.Add(tags[t]?.ToString() ?? string.Empty);
                }

                // 🔴 category 배열/문자열 처리
                if (rawRequest.TryGetValue("category", out object categoryObj))
                {
                    if (categoryObj is List<object> list && list.Count > 0)
                    {
                        request.category = list[0]?.ToString() ?? "";
                        for (int c = 0; c < list.Count; c++)
                        {
                            string cat = list[c]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(cat))
                                request.categories.Add(cat);
                        }
                    }
                    else if (categoryObj is string s)
                    {
                        request.category = s;
                        if (!string.IsNullOrWhiteSpace(s))
                            request.categories.Add(s);
                    }
                }

                if (!string.IsNullOrEmpty(request.requestID))
                    db.requests[request.requestID] = request;
            }

            Debug.Log($"[DrinkDB] requests loaded = {db.requests.Count}");
        }

        private void TryLoadIngredients(DrinkDatabase db, string ingredientsPath)
        {
            string json = ReadJsonFile(ingredientsPath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root) ||
                !root.TryGetValue("ingredients", out object ingredientsObj) || !(ingredientsObj is List<object> ingredients))
                return;

            for (int i = 0; i < ingredients.Count; i++)
            {
                string value = null;
                if (ingredients[i] is Dictionary<string, object> obj)
                {
                    if (obj.TryGetValue("id", out object idObj))
                        value = idObj?.ToString();

                    if (!string.IsNullOrEmpty(value))
                    {
                        var info = new IngredientData
                        {
                            id = value,
                            displayName = FirstNonEmpty(
                                GetString(obj, "displayName"),
                                FirstNonEmpty(
                                    GetString(obj, "name"),
                                    FirstNonEmpty(GetString(obj, "korean_name"), GetString(obj, "english_name"))
                                )
                            ),
                            description = FirstNonEmpty(GetString(obj, "description"), GetString(obj, "desc"))
                        };

                        if (string.IsNullOrWhiteSpace(info.displayName))
                            info.displayName = value.Replace("INGREDIENT_", string.Empty);

                        db.ingredientInfos[value] = info;
                    }
                }
                else
                {
                    value = ingredients[i]?.ToString();
                }

                if (!string.IsNullOrEmpty(value))
                    db.ingredients.Add(value);
            }
        }

        private static string ReadJsonFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[DrinkDB] File not found: {path}");
                return string.Empty;
            }

            return File.ReadAllText(path);
        }

        private static string GetString(Dictionary<string, object> data, string key)
            => data.TryGetValue(key, out object value) ? value?.ToString() ?? string.Empty : string.Empty;

        private static string FirstNonEmpty(string primary, string fallback)
            => !string.IsNullOrWhiteSpace(primary) ? primary : (fallback ?? string.Empty);

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
