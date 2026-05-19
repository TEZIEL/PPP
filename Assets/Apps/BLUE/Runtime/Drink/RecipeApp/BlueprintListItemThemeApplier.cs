using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN.RecipeApp
{
    public sealed class BlueprintListItemThemeApplier : MonoBehaviour
    {
        [Header("List Item Refs")]
        [SerializeField] private Image itemBackground;
        [SerializeField] private Image iconFrame;
        [SerializeField] private Image iconFrame2;
        [SerializeField] private Image iconFrame3;
        [SerializeField] private Button actionButton;
        [SerializeField] private DrinkListItemUI drinkListItemUI;



        [Header("Text Refs (Optional)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private TMP_Text metaText2;

        [Header("Drink Item Color Link")]
        [SerializeField, Range(0f, 1f)] private float hoverBackgroundMultiplier = 0.85f;
        [SerializeField, Range(0f, 1f)] private float selectedBackgroundMultiplier = 0.7f;
        [SerializeField] private Color activeTextFallbackColor = Color.white;

        private void OnEnable()
        {
            var manager = AppUIThemeManager.Instance;
            if (manager != null)
                manager.OnThemeChanged += HandleThemeChanged;

            ApplyCurrentTheme();
        }

        private void OnDisable()
        {
            var manager = AppUIThemeManager.Instance;
            if (manager != null)
                manager.OnThemeChanged -= HandleThemeChanged;
        }

        private void HandleThemeChanged()
        {
            ApplyCurrentTheme();
        }
        public void ApplyCurrentTheme()
        {
            var manager = AppUIThemeManager.Instance;
            if (manager == null || manager.CurrentTheme == null)
                return;

            ApplyTheme(manager.CurrentTheme);
        }

        public void ApplyTheme(AppUIThemeData data)
        {
            if (data == null)
                return;

            var t = data.blueprintListItem;

            if (itemBackground != null && t.itemBackgroundSprite != null)
                itemBackground.sprite = t.itemBackgroundSprite;

            if (iconFrame != null && t.iconFrameSprite != null)
                iconFrame.sprite = t.iconFrameSprite;

            if (iconFrame2 != null && t.iconFrame2Sprite != null)
                iconFrame2.sprite = t.iconFrame2Sprite;

            if (iconFrame3 != null && t.iconFrame3Sprite != null)
                iconFrame3.sprite = t.iconFrame3Sprite;

            if (actionButton != null && actionButton.targetGraphic is Image buttonImage && t.actionButtonSprite != null)
                buttonImage.sprite = t.actionButtonSprite;

            if (titleText != null)
                titleText.color = t.titleTextColor;

            if (bodyText != null)
                bodyText.color = t.bodyTextColor;

            if (metaText != null)
                metaText.color = t.metaTextColor;

            if (metaText2 != null)
                metaText2.color = t.metaTextColor;

            ApplyDrinkListItemThemeColors(t);
        }

        private void ApplyDrinkListItemThemeColors(AppUIThemeData.BlueprintListItemTheme t)
        {
            if (drinkListItemUI == null)
                drinkListItemUI = GetComponent<DrinkListItemUI>();

            if (drinkListItemUI == null)
                return;

            Color defaultBackground = itemBackground != null ? itemBackground.color : Color.white;
            Color hoverBackground = MultiplyRgb(defaultBackground, hoverBackgroundMultiplier);
            Color selectedBackground = MultiplyRgb(defaultBackground, selectedBackgroundMultiplier);
            Color defaultText = titleText != null ? titleText.color : t.titleTextColor;
            Color activeText = activeTextFallbackColor;
            if (activeText.a <= 0f)
                activeText = Color.white;

            drinkListItemUI.ApplyThemeColors(
                defaultBackground,
                hoverBackground,
                selectedBackground,
                defaultText,
                activeText);
        }

        private static Color MultiplyRgb(Color source, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(source.r * multiplier),
                Mathf.Clamp01(source.g * multiplier),
                Mathf.Clamp01(source.b * multiplier),
                source.a);
        }
    }
}
