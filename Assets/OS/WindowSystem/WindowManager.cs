using System.Collections.Generic;
using UnityEngine;
using PPP.OS.Save;


public class WindowManager : MonoBehaviour
{
    [SerializeField] private RectTransform windowsRoot;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TaskbarManager taskbarManager;
    [SerializeField] private RectTransform iconsRoot; // DesktopIconBG 같은 부모
    [SerializeField] private DesktopGridManager desktopGridManager;


    private readonly Dictionary<string, WindowController> openWindows = new();

    private string activeAppId;
    public string ActiveAppId => activeAppId;
    private OSSaveData cachedSave;

    private bool suppressAutoFocus;
    public void BeginBatch() => suppressAutoFocus = true;
    public void EndBatch() => suppressAutoFocus = false;

    public void Open(string appId, WindowController windowPrefab)
    {
        Open(appId, windowPrefab, Vector2.zero);
    }


    // ✅ 새로 추가: 위치+사이즈까지 받는 버전(아이콘에서 이걸 쓸 것)
    public void Open(
    string appId,
    WindowController windowPrefab,
    GameObject contentPrefab,
    Vector2 defaultPos,
    Vector2 defaultSize)
    {
        if (string.IsNullOrWhiteSpace(appId) || windowPrefab == null) return;
        if (openWindows.ContainsKey(appId)) return;

        Transform parent = windowsRoot != null ? windowsRoot : transform;
        var spawned = Instantiate(windowPrefab, parent);

        spawned.Initialize(this, appId, canvasRect);
        spawned.InjectManager(this);

        // cachedSave가 비어있을 수 있으니 "필요 시 로드"
        var data = cachedSave ?? PPP.OS.Save.OSSaveSystem.Load();

        bool appliedSaved = false;
        if (data != null)
        {
            var wd = data.windows.Find(x => x.appId == appId);
            if (wd != null)
            {
                spawned.SetWindowPosition(wd.position);
                TryApplyWindowSize(spawned, wd.size);
                appliedSaved = true;
            }
        }

        if (!appliedSaved)
        {
            spawned.SetWindowPosition(defaultPos);
            TryApplyWindowSize(spawned, Vector2Int.RoundToInt(defaultSize));
        }

        AttachContent(spawned, contentPrefab);

        openWindows.Add(appId, spawned);
        taskbarManager?.Add(appId, spawned);
        Focus(appId);
    }


    private void AttachContent(WindowController spawned, GameObject contentPrefab)
    {
        if (spawned == null) return;
        if (contentPrefab == null) return;
        if (spawned.ContentRoot == null) return;

        var content = Instantiate(contentPrefab, spawned.ContentRoot);

        if (content.transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }





    private void EnsureSaveCacheLoaded()
    {
        if (cachedSave != null) return;
        cachedSave = PPP.OS.Save.OSSaveSystem.Load(); // 없으면 null일 수 있음
    }



    // ✅ 사이즈 적용 헬퍼(메서드 유무/구현 차이에 안전하게)
    private void TryApplyWindowSize(WindowController wc, Vector2Int size)
    {
        var r = wc.GetWindowRoot();
        if (r == null) return;
        r.sizeDelta = size;
    }

    public bool IsOpen(string appId)
    {
        return openWindows.ContainsKey(appId);
    }

    private void Start()
    {
        InjectAllWindows();
        InjectAllIcons();     // ✅ 추가
        LoadOS();
        
    }


    private bool TryGetSavedWindow(string appId, out OSWindowData wd)
    {
        wd = null;
        if (cachedSave == null) return false;

        wd = cachedSave.windows.Find(x => x.appId == appId);
        return wd != null;
    }


    private void InjectAllWindows()
    {
        var windows = windowsRoot.GetComponentsInChildren<WindowController>(true);
        foreach (var w in windows)
            w.InjectManager(this);
    }

    private void InjectAllIcons()
    {
        if (iconsRoot == null) return;

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        foreach (var ic in icons)
            ic.Initialize(this, iconsRoot, desktopGridManager);
    }



    private float nextAutoSaveTime;
    private const float autoSaveCooldown = 0.3f;

    public void RequestAutoSave()
    {
        if (Time.unscaledTime < nextAutoSaveTime) return;
        nextAutoSaveTime = Time.unscaledTime + autoSaveCooldown;
        SaveOS();
    }


    public void SaveOS()
    {
        var data = new OSSaveData();
        CollectWindows(data);
        CollectIcons(data);

        OSSaveSystem.Save(data);
        cachedSave = data;

        Debug.Log("[OS] SaveOS completed.");
    }

    public void LoadOS()
    {
        var data = OSSaveSystem.Load();
        if (data == null) return;

        ApplyWindows(data);
        ApplyIcons(data);

        cachedSave = data;

        Debug.Log("[OS] LoadOS applied.");
    }



    private void CollectIcons(OSSaveData data)
    {
        if (iconsRoot == null) return;

        Rect allowed = DesktopBounds.GetAllowedRect(iconsRoot);

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        foreach (var ic in icons)
        {
            Vector2 pos = ic.GetRect().anchoredPosition;

            DesktopLayoutMode mode =
                (desktopGridManager != null && desktopGridManager.LayoutMode == DesktopGridManager.DesktopLayoutMode.Grid)
                    ? DesktopLayoutMode.Grid
                    : DesktopLayoutMode.Free;

            var entry = new OSIconData
            {
                id = ic.GetId(),
                layoutMode = mode,
                slotIndex = -1,
                normalized = Vector2.zero
            };

            if (mode == DesktopLayoutMode.Grid && desktopGridManager != null)
            {
                entry.slotIndex = desktopGridManager.GetNearestSlotIndex(pos);
            }
            else
            {
                float nx = Mathf.InverseLerp(allowed.xMin, allowed.xMax, pos.x);
                float ny = Mathf.InverseLerp(allowed.yMin, allowed.yMax, pos.y);
                entry.normalized = new Vector2(nx, ny);
            }

            data.icons.Add(entry);
        }
    }




    private void ApplyIcons(OSSaveData data)
    {
        if (iconsRoot == null || data.icons == null) return;

        Rect allowed = DesktopBounds.GetAllowedRect(iconsRoot);

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        foreach (var ic in icons)
        {
            var saved = data.icons.Find(x => x.id == ic.GetId());
            if (saved == null) continue;

            Vector2 target;

            if (saved.layoutMode == DesktopLayoutMode.Grid && desktopGridManager != null)
            {
                target = desktopGridManager.GetSlotPos(saved.slotIndex, ic.GetRect().anchoredPosition);
            }
            else
            {
                float x = Mathf.Lerp(allowed.xMin, allowed.xMax, saved.normalized.x);
                float y = Mathf.Lerp(allowed.yMin, allowed.yMax, saved.normalized.y);
                target = new Vector2(x, y);
            }

            ic.GetRect().anchoredPosition =
                DesktopBounds.ClampAnchoredPosition(target, ic.GetRect(), allowed);
        }
    }



    private void CollectWindows(OSSaveData data)
    {
        var windows = windowsRoot.GetComponentsInChildren<WindowController>(true);
        foreach (var w in windows)
        {
            RectTransform rect = w.GetWindowRoot();

            data.windows.Add(new OSWindowData
            {
                appId = w.GetAppId(),
                position = Vector2Int.RoundToInt(rect.anchoredPosition),
                size = Vector2Int.RoundToInt(rect.sizeDelta),
                isMinimized = false
            });
        }
    }


    private void ApplyWindows(OSSaveData data)
    {
        var windows = windowsRoot.GetComponentsInChildren<WindowController>(true);
        foreach (var w in windows)
        {
            var saved = data.windows.Find(x => x.appId == w.GetAppId());
            if (saved == null) continue;

            RectTransform rect = w.GetWindowRoot();
            rect.anchoredPosition = saved.position;
            rect.sizeDelta = saved.size;
        }
    }








    public void Open(string appId, WindowController windowPrefab, Vector2 defaultPos)
    {
        if (string.IsNullOrWhiteSpace(appId) || windowPrefab == null) return;
        if (openWindows.ContainsKey(appId)) return;

        Transform parent = windowsRoot != null ? windowsRoot : transform;
        WindowController spawned = Instantiate(windowPrefab, parent);
        spawned.Initialize(this, appId, canvasRect);

        // ✅ OS 저장에서 위치/사이즈/최소화 불러오기
        if (TryGetSavedWindow(appId, out var wd))
        {
            spawned.SetWindowPosition(wd.position);
            spawned.SetWindowSize(wd.size);          // 이 메서드 없으면 size 적용은 나중에
            
        }
        else
        {
            spawned.SetWindowPosition(defaultPos);
        }

        openWindows.Add(appId, spawned);
        taskbarManager?.Add(appId, spawned);

        Focus(appId);
        spawned.InjectManager(this); // ✅ 굳이 InjectAllWindows 말고 얘만 주입해도 됨
    }


    public void Close(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController window) || window == null)
            return;

        bool wasActive = (activeAppId == appId);
                
        if (wasActive && !suppressAutoFocus)
            FocusNextTopWindow(appId);

        openWindows.Remove(appId);
        taskbarManager?.Remove(appId);

        Destroy(window.gameObject);
    }

    public void Focus(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var target) || target == null)
            return;

        activeAppId = appId;

        // ✅ 최소화 상태면 포커스 시 복원
        if (target.IsMinimized)
        {
            target.SetMinimized(false);
            taskbarManager?.SetMinimized(appId, false);
        }

        target.transform.SetAsLastSibling();

        foreach (var pair in openWindows)
        {
            if (pair.Value == null) continue;

            bool active = pair.Key == appId;

            // minimized면 안 보이니까 active visual 생략 가능(선택)
            if (!pair.Value.IsMinimized)
                pair.Value.SetActiveVisual(active);

            taskbarManager?.SetActive(pair.Key, active);
        }
    }

    public void Minimize(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController target) || target == null)
            return;

        bool wasActive = (activeAppId == appId);

        if (target.WindowRoot != null)
            SaveSystem.SetWindowPositionHook(appId, target.WindowRoot.anchoredPosition);

        target.SetMinimized(true);
        taskbarManager?.SetMinimized(appId, true);

        if (wasActive && !suppressAutoFocus)
            FocusNextTopWindow(appId);
    }

    public void Restore(string appId)
    {
        if (!openWindows.TryGetValue(appId, out WindowController target) || target == null)
            return;

        target.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);

        Focus(appId);
    }

    public bool IsMinimized(string appId)
    {
        return openWindows.TryGetValue(appId, out var target) && target != null && target.IsMinimized;
    }

    public void MinimizeNoFocus(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var target) || target == null)
            return;

        if (target.WindowRoot != null)
            SaveSystem.SetWindowPositionHook(appId, target.WindowRoot.anchoredPosition);

        target.SetMinimized(true);
        taskbarManager?.SetMinimized(appId, true);
    }

    public void RestoreNoFocus(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var target) || target == null)
            return;

        target.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);
    }

    public void OnWindowMoved(string appId, Vector2 anchoredPosition)
    {
        SaveSystem.SetWindowPositionHook(appId, anchoredPosition);
    }

    public IReadOnlyDictionary<string, WindowController> GetOpenWindows()
    {
        return openWindows;
    }

    private void FocusNextTopWindow(string excludedAppId)
    {
        var root = windowsRoot != null ? windowsRoot : (RectTransform)transform;

        WindowController best = null;
        int bestSibling = -1;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null) continue;

            var wc = child.GetComponent<WindowController>();
            if (wc == null) continue;

            if (!string.IsNullOrEmpty(excludedAppId) && wc.AppId == excludedAppId) continue;

            // ✅ 너는 이제 "SetActive(false)" 안 쓰는 구조로 가고 있으니 IsMinimized 기준이 맞음
            if (wc.IsMinimized) continue;

            if (i > bestSibling)
            {
                bestSibling = i;
                best = wc;
            }
        }

        if (best != null)
        {
            Focus(best.AppId);
        }
        else
        {
            activeAppId = null;
            foreach (var pair in openWindows)
                taskbarManager?.SetActive(pair.Key, false);
        }
    }
}
