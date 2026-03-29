using UnityEngine;
using UnityEngine.UI;

public class UIButtonSpriteToggle : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    [Header("Background Sprites")]
    [SerializeField] private Sprite offBackgroundSprite;
    [SerializeField] private Sprite onBackgroundSprite;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite offIconSprite;
    [SerializeField] private Sprite onIconSprite;

    [Header("Optional Disabled Sprites")]
    [SerializeField] private Sprite disabledBackgroundSprite;
    [SerializeField] private Sprite disabledIconSprite;

    [Header("State")]
    [SerializeField] private bool isOn;
    [SerializeField] private bool isInteractable = true;

    public bool IsOn => isOn;

    public void SetOn(bool value)
    {
        isOn = value;
        Apply();
    }

    public void Toggle()
    {
        isOn = !isOn;
        Apply();
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;

        if (button != null)
            button.interactable = value;

        Apply();
    }

    public void Apply()
    {
        if (!isInteractable)
        {
            if (backgroundImage != null)
                backgroundImage.sprite = disabledBackgroundSprite != null ? disabledBackgroundSprite : offBackgroundSprite;

            if (iconImage != null)
                iconImage.sprite = disabledIconSprite != null ? disabledIconSprite : offIconSprite;

            return;
        }

        if (backgroundImage != null)
            backgroundImage.sprite = isOn ? onBackgroundSprite : offBackgroundSprite;

        if (iconImage != null)
            iconImage.sprite = isOn ? onIconSprite : offIconSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply();
    }
#endif
}