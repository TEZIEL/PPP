namespace PPP.BLUE.VN.RecipeApp
{
    internal static class RecipeIngredientTextFormatter
    {
        public static string FormatIngredientDisplayName(string localizedName, string colorHex)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(colorHex))
                return localizedName;

            return $"<color={colorHex}>{localizedName}</color>";
        }
    }
}
