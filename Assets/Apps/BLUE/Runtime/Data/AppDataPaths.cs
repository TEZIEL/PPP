using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AppDataPaths
{
    // VN
    public const string VN_RUNTIME = "VN/";
    // StreamingAssets 내부 기준 (Application.streamingAssetsPath + VN_RUNTIME)

    // Drink
    public const string DRINK_ROOT = "Data/Drink/";

    public const string DRINK_INGREDIENTS = DRINK_ROOT + "Ingredients/ingredients.json";
    public const string DRINK_DRINKS = DRINK_ROOT + "Drinks/drinks.json";
    public const string DRINK_REQUESTS = DRINK_ROOT + "Requests/requests.json";
}
