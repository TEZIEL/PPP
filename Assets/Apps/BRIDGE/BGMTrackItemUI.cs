using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BGMTrackItemUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject checkmarkObject;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Button rootButton;

    [Header("State Sprites - Background")]
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Sprite pressedBackgroundSprite;
    [SerializeField] private Sprite disabledBackgroundSprite;

    [Header("State Sprites - Checkmark")]
    [SerializeField] private Sprite normalCheckmarkSprite;
    [SerializeField] private Sprite selectedCheckmarkSprite;
    [SerializeField] private Sprite pressedCheckmarkSprite;
    [SerializeField] private Sprite disabledCheckmarkSprite;

    [Header("State Colors - Text")]
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.black;
    [SerializeField] private Color pressedTextColor = Color.black;
    [SerializeField] private Color disabledTextColor = Color.gray;

    [Header("Behavior")]
    [SerializeField] private float doubleClickThreshold = 0.25f;
    [SerializeField] private bool interactable = true;

    private BGMTrackData boundTrack;
    private float lastClickTime = -999f;
    private bool isPointerDown;
    private bool isPointerOver;
    private bool isSelected;
    private bool isPlaying;

    public event Action<BGMTrackItemUI> OnClicked;
    public BGMTrackData BoundTrack => boundTrack;

    public void Bind(BGMTrackData track)
    {
        boundTrack = track;

        if (titleText != null)
            titleText.text = track != null ? track.displayName : string.Empty;

        isSelected = false;
        isPointerDown = false;
        isPointerOver = false;
        isPlaying = false;

        UpdateCheckmarkVisibility();
        ApplyVisualState();
        ApplyCurrentTheme();
    }

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

    public void SetTextColors(Color normal, Color selected, Color pressed, Color disabled)
    {
        normalTextColor = normal;
        selectedTextColor = selected;
        pressedTextColor = pressed;
        disabledTextColor = disabled;
        ApplyVisualState();
    }


    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisualState();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (rootButton != null)
            rootButton.interactable = value;

        ApplyVisualState();
    }

    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
        UpdateCheckmarkVisibility();
        ApplyVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable || boundTrack == null)
            return;

        SetSelected(true);
        OnClicked?.Invoke(this);

        float now = Time.unscaledTime;
        if (now - lastClickTime <= doubleClickThreshold)
        {
            PlayBoundTrack();
            lastClickTime = -999f;
        }
        else
        {
            lastClickTime = now;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
            return;

        isPointerDown = true;
        ApplyVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable)
            return;

        isPointerDown = false;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable)
            return;

        isPointerOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable)
            return;

        isPointerOver = false;
        isPointerDown = false;
        ApplyVisualState();
    }

    private void PlayBoundTrack()
    {
        if (boundTrack == null)
            return;

        BGMManager.Instance?.PlayTrackByDoubleClick(boundTrack);
    }

    private void ApplyVisualState()
    {
        Sprite bgSprite;
        Sprite markSprite = null;
        Color txtColor;

        if (!interactable)
        {
            bgSprite = disabledBackgroundSprite != null ? disabledBackgroundSprite : normalBackgroundSprite;
            markSprite = disabledCheckmarkSprite != null ? disabledCheckmarkSprite : normalCheckmarkSprite;
            txtColor = disabledTextColor;
        }
        else if (isPointerDown)
        {
            bgSprite = pressedBackgroundSprite != null ? pressedBackgroundSprite : selectedBackgroundSprite;
            markSprite = pressedCheckmarkSprite != null ? pressedCheckmarkSprite : selectedCheckmarkSprite;
            txtColor = pressedTextColor;
        }
        else if (isSelected || isPointerOver)
        {
            bgSprite = selectedBackgroundSprite != null ? selectedBackgroundSprite : normalBackgroundSprite;
            markSprite = selectedCheckmarkSprite != null ? selectedCheckmarkSprite : normalCheckmarkSprite;
            txtColor = selectedTextColor;
        }
        else
        {
            bgSprite = normalBackgroundSprite;
            markSprite = normalCheckmarkSprite;
            txtColor = normalTextColor;
        }

        if (backgroundImage != null)
            backgroundImage.sprite = bgSprite;

        if (titleText != null)
            titleText.color = txtColor;

        if (checkmarkImage != null && checkmarkObject != null && checkmarkObject.activeSelf)
            checkmarkImage.sprite = markSprite;
    }


    public void ApplyCurrentTheme()
    {
        var theme = AppUIThemeManager.Instance?.CurrentTheme;
        if (theme == null)
            return;

        var t = theme.bridge;

        SetBackgroundSprites(
            t.trackItemNormalSprite,
            t.trackItemSelectedSprite,
            t.trackItemPressedSprite,
            t.trackItemDisabledSprite
        );

        SetCheckmarkSprites(
            t.trackCheckmarkNormalSprite,
            t.trackCheckmarkSelectedSprite,
            t.trackCheckmarkPressedSprite,
            t.trackCheckmarkDisabledSprite
        );

        SetTextColors(
            t.trackTextNormalColor,
            t.trackTextSelectedColor,
            t.trackTextPressedColor,
            t.trackTextDisabledColor
        );
    }

    private void UpdateCheckmarkVisibility()
    {
        if (checkmarkObject != null)
            checkmarkObject.SetActive(isPlaying);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (rootButton == null)
            rootButton = GetComponent<Button>();

        if (checkmarkObject != null && checkmarkImage == null)
            checkmarkImage = checkmarkObject.GetComponent<Image>();

        if (Application.isPlaying)
            return;

        UpdateCheckmarkVisibility();
        ApplyVisualState();
    }
#endif
}