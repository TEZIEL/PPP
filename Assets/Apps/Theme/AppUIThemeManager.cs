using UnityEngine;
using System;

public class AppUIThemeManager : MonoBehaviour
{
    public static AppUIThemeManager Instance { get; private set; }
    public event Action OnThemeChanged;

    [SerializeField] private AppUIThemeData currentTheme;

    public AppUIThemeData CurrentTheme => currentTheme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetTheme(AppUIThemeData theme)
    {
        currentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    public void ApplyThemeToContent(string appId, Transform contentRoot)
    {
        if (currentTheme == null || contentRoot == null || string.IsNullOrWhiteSpace(appId))
            return;

        var appliers = contentRoot.GetComponentsInChildren<AppUIThemeApplierBase>(true);
        for (int i = 0; i < appliers.Length; i++)
            appliers[i].ApplyFromManager(currentTheme, appId);
    }
}
