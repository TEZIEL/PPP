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
