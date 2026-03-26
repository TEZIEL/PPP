using System;
using System.Collections.Generic;
using PPP.BLUE.VN.DrinkSystem;
using UnityEngine;

namespace PPP.BLUE.VN.RecipeApp
{
    /// <summary>
    /// Resources 폴더의 JSON을 읽어 레시피 앱에서 쓰는 단순 모델로 변환한다.
    /// </summary>
    public sealed class RecipeDataLoader : MonoBehaviour
    {
        [Header("파일 경로 (Assets 기준)")]
        [SerializeField] private string ingredientsFolderFromAssets = "Data/Drink/Ingredients";
        [SerializeField] private string ingredientsFileName = "ingredients.json";
        [SerializeField] private string drinksFolderFromAssets = "Data/Drink/Drinks";
        [SerializeField] private string drinksFileName = "drinks.json";

        [Header("Fallback: Resources 경로 (확장자 제외)")]
        [SerializeField] private string ingredientsResourcePath = "DrinkData/ingredients";
        [SerializeField] private string drinksResourcePath = "DrinkData/drinks";

        public IngredientRoot LoadIngredients()
        {
            string json = ReadJsonFromAssets(ingredientsFolderFromAssets, ingredientsFileName);
            if (string.IsNullOrWhiteSpace(json))
            {
                // 폴더 기반 로딩 실패 시 Resources로 한 번 더 시도한다.
                var text = Resources.Load<TextAsset>(ingredientsResourcePath);
                if (text != null)
                    json = text.text;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[RecipeDataLoader] ingredients json not found. folder={ingredientsFolderFromAssets}, file={ingredientsFileName}, resource={ingredientsResourcePath}");
                return new IngredientRoot();
            }

            var root = ParseIngredients(json);
            return root ?? new IngredientRoot();
        }

        public DrinkRoot LoadDrinks()
        {
            string json = ReadJsonFromAssets(drinksFolderFromAssets, drinksFileName);
            if (string.IsNullOrWhiteSpace(json))
            {
                // 폴더 기반 로딩 실패 시 Resources로 한 번 더 시도한다.
                var text = Resources.Load<TextAsset>(drinksResourcePath);
                if (text != null)
                    json = text.text;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[RecipeDataLoader] drinks json not found. folder={drinksFolderFromAssets}, file={drinksFileName}, resource={drinksResourcePath}");
                return new DrinkRoot();
            }

            var root = ParseDrinks(json);
            return root ?? new DrinkRoot();
        }

        private static IngredientRoot ParseIngredients(string json)
        {
            var result = new IngredientRoot();
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root) ||
                !root.TryGetValue("ingredients", out var ingredientsObj) ||
                !(ingredientsObj is List<object> rawIngredients))
            {
                return result;
            }

            for (int i = 0; i < rawIngredients.Count; i++)
            {
                if (!(rawIngredients[i] is Dictionary<string, object> row))
                    continue;

                var item = new IngredientEntry
                {
                    id = GetString(row, "id"),
                    name = GetString(row, "name"),
                    english_name = GetString(row, "english_name"),
                    color = GetString(row, "color"),
                    taste = GetString(row, "taste"),
                    description = GetString(row, "description")
                };

                if (!string.IsNullOrWhiteSpace(item.id))
                    result.ingredients.Add(item);
            }

            return result;
        }

        private static DrinkRoot ParseDrinks(string json)
        {
            var result = new DrinkRoot();
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root) ||
                !root.TryGetValue("drinks", out var drinksObj) ||
                !(drinksObj is List<object> rawDrinks))
            {
                return result;
            }

            for (int i = 0; i < rawDrinks.Count; i++)
            {
                if (!(rawDrinks[i] is Dictionary<string, object> row))
                    continue;

                var drink = new DrinkEntry
                {
                    id = GetString(row, "id"),
                    name = GetString(row, "name"),
                    description = GetString(row, "description"),
                    imageKey = GetString(row, "imageKey"),
                    category = GetString(row, "category"),
                    artheon_addable = GetBool(row, "artheon_addable"),
                    total = GetInt(row, "total")
                };

                if (row.TryGetValue("tags", out var tagsObj) && tagsObj is List<object> tags)
                {
                    for (int t = 0; t < tags.Count; t++)
                    {
                        var tag = tags[t]?.ToString();
                        if (!string.IsNullOrWhiteSpace(tag))
                            drink.tags.Add(tag);
                    }
                }

                // 핵심: ingredients 는 객체 형태이므로 key 목록만 추출한다.
                if (row.TryGetValue("ingredients", out var ingredientsObj) && ingredientsObj is Dictionary<string, object> ingredientMap)
                {
                    foreach (var pair in ingredientMap)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Key))
                            continue;

                        int amount = GetIntValue(pair.Value);
                        drink.ingredientAmounts[pair.Key] = amount;
                        drink.ingredientKeys.Add(pair.Key);
                    }
                }

                if (!string.IsNullOrWhiteSpace(drink.id))
                    result.drinks.Add(drink);
            }

            return result;
        }


        private static string ReadJsonFromAssets(string folderFromAssets, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderFromAssets) || string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            string folderPath = System.IO.Path.Combine(Application.dataPath, folderFromAssets);
            string fullPath = System.IO.Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(fullPath))
                return string.Empty;

            return System.IO.File.ReadAllText(fullPath);
        }

        private static int GetIntValue(object value)
        {
            if (value == null)
                return 0;

            if (value is long l)
                return (int)l;
            if (value is int i)
                return i;
            if (value is double d)
                return Mathf.RoundToInt((float)d);

            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }
        private static string GetString(Dictionary<string, object> map, string key)
            => map.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

        private static bool GetBool(Dictionary<string, object> map, string key)
        {
            if (!map.TryGetValue(key, out var value) || value == null)
                return false;

            if (value is bool b)
                return b;

            return bool.TryParse(value.ToString(), out var parsed) && parsed;
        }

        private static int GetInt(Dictionary<string, object> map, string key)
        {
            if (!map.TryGetValue(key, out var value) || value == null)
                return 0;

            if (value is long l)
                return (int)l;
            if (value is int i)
                return i;
            if (value is double d)
                return Mathf.RoundToInt((float)d);

            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }
    }
}
