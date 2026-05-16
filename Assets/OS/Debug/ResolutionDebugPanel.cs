using System.Collections;
using System.Text;
using PPP.BLUE.VN;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temporary build-only resolution test helper for verifying that the fake OS UI scales from the
/// 1920x1080 reference canvas and that opened windows remain inside the visible canvas after a resize.
/// Attach this to a debug panel object and wire the public methods to UI Button OnClick events.
/// </summary>
public sealed class ResolutionDebugPanel : MonoBehaviour
{
    private static readonly Vector2 ExpectedReferenceResolution = new Vector2(1920f, 1080f);
    private const float ExpectedMatchWidthOrHeight = 0.5f;

    [Header("Canvas Diagnostics")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private CanvasScaler mainCanvasScaler;
    [SerializeField] private RectTransform mainCanvasRoot;

    [Header("Fixed Aspect Viewport")]
    [Tooltip("Optional 16:9 viewport controller. If assigned, resolution diagnostics refresh and log it after screen changes.")]
    [SerializeField] private FixedAspectViewport fixedAspectViewport;

    [Header("Layout Refresh")]
    [Tooltip("Optional layout roots to rebuild after Screen.SetResolution. If empty, the main canvas root is used.")]
    [SerializeField] private RectTransform[] layoutRebuildRoots;

    [Header("Window Clamp")]
    [Tooltip("Optional explicit WindowManager reference. If empty, the scene WindowManager is found at runtime.")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private bool refreshWindowManagerClamp = true;
    [SerializeField] private bool refreshClampedDragPanels = true;

    [Header("Tracked UI Roots")]
    [Tooltip("Roots whose localScale, sizeDelta, anchoredPosition, rect, and world corners are logged before/after each resolution change.")]
    [SerializeField] private RectTransform[] trackedRoots;
    [SerializeField] private bool autoFindTrackedRootsByName = true;
    [SerializeField]
    private string[] trackedRootNames =
    {
        "BackgroundBG",
        "TaskbarBG",
        "DesktopIconBG",
        "WindowRoot",
        "OptionsModal",
        "VNRoot",
        "VN UI",
        "VNCanvas",
        "Music",
        "Recipe",
        "Fidget"
    };

    private Coroutine applyResolutionRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (mainCanvas != null && mainCanvasScaler == null)
            mainCanvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        if (mainCanvas != null && mainCanvasRoot == null)
            mainCanvasRoot = mainCanvas.transform as RectTransform;
    }

    // Button Methods: Windowed
    public void Set1280x720Windowed() => RequestResolution(1280, 720, FullScreenMode.Windowed, "1280x720 Windowed");

    public void Set1600x900Windowed() => RequestResolution(1600, 900, FullScreenMode.Windowed, "1600x900 Windowed");

    public void Set1920x1080Windowed() => RequestResolution(1920, 1080, FullScreenMode.Windowed, "1920x1080 Windowed");

    public void Set2560x1440Windowed() => RequestResolution(2560, 1440, FullScreenMode.Windowed, "2560x1440 Windowed");

    // Button Methods: Exclusive Fullscreen
    // [EXCLUSIVE_TEST] Exclusive fullscreen is intentionally exposed only for build diagnostics.
    // It can flicker, destabilize Alt+Tab, or be substituted by the OS/GPU driver on some Windows setups.
    public void Set1280x720ExclusiveFullscreen() => RequestResolution(1280, 720, FullScreenMode.ExclusiveFullScreen, "1280x720 Exclusive Fullscreen");

    public void Set1600x900ExclusiveFullscreen() => RequestResolution(1600, 900, FullScreenMode.ExclusiveFullScreen, "1600x900 Exclusive Fullscreen");

    public void Set1920x1080ExclusiveFullscreen() => RequestResolution(1920, 1080, FullScreenMode.ExclusiveFullScreen, "1920x1080 Exclusive Fullscreen");

    public void Set2560x1440ExclusiveFullscreen() => RequestResolution(2560, 1440, FullScreenMode.ExclusiveFullScreen, "2560x1440 Exclusive Fullscreen");

    // Button Methods: Auxiliary
    // Auxiliary deployment-candidate check: use native monitor size with borderless fullscreen.
    // This is not part of the core per-resolution exclusive fullscreen comparison matrix.
    public void SetBorderlessFullscreenNative()
    {
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;

        if (width <= 0 || height <= 0)
        {
            Resolution current = Screen.currentResolution;
            width = current.width;
            height = current.height;
        }

        if (width <= 0 || height <= 0)
        {
            width = Screen.width;
            height = Screen.height;
        }

        RequestResolution(width, height, FullScreenMode.FullScreenWindow, "Borderless Fullscreen Native");
    }

    public void SetBackTo1920x1080Windowed() => RequestResolution(1920, 1080, FullScreenMode.Windowed, "Back to 1920x1080 Windowed");

    public void LogCurrentResolutionDiagnostics()
    {
        ResolveReferences();
        RefreshFixedAspectViewport("Manual Diagnostics");
        Debug.Log(BuildResolutionLog("Manual Diagnostics", Screen.width, Screen.height, Screen.fullScreenMode));
        LogAllTrackedRoots("Manual Diagnostics");
        ValidateCanvasScalers();
    }

    public void LogSupportedResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0)
        {
            Debug.Log("[ResolutionDebugPanel] SupportedResolutions=<none reported by Screen.resolutions>");
            return;
        }

        var sb = new StringBuilder(1024);
        sb.Append("[ResolutionDebugPanel] SupportedResolutions count=").Append(resolutions.Length);
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            sb.Append("\n  [").Append(i).Append("] ")
                .Append(resolution.width).Append('x').Append(resolution.height).Append('@').Append(resolution.refreshRateRatio).Append("Hz");
        }

        Debug.Log(sb.ToString());
    }

    private void RequestResolution(int width, int height, FullScreenMode mode, string label)
    {
        ResolveReferences();

        if (applyResolutionRoutine != null)
            StopCoroutine(applyResolutionRoutine);

        applyResolutionRoutine = StartCoroutine(CoApplyResolution(width, height, mode, label));
    }

    private IEnumerator CoApplyResolution(int width, int height, FullScreenMode mode, string label)
    {
        RefreshFixedAspectViewport($"Before Request: {label}");
        Debug.Log(BuildResolutionLog($"Before Request: {label}", width, height, mode));
        LogAllTrackedRoots($"Before Request: {label}");
        ValidateCanvasScalers();

        Screen.SetResolution(width, height, mode);
        string exclusiveMarker = mode == FullScreenMode.ExclusiveFullScreen ? " [EXCLUSIVE_TEST]" : string.Empty;
        Debug.Log($"[ResolutionDebugPanel]{exclusiveMarker} Requested {label}: {width}x{height}, mode={mode}");
        if (mode == FullScreenMode.ExclusiveFullScreen)
        {
            Debug.LogWarning("[ResolutionDebugPanel][EXCLUSIVE_TEST] Exclusive fullscreen can flicker, affect Alt+Tab stability, or apply a different actual mode depending on the monitor/GPU/Windows environment. Verify the After Apply Screen values.");
        }

        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();
        RefreshFixedAspectViewport($"After Canvas Update: {label}");
        ForceRebuildConfiguredLayouts();
        Canvas.ForceUpdateCanvases();

        RefreshWindowClamps();

        yield return null;

        Canvas.ForceUpdateCanvases();
        RefreshFixedAspectViewport($"After Apply: {label}");
        Debug.Log(BuildResolutionLog($"After Apply: {label}", width, height, mode));
        LogAllTrackedRoots($"After Apply: {label}");
        ValidateCanvasScalers();

        applyResolutionRoutine = null;
    }

    private void ResolveReferences()
    {
        if (mainCanvas == null)
            mainCanvas = GetComponentInParent<Canvas>();

        if (mainCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.isRootCanvas)
                {
                    mainCanvas = canvas;
                    break;
                }
            }
        }

        if (mainCanvasScaler == null && mainCanvas != null)
            mainCanvasScaler = mainCanvas.GetComponent<CanvasScaler>();

        if (mainCanvasRoot == null && mainCanvas != null)
            mainCanvasRoot = mainCanvas.transform as RectTransform;

        if (windowManager == null)
            windowManager = FindObjectOfType<WindowManager>(true);

        if (fixedAspectViewport == null)
            fixedAspectViewport = FindObjectOfType<FixedAspectViewport>(true);
    }

    private void RefreshFixedAspectViewport(string phase)
    {
        if (fixedAspectViewport == null)
            fixedAspectViewport = FindObjectOfType<FixedAspectViewport>(true);

        if (fixedAspectViewport == null)
        {
            Debug.Log($"[ResolutionDebugPanel] {phase} FixedAspectViewport=<none>");
            return;
        }

        fixedAspectViewport.RefreshNow();
        Debug.Log(fixedAspectViewport.GetDiagnostics(phase));
    }

    private void ForceRebuildConfiguredLayouts()
    {
        bool rebuiltAny = false;

        if (layoutRebuildRoots != null)
        {
            foreach (RectTransform root in layoutRebuildRoots)
            {
                if (root == null) continue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                rebuiltAny = true;
                Debug.Log($"[ResolutionDebugPanel] ForceRebuildLayoutImmediate: {GetHierarchyPath(root.transform)}");
            }
        }

        if (!rebuiltAny && mainCanvasRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainCanvasRoot);
            Debug.Log($"[ResolutionDebugPanel] ForceRebuildLayoutImmediate fallback: {GetHierarchyPath(mainCanvasRoot.transform)}");
        }
    }

    private void RefreshWindowClamps()
    {
        if (refreshWindowManagerClamp)
        {
            if (windowManager == null)
                windowManager = FindObjectOfType<WindowManager>(true);

            if (windowManager != null)
            {
                windowManager.RefreshClampAllWindows();
            }
            else
            {
                Debug.LogWarning("[ResolutionDebugPanel] WindowManager not found; skipped open-window clamp refresh.");
            }
        }

        if (!refreshClampedDragPanels)
            return;

        UIDragMoveClamped[] dragPanels = FindObjectsOfType<UIDragMoveClamped>(true);
        foreach (UIDragMoveClamped panel in dragPanels)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy)
                continue;

            panel.SetSavedAnchoredPosition(panel.GetSavedAnchoredPosition());
            Debug.Log($"[ResolutionDebugPanel] Refreshed UIDragMoveClamped bounds: {GetHierarchyPath(panel.transform)}");
        }
    }

    private string BuildResolutionLog(string phase, int requestedWidth, int requestedHeight, FullScreenMode requestedMode)
    {
        Resolution current = Screen.currentResolution;
        var sb = new StringBuilder(512);
        string exclusiveMarker = requestedMode == FullScreenMode.ExclusiveFullScreen ? "[EXCLUSIVE_TEST] " : string.Empty;
        sb.Append("[ResolutionDebugPanel] ").Append(exclusiveMarker).Append(phase)
            .Append("\n  requested=").Append(requestedWidth).Append('x').Append(requestedHeight).Append(' ').Append(requestedMode)
            .Append("\n  actual Screen.width/height=").Append(Screen.width).Append('x').Append(Screen.height)
            .Append("\n  Screen.currentResolution=").Append(current.width).Append('x').Append(current.height).Append('@').Append(current.refreshRateRatio).Append("Hz")
            .Append("\n  Screen.fullScreenMode=").Append(Screen.fullScreenMode)
            .Append("\n  Screen.fullScreen=").Append(Screen.fullScreen);

        AppendCanvasLog(sb, mainCanvas, mainCanvasScaler, "mainCanvas");
        return sb.ToString();
    }

    private void AppendCanvasLog(StringBuilder sb, Canvas canvas, CanvasScaler scaler, string label)
    {
        if (canvas == null)
        {
            sb.Append("\n  ").Append(label).Append("=<null>");
            return;
        }

        RectTransform rootRect = canvas.transform as RectTransform;
        sb.Append("\n  ").Append(label).Append('=').Append(GetHierarchyPath(canvas.transform))
            .Append("\n    renderMode=").Append(canvas.renderMode)
            .Append(", pixelPerfect=").Append(canvas.pixelPerfect)
            .Append(", scaleFactor=").Append(canvas.scaleFactor)
            .Append(", pixelRect=").Append(canvas.pixelRect);

        if (rootRect != null)
            sb.Append("\n    rootRect.size=").Append(rootRect.rect.size).Append(", rootRect.rect=").Append(rootRect.rect);

        if (scaler == null)
        {
            sb.Append("\n    CanvasScaler=<null>");
            return;
        }

        sb.Append("\n    CanvasScaler.uiScaleMode=").Append(scaler.uiScaleMode)
            .Append(", referenceResolution=").Append(scaler.referenceResolution)
            .Append(", matchWidthOrHeight=").Append(scaler.matchWidthOrHeight)
            .Append(", scaler.scaleFactor=").Append(scaler.scaleFactor);
    }

    private void ValidateCanvasScalers()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                Debug.LogWarning($"[ResolutionDebugPanel] Canvas has no CanvasScaler: {GetHierarchyPath(canvas.transform)}");
                continue;
            }

            bool expectedMode = scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bool expectedReference = Approximately(scaler.referenceResolution, ExpectedReferenceResolution);
            bool expectedMatch = Mathf.Approximately(scaler.matchWidthOrHeight, ExpectedMatchWidthOrHeight);

            if (!expectedMode || !expectedReference || !expectedMatch)
            {
                Debug.LogWarning(
                    $"[ResolutionDebugPanel] CanvasScaler differs from expected ScaleWithScreenSize/1920x1080/Match0.5: " +
                    $"{GetHierarchyPath(canvas.transform)} mode={scaler.uiScaleMode}, reference={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}");
            }

            if (mainCanvasScaler != null && scaler != mainCanvasScaler)
            {
                bool differsFromMain = scaler.uiScaleMode != mainCanvasScaler.uiScaleMode ||
                                       !Approximately(scaler.referenceResolution, mainCanvasScaler.referenceResolution) ||
                                       !Mathf.Approximately(scaler.matchWidthOrHeight, mainCanvasScaler.matchWidthOrHeight);
                if (differsFromMain)
                {
                    Debug.LogWarning(
                        $"[ResolutionDebugPanel] CanvasScaler differs from main canvas: {GetHierarchyPath(canvas.transform)} " +
                        $"mode={scaler.uiScaleMode}, reference={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}; " +
                        $"main mode={mainCanvasScaler.uiScaleMode}, reference={mainCanvasScaler.referenceResolution}, match={mainCanvasScaler.matchWidthOrHeight}");
                }
            }
        }
    }

    private void LogAllTrackedRoots(string phase)
    {
        RectTransform[] roots = GetTrackedRoots();
        if (roots == null || roots.Length == 0)
        {
            Debug.Log($"[ResolutionDebugPanel] {phase} trackedRoots=<none>");
            return;
        }

        foreach (RectTransform root in roots)
        {
            if (root == null)
                continue;

            Vector3[] corners = new Vector3[4];
            root.GetWorldCorners(corners);
            Debug.Log(
                $"[ResolutionDebugPanel] {phase} root={GetHierarchyPath(root.transform)} " +
                $"active={root.gameObject.activeInHierarchy}, localScale={root.localScale}, sizeDelta={root.sizeDelta}, " +
                $"anchoredPosition={root.anchoredPosition}, rect={root.rect}, " +
                $"worldLB={corners[0]}, worldRT={corners[2]}");
        }
    }

    private RectTransform[] GetTrackedRoots()
    {
        if (!autoFindTrackedRootsByName)
            return trackedRoots;

        var found = new System.Collections.Generic.List<RectTransform>();
        if (trackedRoots != null)
        {
            foreach (RectTransform root in trackedRoots)
            {
                if (root != null && !found.Contains(root))
                    found.Add(root);
            }
        }

        if (trackedRootNames == null || trackedRootNames.Length == 0)
            return found.ToArray();

        RectTransform[] allRects = FindObjectsOfType<RectTransform>(true);
        foreach (string targetName in trackedRootNames)
        {
            if (string.IsNullOrWhiteSpace(targetName))
                continue;

            foreach (RectTransform rect in allRects)
            {
                if (rect == null)
                    continue;

                if (rect.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase) ||
                    rect.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!found.Contains(rect))
                        found.Add(rect);
                }
            }
        }

        return found.ToArray();
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
