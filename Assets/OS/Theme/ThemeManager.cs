using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [SerializeField] private ThemeData currentTheme;
    [Header("Taskbar Surface")]
    [SerializeField] private Image taskbarBackgroundImage;
    [SerializeField] private List<Image> statusBarPanels = new();

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

        ResolveTaskbarReferencesIfNeeded();

        if (taskbarBackgroundImage != null)
            taskbarBackgroundImage.color = currentTheme.taskbarBg;

        for (int i = 0; i < statusBarPanels.Count; i++)
        {
            if (statusBarPanels[i] == null) continue;
            statusBarPanels[i].color = currentTheme.statusBarTint;
        }

        var taskbarManager = FindObjectOfType<TaskbarManager>();
        taskbarManager?.ApplyThemeToAllButtons(this);

        var windowManager = FindObjectOfType<WindowManager>();
        windowManager?.ApplyThemeToOpenWindows(this);
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

    private void ResolveTaskbarReferencesIfNeeded()
    {
        if (taskbarBackgroundImage == null)
        {
            var bg = GameObject.Find("TaskbarBG");
            if (bg != null) taskbarBackgroundImage = bg.GetComponent<Image>();
        }

        if (statusBarPanels == null) statusBarPanels = new List<Image>();

        if (statusBarPanels.Count > 0) return;

        var candidates = FindObjectsOfType<Image>();
        for (int i = 0; i < candidates.Length; i++)
        {
            var img = candidates[i];
            if (img == null) continue;
            if (img.name.StartsWith("BarTask")) statusBarPanels.Add(img);
        }
    }
}
