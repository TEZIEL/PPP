using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DropdownItemVisual : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image checkmarkImage;

    [Header("Background Sprites")]
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Sprite pressedBackgroundSprite;
    [SerializeField] private Sprite disabledBackgroundSprite;

    [Header("Checkmark Sprites")]
    [SerializeField] private Sprite normalCheckmarkSprite;
    [SerializeField] private Sprite selectedCheckmarkSprite;
    [SerializeField] private Sprite pressedCheckmarkSprite;
    [SerializeField] private Sprite disabledCheckmarkSprite;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.black;
    [SerializeField] private Color pressedTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = Color.gray;

    private bool isSelected;
    private bool isPointerOver;
    private bool isPointerDown;
    private bool interactable = true;

    private void Awake()
    {
        ApplyVisualState();

        var theme = AppUIThemeManager.Instance?.CurrentTheme;
        if (theme != null)
        {
            var t = theme.bridge;

            SetBackgroundSprites(
    t.dropdownItemNormalSprite,
    t.dropdownItemSelectedSprite,
    t.dropdownItemPressedSprite,
    t.dropdownItemDisabledSprite
);

            SetCheckmarkSprites(
                t.dropdownCheckmarkNormalSprite,
                t.dropdownCheckmarkSelectedSprite,
                t.dropdownCheckmarkPressedSprite,
                t.dropdownCheckmarkDisabledSprite
            );
        }
    }

    // =========================
    // 외부 제어
    // =========================
    public void SetSelected(bool value)
    {
        isSelected = value;
        ApplyVisualState();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        ApplyVisualState();
    }

    // =========================
    // Pointer
    // =========================
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isPointerDown = false;
        ApplyVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        ApplyVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        ApplyVisualState();
    }

    // =========================
    // 핵심 상태 처리
    // =========================
    private void ApplyVisualState()
    {
        Sprite bg;
        Sprite mark;
        Color txt;

        if (!interactable)
        {
            bg = disabledBackgroundSprite ?? normalBackgroundSprite;
            mark = disabledCheckmarkSprite ?? normalCheckmarkSprite;
            txt = disabledTextColor;
        }
        else if (isPointerDown)
        {
            bg = pressedBackgroundSprite ?? selectedBackgroundSprite;
            mark = pressedCheckmarkSprite ?? selectedCheckmarkSprite;
            txt = pressedTextColor;
        }
        else if (isSelected || isPointerOver)
        {
            bg = selectedBackgroundSprite ?? normalBackgroundSprite;
            mark = selectedCheckmarkSprite ?? normalCheckmarkSprite;
            txt = selectedTextColor;
        }
        else
        {
            bg = normalBackgroundSprite;
            mark = normalCheckmarkSprite;
            txt = normalTextColor;
        }

        if (background != null)
            background.sprite = bg;

        if (label != null)
            label.color = txt;

        if (checkmarkImage != null)
            checkmarkImage.sprite = mark;
    }

    // =========================
    // 테마 setter
    // =========================
    public void SetBackgroundSprites(Sprite normal, Sprite selected, Sprite pressed, Sprite disabled)
    {
        if (normal != null) normalBackgroundSprite = normal;
        if (selected != null) selectedBackgroundSprite = selected;
        if (pressed != null) pressedBackgroundSprite = pressed;
        if (disabled != null) disabledBackgroundSprite = disabled;

        ApplyVisualState();
    }

    public void SetCheckmarkSprites(Sprite normal, Sprite selected, Sprite pressed, Sprite disabled)
    {
        if (normal != null) normalCheckmarkSprite = normal;
        if (selected != null) selectedCheckmarkSprite = selected;
        if (pressed != null) pressedCheckmarkSprite = pressed;
        if (disabled != null) disabledCheckmarkSprite = disabled;

        ApplyVisualState();
    }
}