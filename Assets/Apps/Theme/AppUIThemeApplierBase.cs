using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class AppUIThemeApplierBase : MonoBehaviour
{
    protected static void ApplyImageSprite(Image target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        target.sprite = sprite;
    }

    protected static void ApplyButtonSprite(Button target, Sprite sprite)
    {
        if (target == null || sprite == null) return;
        if (target.targetGraphic is Image image)
            image.sprite = sprite;
    }

    protected static void ApplyScrollbarSprites(Scrollbar target, Sprite trackSprite, Sprite handleSprite)
    {
        if (target == null) return;

        var trackImage = target.GetComponent<Image>();
        if (trackImage != null && trackSprite != null)
            trackImage.sprite = trackSprite;

        if (target.handleRect != null)
        {
            var handleImage = target.handleRect.GetComponent<Image>();
            if (handleImage != null && handleSprite != null)
                handleImage.sprite = handleSprite;
        }
    }

    protected static void ApplyDropdownSprites(TMP_Dropdown target, Sprite backgroundSprite, Sprite buttonSprite)
    {
        if (target == null) return;

        if (target.targetGraphic is Image bg && backgroundSprite != null)
            bg.sprite = backgroundSprite;

        if (target.captionImage != null && buttonSprite != null)
            target.captionImage.sprite = buttonSprite;
    }

    protected static void ApplyTextColor(TMP_Text target, Color color)
    {
        if (target == null) return;
        target.color = color;
    }

    public abstract void ApplyFromManager(AppUIThemeData data, string appId);
}
