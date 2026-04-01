using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DesktopIconLauncher : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;

    [Header("App")]
    [SerializeField] private AppDefinition appDef;
    [SerializeField] private GameObject optionsModal;
    [SerializeField] private string optionsAppId = "app.options";
    [SerializeField] private string optionsIconId = "99";

    [Header("Optional Label")]
    [SerializeField] private TMP_Text iconLabel;

    [Header("Selection Visual (Optional)")]
    [SerializeField] private GameObject selectedVisual;

    [Header("Icon Tint (Optional)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Color32 normalIconColor = new Color32(0, 0, 0, 255);
    [SerializeField] private Color32 selectedIconColor = new Color32(128, 128, 184, 255);

    [Header("Double Click")]
    [SerializeField] private float doubleClickThreshold = 0.28f;
    [SerializeField] private float dragClickIgnoreSeconds = 0.15f;

    [Header("Cooldown")]
    [SerializeField] private float clickCooldownSeconds = 0.15f;

    private static DesktopIconLauncher activeSelection;

    private float lastClickTime = -999f;
    private float nextAllowedTime = 0f;
    private bool isThemeBound;

    private void Awake()
    {
        if (iconLabel != null && appDef != null)
            iconLabel.text = appDef.DisplayName;

        ResolveIconImageIfNeeded();

        if (selectedVisual != null)
            selectedVisual.SetActive(false);
    }

    private void OnEnable()
    {
        BindThemeEvents();
        ApplyCurrentTheme();
    }

    private void Start()
    {
        // ThemeManager 초기화 순서에 덜 민감하게 한 번 더 보정
        BindThemeEvents();
        ApplyCurrentTheme();
    }

    private void OnDisable()
    {
        UnbindThemeEvents();

        if (activeSelection == this)
            activeSelection = null;

        ResolveIconImageIfNeeded();
        ApplyIconColor(false);

        if (selectedVisual != null)
            selectedVisual.SetActive(false);
    }

    private void Update()
    {
        if (activeSelection == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (IsPointerOverAnyDesktopIcon()) return;

        SetActiveSelection(null);
    }

    private static bool IsPointerOverAnyDesktopIcon()
    {
        var es = EventSystem.current;
        if (es == null) return false;

        var pointerData = new PointerEventData(es) { position = Input.mousePosition };
        var results = new List<RaycastResult>(16);
        es.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go == null) continue;

            if (go.GetComponentInParent<DesktopIconLauncher>() != null)
                return true;
        }

        return false;
    }

    private static void SetActiveSelection(DesktopIconLauncher target)
    {
        if (activeSelection == target)
        {
            activeSelection?.SetSelectionVisual(true);
            return;
        }

        if (activeSelection != null)
            activeSelection.SetSelectionVisual(false);

        activeSelection = target;

        if (activeSelection != null)
            activeSelection.SetSelectionVisual(true);
    }

    private void SetSelectionVisual(bool selected)
    {
        ApplyIconColor(selected);

        if (selectedVisual != null)
            selectedVisual.SetActive(selected);
    }

    private void ResolveIconImageIfNeeded()
    {
        if (iconImage != null) return;

        var images = GetComponentsInChildren<Image>(includeInactive: true);
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            if (img == null) continue;

            if (selectedVisual != null && img.transform.IsChildOf(selectedVisual.transform))
                continue;

            iconImage = img;
            return;
        }
    }

    private void ApplyIconColor(bool selected)
    {
        ResolveIconImageIfNeeded();
        if (iconImage == null) return;

        iconImage.color = selected ? selectedIconColor : normalIconColor;
    }

    private void BindThemeEvents()
    {
        if (isThemeBound)
            return;

        var manager = ThemeManager.Instance;
        if (manager == null)
            return;

        manager.OnThemeApplied += HandleThemeChanged;
        isThemeBound = true;
    }

    private void UnbindThemeEvents()
    {
        if (!isThemeBound)
            return;

        var manager = ThemeManager.Instance;
        if (manager != null)
            manager.OnThemeApplied -= HandleThemeChanged;

        isThemeBound = false;
    }

    private void HandleThemeChanged()
    {
        ApplyCurrentTheme();
    }

    private void ApplyCurrentTheme()
    {
        ResolveIconImageIfNeeded();
        if (iconImage == null)
            return;

        var themeManager = ThemeManager.Instance;
        var theme = themeManager != null ? themeManager.CurrentTheme : null;

        if (theme != null)
        {
            normalIconColor = theme.desktopLauncherIconNormalTint;
            selectedIconColor = theme.desktopLauncherIconSelectedTint;
        }

        Sprite icon = ResolveThemedIconSprite();
        if (icon != null)
            iconImage.sprite = icon;

        bool isSelected = activeSelection == this;
        ApplyIconColor(isSelected);

        if (selectedVisual != null)
            selectedVisual.SetActive(isSelected);
    }

    private Sprite ResolveThemedIconSprite()
    {
        string appId = appDef != null ? appDef.AppId : null;
        var themeManager = ThemeManager.Instance;
        if (themeManager != null && themeManager.CurrentTheme != null && !string.IsNullOrWhiteSpace(appId))
        {
            var icons = themeManager.CurrentTheme.desktopLauncherIcons;
            if (icons != null)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    var entry = icons[i];
                    if (!string.Equals(entry.appId, appId, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.iconSprite != null)
                        return entry.iconSprite;
                }
            }
        }

        return appDef != null ? appDef.IconSprite : null;
    }

    private bool CanExecuteNow()
    {
        if (Time.unscaledTime < nextAllowedTime) return false;
        nextAllowedTime = Time.unscaledTime + clickCooldownSeconds;
        return true;
    }

    private bool IsDragRecent()
    {
        var drag = GetComponent<DesktopIconDraggable>();
        if (drag == null) return false;

        if (drag.IsDragging) return true;
        if (Time.unscaledTime - drag.LastDragEndTime < dragClickIgnoreSeconds) return true;

        return false;
    }

    private void ExecuteOpen()
    {
        if (!CanExecuteNow()) return;

        if (TryOpenOptions())
            return;

        if (windowManager == null || appDef == null)
            return;

        if (windowManager.IsOpen(appDef.AppId))
            return;

        windowManager.Open(appDef);
    }

    private bool TryOpenOptions()
    {
        if (optionsModal == null)
            return false;

        if (MatchesOptionsAppId() || MatchesOptionsIconId())
        {
            optionsModal.SetActive(true);
            return true;
        }

        return false;
    }

    private bool MatchesOptionsAppId()
    {
        if (string.IsNullOrWhiteSpace(optionsAppId))
            return false;

        if (appDef == null || string.IsNullOrWhiteSpace(appDef.AppId))
            return false;

        return string.Equals(appDef.AppId, optionsAppId, System.StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesOptionsIconId()
    {
        if (string.IsNullOrWhiteSpace(optionsIconId))
            return false;

        var drag = GetComponent<DesktopIconDraggable>();
        if (drag == null)
            return false;

        return string.Equals(drag.GetId(), optionsIconId, System.StringComparison.OrdinalIgnoreCase);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (IsDragRecent())
        {
            lastClickTime = -999f;
            return;
        }

        SetActiveSelection(this);

        float now = Time.unscaledTime;

        if (now - lastClickTime <= doubleClickThreshold)
        {
            lastClickTime = -999f;
            ExecuteOpen();
            return;
        }

        lastClickTime = now;
    }
}
