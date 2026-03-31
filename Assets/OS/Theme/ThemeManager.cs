using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }
    public event System.Action OnThemeApplied;

    [SerializeField] private ThemeData currentTheme;
    [Header("Taskbar Surface")]
    [SerializeField] private Image taskbarBackgroundImage;
    [SerializeField] private Image taskbarPanelImage;
    [SerializeField] private Image startButtonImage;
    [SerializeField] private Image clockPanelImage;
    [SerializeField] private Image taskbarSeparatorImage;
    [SerializeField] private List<Image> extraTaskbarPanelImages = new();
    [SerializeField] private Image otherWindow1RootImage;
    [SerializeField] private Image otherWindow2RootImage;
    [SerializeField] private Image otherWindow3RootImage;
    [SerializeField] private Image otherWindow4RootImage;
    [SerializeField] private Image otherWindow5RootImage;
    [SerializeField] private Image otherWindow6RootImage;
    [SerializeField] private Image otherWindow7RootImage;
    [SerializeField] private Image otherWindow8RootImage;
    [SerializeField] private Image otherWindow9RootImage;
    [SerializeField] private Image otherWindow10RootImage;
    [SerializeField] private Image otherWindow11RootImage;
    [SerializeField] private Image otherWindow12RootImage;
    [SerializeField] private Image otherWindow13RootImage;
    [SerializeField] private Image otherWindow14RootImage;
    [SerializeField] private Image otherWindow15RootImage;

    public ThemeData CurrentTheme => currentTheme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyTheme();
    }

    public void SetTheme(ThemeData theme, bool applyImmediately = true)
    {
        currentTheme = theme;
        if (applyImmediately) ApplyTheme();
    }

    public void ApplyTheme()
    {
        if (currentTheme == null) return;
        ApplySpriteIfPresent(taskbarBackgroundImage, currentTheme.taskbarBackgroundSprite);
        ApplySpriteIfPresent(taskbarPanelImage, currentTheme.taskbarPanelSprite);
        ApplySpriteIfPresent(startButtonImage, currentTheme.startButtonSprite);
        ApplySpriteIfPresent(clockPanelImage, currentTheme.clockPanelSprite);
        ApplySpriteIfPresent(taskbarSeparatorImage, currentTheme.taskbarSeparatorSprite);
        ApplySpriteIfPresent(otherWindow1RootImage, currentTheme.otherWindow1RootSprite);
        ApplySpriteIfPresent(otherWindow2RootImage, currentTheme.otherWindow2RootSprite);
        ApplySpriteIfPresent(otherWindow3RootImage, currentTheme.otherWindow3RootSprite);
        ApplySpriteIfPresent(otherWindow4RootImage, currentTheme.otherWindow4RootSprite);
        ApplySpriteIfPresent(otherWindow5RootImage, currentTheme.otherWindow5RootSprite);
        ApplySpriteIfPresent(otherWindow6RootImage, currentTheme.otherWindow6RootSprite);
        ApplySpriteIfPresent(otherWindow7RootImage, currentTheme.otherWindow7RootSprite);
        ApplySpriteIfPresent(otherWindow8RootImage, currentTheme.otherWindow8RootSprite);
        ApplySpriteIfPresent(otherWindow9RootImage, currentTheme.otherWindow9RootSprite);
        ApplySpriteIfPresent(otherWindow10RootImage, currentTheme.otherWindow10RootSprite);
        ApplySpriteIfPresent(otherWindow11RootImage, currentTheme.otherWindow11RootSprite);
        ApplySpriteIfPresent(otherWindow12RootImage, currentTheme.otherWindow12RootSprite);
        ApplySpriteIfPresent(otherWindow13RootImage, currentTheme.otherWindow13RootSprite);
        ApplySpriteIfPresent(otherWindow14RootImage, currentTheme.otherWindow14RootSprite);
        ApplySpriteIfPresent(otherWindow15RootImage, currentTheme.otherWindow15RootSprite);

        for (int i = 0; i < extraTaskbarPanelImages.Count; i++)
            ApplySpriteIfPresent(extraTaskbarPanelImages[i], currentTheme.taskbarPanelSprite);

        var taskbarManager = FindObjectOfType<TaskbarManager>();
        taskbarManager?.ApplyThemeToAllButtons(this);

        var windowManager = FindObjectOfType<WindowManager>();
        windowManager?.ApplyThemeToOpenWindows(this);

        OnThemeApplied?.Invoke();
    }

    public void ApplyThemeToWindow(WindowController window)
    {
        if (window == null) return;
        window.ApplyTheme(currentTheme);
    }

    public void ApplyThemeToTaskbarButton(TaskbarButtonController button)
    {
        if (button == null) return;
        button.ApplyTheme(currentTheme);
    }

    private static void ApplySpriteIfPresent(Image image, Sprite sprite)
    {
        if (image == null) return;
        if (sprite == null) return;
        image.sprite = sprite;
    }
}
