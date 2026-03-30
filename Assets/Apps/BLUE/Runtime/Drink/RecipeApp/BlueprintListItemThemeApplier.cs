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
        [SerializeField] private TMP_Text metaText2;

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
        }
    }
}
