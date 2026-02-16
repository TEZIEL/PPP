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

    public bool IsOpen(string appId)
    {
        return openWindows.ContainsKey(appId);
    }

    private void Start()
    {
        InjectAllWindows();
        InjectAllIcons();     // ✅ 추가
        LoadWindows();
        cachedSave = OSSaveSystem.Load(); // 캐시 로드
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
        SaveWindows();
    }


   
  

    public void SaveWindows()
    {
        var data = new OSSaveData();

        // 1) windows 저장
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

        // 2) icons 저장
        if (iconsRoot != null)
        {
            var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
            foreach (var ic in icons)
            {
                data.icons.Add(new OSIconData
                {
                    id = ic.GetId(),
                    position = Vector2Int.RoundToInt(ic.GetRect().anchoredPosition)
                });
            }
        }

        // ✅ 파일 저장은 마지막에 1번만
        OSSaveSystem.Save(data);
        cachedSave = data; // 캐시도 갱신(선택)
        Debug.Log("[OS] SaveWindows completed.");
    }

    public void LoadWindows()
    {
        var data = OSSaveSystem.Load();
        if (data == null) return;

        // 1) windows 복원
        var windows = windowsRoot.GetComponentsInChildren<WindowController>(true);
        foreach (var w in windows)
        {
            string id = w.GetAppId();
            var saved = data.windows.Find(x => x.appId == id);
            if (saved == null) continue;

            RectTransform rect = w.GetWindowRoot();
            rect.anchoredPosition = saved.position;
            rect.sizeDelta = saved.size;
        }

        // 2) icons 복원
        if (iconsRoot != null && data.icons != null)
        {
            var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
            foreach (var ic in icons)
            {
                var saved = data.icons.Find(x => x.id == ic.GetId());
                if (saved == null) continue;

                ic.GetRect().anchoredPosition = saved.position;
            }
        }

        cachedSave = data; // 캐시도 갱신(선택)
        Debug.Log("[OS] LoadWindows applied.");
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
