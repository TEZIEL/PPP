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

    [System.Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
        public string label;
        public string aspectType;
        public bool isLetterboxed;

        public ResolutionOption(int width, int height, string label, string aspectType, bool isLetterboxed)
        {
            this.width = width;
            this.height = height;
            this.label = label;
            this.aspectType = aspectType;
            this.isLetterboxed = isLetterboxed;
        }
    }

    [Header("Canvas Diagnostics")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private CanvasScaler mainCanvasScaler;
    [SerializeField] private RectTransform mainCanvasRoot;

    [Header("Fixed Aspect Viewport")]
    [Tooltip("Optional 16:9 viewport controller. If assigned, resolution diagnostics refresh and log it after screen changes.")]
    [SerializeField] private FixedAspectViewport fixedAspectViewport;
    [SerializeField] private bool validateFixedAspectSceneWiring = true;
    [SerializeField] private RectTransform[] requiredViewportChildren;
    [SerializeField]
    private string[] requiredViewportChildNames =
    {
        "BackgroundBG",
        "DesktopIconBG",
        "TaskbarBG",
        "WindowRoot",
        "OptionsModal",
        "VNRoot",
        "VN UI",
        "VNCanvas",
        "Music",
        "Recipe",
        "Fidget"
    };

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

    // Button Methods: Windowed 16:9 (no letterbox expected)
    public void Set1280x720Windowed() => RequestWindowed(new ResolutionOption(1280, 720, "1280x720 Windowed", "16:9", false));

    public void Set1600x900Windowed() => RequestWindowed(new ResolutionOption(1600, 900, "1600x900 Windowed", "16:9", false));

    public void Set1920x1080Windowed() => RequestWindowed(new ResolutionOption(1920, 1080, "1920x1080 Windowed", "16:9", false));

    public void Set2560x1440Windowed() => RequestWindowed(new ResolutionOption(2560, 1440, "2560x1440 Windowed", "16:9", false));

    // Button Methods: Windowed 16:10 (letterboxed inside FixedAspectViewport)
    public void Set1280x800Windowed() => RequestWindowed(new ResolutionOption(1280, 800, "1280x800 Windowed", "16:10 Letterboxed", true));

    public void Set1600x1000Windowed() => RequestWindowed(new ResolutionOption(1600, 1000, "1600x1000 Windowed", "16:10 Letterboxed", true));

    public void Set1680x1050Windowed() => RequestWindowed(new ResolutionOption(1680, 1050, "1680x1050 Windowed", "16:10 Letterboxed", true));

    public void Set1920x1200Windowed() => RequestWindowed(new ResolutionOption(1920, 1200, "1920x1200 Windowed", "16:10 Letterboxed", true));

    public void Set2560x1600Windowed() => RequestWindowed(new ResolutionOption(2560, 1600, "2560x1600 Windowed", "16:10 Letterboxed", true));

    public void Set2880x1800Windowed() => RequestWindowed(new ResolutionOption(2880, 1800, "2880x1800 Windowed", "16:10 Letterboxed", true));

    // Official fullscreen policy: borderless native monitor size. FixedAspectViewport keeps the internal game stage 16:9.
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

        RequestResolution(width, height, FullScreenMode.FullScreenWindow, "Borderless Fullscreen Native", "Native Borderless", IsLetterboxedAspect(width, height));
    }

    public void SetBackTo1920x1080Windowed() => RequestWindowed(new ResolutionOption(1920, 1080, "Back to 1920x1080 Windowed", "16:9", false));

    public void LogCurrentResolutionDiagnostics()
    {
        ResolveReferences();
        RefreshFixedAspectViewport("Manual Diagnostics");
        Debug.Log(BuildResolutionLog("Manual Diagnostics", Screen.width, Screen.height, Screen.fullScreenMode, "Current", IsLetterboxedAspect(Screen.width, Screen.height)));
        LogAllTrackedRoots("Manual Diagnostics");
        ValidateCanvasScalers();
        ValidateFixedAspectSceneWiring();
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

    private void RequestWindowed(ResolutionOption option)
    {
        RequestResolution(option.width, option.height, FullScreenMode.Windowed, option.label, option.aspectType, option.isLetterboxed);
    }

    private void RequestResolution(int width, int height, FullScreenMode mode, string label, string aspectType, bool isLetterboxed)
    {
        ResolveReferences();

        if (applyResolutionRoutine != null)
            StopCoroutine(applyResolutionRoutine);

        applyResolutionRoutine = StartCoroutine(CoApplyResolution(width, height, mode, label, aspectType, isLetterboxed));
    }

    private IEnumerator CoApplyResolution(int width, int height, FullScreenMode mode, string label, string aspectType, bool isLetterboxed)
    {
        RefreshFixedAspectViewport($"Before Request: {label}");
        Debug.Log(BuildResolutionLog($"Before Request: {label}", width, height, mode, aspectType, isLetterboxed));
        LogAllTrackedRoots($"Before Request: {label}");
        ValidateCanvasScalers();
        ValidateFixedAspectSceneWiring();
        if (windowManager == null)
            windowManager = FindObjectOfType<WindowManager>(true);
        windowManager?.CaptureIconLayoutForResize();

        Screen.SetResolution(width, height, mode);
        Debug.Log($"[ResolutionDebugPanel] Requested {label}: {width}x{height}, mode={mode}, aspectType={aspectType}, isLetterboxed={isLetterboxed}");

        yield return null;
        yield return null;

        RefreshFixedAspectViewport($"After Resolution Wait: {label}");
        Canvas.ForceUpdateCanvases();
        ForceRebuildConfiguredLayouts();
        Canvas.ForceUpdateCanvases();

        if (windowManager == null)
            windowManager = FindObjectOfType<WindowManager>(true);
        windowManager?.RestoreIconLayoutAfterResize();
        windowManager?.OnDesktopResized();
        RefreshWindowClamps();

        yield return null;

        Canvas.ForceUpdateCanvases();
        RefreshFixedAspectViewport($"After Apply: {label}");
        Debug.Log(BuildResolutionLog($"After Apply: {label}", width, height, mode, aspectType, isLetterboxed));
        LogAllTrackedRoots($"After Apply: {label}");
        ValidateCanvasScalers();
        ValidateFixedAspectSceneWiring();

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
                windowManager.LogIconBoundsDiagnostics("After Resolution Clamp Refresh");
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

    private string BuildResolutionLog(string phase, int requestedWidth, int requestedHeight, FullScreenMode requestedMode, string aspectType, bool isLetterboxed)
    {
        Resolution current = Screen.currentResolution;
        var sb = new StringBuilder(512);
        sb.Append("[ResolutionDebugPanel] ").Append(phase)
            .Append("\n  requested=").Append(requestedWidth).Append('x').Append(requestedHeight).Append(' ').Append(requestedMode)
            .Append(", aspectType=").Append(aspectType).Append(", isLetterboxed=").Append(isLetterboxed)
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


    private void ValidateFixedAspectSceneWiring()
    {
        if (!validateFixedAspectSceneWiring)
            return;

        if (fixedAspectViewport == null)
        {
            Debug.LogWarning("[ResolutionDebugPanel] FixedAspectViewport is not assigned/found. 16:10 letterboxing cannot be verified.");
            return;
        }

        RectTransform viewport = fixedAspectViewport.GameViewport;
        RectTransform parent = fixedAspectViewport.ParentRect;

        if (mainCanvasRoot != null && parent != mainCanvasRoot)
        {
            Debug.LogWarning($"[ResolutionDebugPanel] FixedAspectViewport parent should be the main Canvas rect. parent={GetHierarchyPath(parent)}, expected={GetHierarchyPath(mainCanvasRoot)}");
        }

        if (viewport == null)
        {
            Debug.LogWarning("[ResolutionDebugPanel] FixedAspectViewport.GameViewport is null.");
            return;
        }

        ValidateRequiredViewportChildren(viewport);
        ValidateWindowManagerViewportWiring(viewport);
        ValidateDesktopGridManagerViewportWiring(viewport);
        ValidateContextMenuViewportWiring(viewport);
    }

    private void ValidateRequiredViewportChildren(RectTransform viewport)
    {
        if (requiredViewportChildren != null)
        {
            foreach (RectTransform child in requiredViewportChildren)
            {
                if (child == null) continue;
                WarnIfNotChildOfViewport(child, viewport, "requiredViewportChildren");
            }
        }

        if (requiredViewportChildNames == null)
            return;

        foreach (string childName in requiredViewportChildNames)
        {
            if (string.IsNullOrWhiteSpace(childName))
                continue;

            RectTransform child = FindRectTransformByName(childName);
            if (child == null)
                continue;

            WarnIfNotChildOfViewport(child, viewport, childName);
        }
    }

    private void ValidateWindowManagerViewportWiring(RectTransform viewport)
    {
        if (windowManager == null)
            windowManager = FindObjectOfType<WindowManager>(true);

        if (windowManager == null)
            return;

        if (windowManager.CanvasRect != viewport)
        {
            Debug.LogWarning($"[ResolutionDebugPanel] WindowManager.canvasRect should point to GameViewport16x9 for viewport-based window clamp. current={GetHierarchyPath(windowManager.CanvasRect)}, expected={GetHierarchyPath(viewport)}");
        }

        WarnIfNotChildOfViewport(windowManager.WindowsRoot, viewport, "WindowManager.windowsRoot");
        WarnIfNotChildOfViewport(windowManager.IconsRoot, viewport, "WindowManager.iconsRoot");
    }

    private void ValidateDesktopGridManagerViewportWiring(RectTransform viewport)
    {
        DesktopGridManager[] gridManagers = FindObjectsOfType<DesktopGridManager>(true);
        foreach (DesktopGridManager gridManager in gridManagers)
        {
            if (gridManager == null)
                continue;

            WarnIfNotChildOfViewport(gridManager.IconsRoot, viewport, "DesktopGridManager.iconsRoot");
            if (windowManager != null && windowManager.IconsRoot != null && gridManager.IconsRoot != windowManager.IconsRoot)
            {
                Debug.LogWarning($"[ResolutionDebugPanel] DesktopGridManager.iconsRoot should match WindowManager.iconsRoot so normalized icon restore and grid slots share DesktopIconBG. grid={GetHierarchyPath(gridManager.IconsRoot)}, windowManager={GetHierarchyPath(windowManager.IconsRoot)}");
            }
        }
    }

    private void ValidateContextMenuViewportWiring(RectTransform viewport)
    {
        DesktopContextMenuController[] controllers = FindObjectsOfType<DesktopContextMenuController>(true);
        foreach (DesktopContextMenuController controller in controllers)
        {
            if (controller == null)
                continue;

            if (controller.CanvasRect != viewport)
            {
                Debug.LogWarning($"[ResolutionDebugPanel] DesktopContextMenuController.canvasRect should point to GameViewport16x9 so context menus clamp inside the 16:9 stage. controller={GetHierarchyPath(controller.transform)}, current={GetHierarchyPath(controller.CanvasRect)}, expected={GetHierarchyPath(viewport)}");
            }
        }
    }

    private void WarnIfNotChildOfViewport(RectTransform rect, RectTransform viewport, string label)
    {
        if (rect == null || viewport == null)
            return;

        if (rect == viewport || rect.IsChildOf(viewport))
            return;

        Debug.LogWarning($"[ResolutionDebugPanel] {label} should be under GameViewport16x9 for fixed 16:9 staging. object={GetHierarchyPath(rect)}, viewport={GetHierarchyPath(viewport)}");
    }

    private RectTransform FindRectTransformByName(string targetName)
    {
        RectTransform[] allRects = FindObjectsOfType<RectTransform>(true);
        foreach (RectTransform rect in allRects)
        {
            if (rect != null && rect.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
                return rect;
        }

        return null;
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

    private static bool IsLetterboxedAspect(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;

        float aspect = width / (float)height;
        return aspect < (16f / 9f) - 0.001f;
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
