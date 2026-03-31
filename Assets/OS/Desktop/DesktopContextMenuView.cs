using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DesktopContextMenuView : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button alignIconsButton;
    [SerializeField] private Button toggleLayoutButton;
    [SerializeField] private Button resetWindowsButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createFileButton;

    [Header("Text Colors")]
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color highlightedTextColor = Color.white;
    [SerializeField] private Color pressedTextColor = Color.white;

    [Header("Button Tint Fallback")]
    [SerializeField] private Color buttonNormalTint = Color.white;
    [SerializeField] private Color buttonSelectedTint = new Color32(128, 128, 184, 255);
    [SerializeField] private Color buttonPressedTint = new Color32(180, 180, 180, 255);
    [SerializeField] private Color buttonDisabledTint = new Color32(180, 180, 180, 128);

    public event Action OnAlignIcons;
    public event Action OnToggleLayout;
    public event Action OnResetWindows;
    public event Action OnRefresh;
    public event Action OnCreateFile;

    private sealed class ButtonVisualState
    {
        public Button button;
        public TMP_Text text;
        public bool isHovered;
        public bool isPressed;
    }

    private readonly System.Collections.Generic.List<ButtonVisualState> visualStates =
        new System.Collections.Generic.List<ButtonVisualState>(5);

    private void Awake()
    {
        RegisterButton(alignIconsButton, () => OnAlignIcons?.Invoke());
        RegisterButton(toggleLayoutButton, () => OnToggleLayout?.Invoke());
        RegisterButton(resetWindowsButton, () => OnResetWindows?.Invoke());
        RegisterButton(refreshButton, () => OnRefresh?.Invoke());
        RegisterButton(createFileButton, () => OnCreateFile?.Invoke());

        ResetTextVisualState();
        ApplyCurrentTheme();
    }

    private void OnEnable()
    {
        var manager = ThemeManager.Instance;
        if (manager != null)
            manager.OnThemeApplied += HandleThemeChanged;

        ApplyCurrentTheme();
    }

    private void OnDisable()
    {
        var manager = ThemeManager.Instance;
        if (manager != null)
            manager.OnThemeApplied -= HandleThemeChanged;
    }

    public void ResetTextVisualState()
    {
        for (int i = 0; i < visualStates.Count; i++)
        {
            var state = visualStates[i];
            state.isHovered = false;
            state.isPressed = false;
            ApplyTextColor(state);
        }
    }

    private void RegisterButton(Button button, Action onClick)
    {
        if (button == null) return;

        if (onClick != null)
            button.onClick.AddListener(() => onClick.Invoke());

        var text = button.GetComponentInChildren<TMP_Text>(true);
        var state = new ButtonVisualState
        {
            button = button,
            text = text,
            isHovered = false,
            isPressed = false
        };

        visualStates.Add(state);
        AddPointerTriggers(state);
        ApplyButtonTint(state);
        ApplyTextColor(state);
    }

    private void AddPointerTriggers(ButtonVisualState state)
    {
        if (state == null || state.button == null) return;

        var trigger = state.button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = state.button.gameObject.AddComponent<EventTrigger>();
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
        {
            state.isHovered = true;
            ApplyTextColor(state);
        });

        AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
        {
            state.isHovered = false;
            state.isPressed = false;
            ApplyTextColor(state);
        });

        AddTrigger(trigger, EventTriggerType.PointerDown, data =>
        {
            if (data is PointerEventData ped && ped.button != PointerEventData.InputButton.Left) return;
            state.isPressed = true;
            ApplyTextColor(state);
        });

        AddTrigger(trigger, EventTriggerType.PointerUp, _ =>
        {
            state.isPressed = false;
            ApplyTextColor(state);
        });
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        if (trigger == null || callback == null) return;

        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void ApplyTextColor(ButtonVisualState state)
    {
        if (state == null || state.text == null) return;

        if (state.isPressed)
            state.text.color = pressedTextColor;
        else if (state.isHovered)
            state.text.color = highlightedTextColor;
        else
            state.text.color = normalTextColor;
    }

    private void HandleThemeChanged()
    {
        ApplyCurrentTheme();
    }

    private void ApplyCurrentTheme()
    {
        var manager = ThemeManager.Instance;
        if (manager != null && manager.CurrentTheme != null)
        {
            var theme = manager.CurrentTheme;
            if (IsConfiguredTint(theme.desktopContextMenuButtonNormalTint))
                buttonNormalTint = theme.desktopContextMenuButtonNormalTint;
            if (IsConfiguredTint(theme.desktopContextMenuButtonSelectedTint))
                buttonSelectedTint = theme.desktopContextMenuButtonSelectedTint;
            if (IsConfiguredTint(theme.desktopContextMenuButtonPressedTint))
                buttonPressedTint = theme.desktopContextMenuButtonPressedTint;
            if (IsConfiguredTint(theme.desktopContextMenuButtonDisabledTint))
                buttonDisabledTint = theme.desktopContextMenuButtonDisabledTint;
        }

        for (int i = 0; i < visualStates.Count; i++)
        {
            ApplyButtonTint(visualStates[i]);
            ApplyTextColor(visualStates[i]);
        }
    }

    private void ApplyButtonTint(ButtonVisualState state)
    {
        if (state == null || state.button == null)
            return;

        var colors = state.button.colors;
        colors.normalColor = buttonNormalTint;
        colors.selectedColor = buttonSelectedTint;
        colors.pressedColor = buttonPressedTint;
        colors.disabledColor = buttonDisabledTint;
        state.button.colors = colors;
    }

    private static bool IsConfiguredTint(Color color)
    {
        return color.a > 0f;
    }
}
