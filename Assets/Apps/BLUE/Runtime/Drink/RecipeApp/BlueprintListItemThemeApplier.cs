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
        [SerializeField] private Button actionButton;

        [Header("Text Refs (Optional)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text metaText;

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

            if (actionButton != null && actionButton.targetGraphic is Image buttonImage && t.actionButtonSprite != null)
                buttonImage.sprite = t.actionButtonSprite;

            if (titleText != null)
                titleText.color = t.titleTextColor;

            if (bodyText != null)
                bodyText.color = t.bodyTextColor;

            if (metaText != null)
                metaText.color = t.metaTextColor;
        }
    }
}
