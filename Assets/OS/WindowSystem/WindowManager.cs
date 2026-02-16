using System.Collections.Generic;
using System.Collections;   
using UnityEngine;
using PPP.OS.Save;


public class WindowManager : MonoBehaviour
{
    [SerializeField] private RectTransform windowsRoot;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TaskbarManager taskbarManager;
    [SerializeField] private RectTransform iconsRoot; // DesktopIconBG 같은 부모
    [SerializeField] private DesktopGridManager desktopGridManager;

    [System.Serializable]
    public struct WindowDefault
    {
        public Vector2 pos;
        public Vector2 size;
    }

    private readonly Dictionary<string, WindowDefault> windowDefaults = new();

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


    private IEnumerator CoFinalizeSpawn(WindowController w)
    {
        yield return null; // ✅ 1프레임 대기 (레이아웃/컨텐츠 반영)
        w.ForceClampNow(overflow: 0f);
        RequestAutoSave(); // 선택
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
        EnsureSaveCacheLoaded();
        var data = cachedSave;

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
        windowDefaults[appId] = new WindowDefault { pos = defaultPos, size = defaultSize };

        Focus(appId);
        spawned.PlayOpen();
        StartCoroutine(CoFinalizeSpawn(spawned));

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

    // 공용 헬퍼 하나
    private void EnsureFocused(string appId)
    {
        if (activeAppId == appId) return;
        Focus(appId);
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

        PostApplyLayoutSanity(); // ✅ 추가

        Debug.Log("[OS] LoadOS applied.");
    }

    private void PostApplyLayoutSanity()
    {
        // 1) 아이콘: allowed rect 기준 clamp + (Grid면 슬롯 스냅)
        if (iconsRoot != null)
        {
            var allowed = DesktopBounds.GetAllowedRect(iconsRoot);
            var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);

            foreach (var ic in icons)
            {
                var rt = ic.GetRect();
                rt.anchoredPosition = DesktopBounds.ClampAnchoredPosition(rt.anchoredPosition, rt, allowed);
            }

            // Grid 모드면 “현재 모드” 기준으로 한번 더 정렬(선택)
            desktopGridManager?.SnapAllIfGridMode();
        }

        // 2) 창: canvas 기준 clamp(네 WindowController에 clamp가 있으면 여기선 생략 가능)
        // 정말 완전하게 하려면 WindowController에 "ForceClampNow()" 같은 메서드 하나 만들어서 호출하면 베스트.
    }

    public void OnDesktopResized()
    {
        PostApplyLayoutSanity();
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

    public void Minimize(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (w.IsMinimized) return;

        EnsureFocused(appId); // ✅ 추가 (버튼 누른 창이 먼저 포커스)

        // (이하 기존 코드)
        w.CacheRestorePos(w.GetWindowRoot().anchoredPosition);

        var btnRect = taskbarManager?.GetButtonRect(appId);
        Vector2 target = btnRect != null ? btnRect.anchoredPosition : new Vector2(0, -500);

        w.PlayMinimize(target, () =>
        {
            w.SetMinimized(true);
            taskbarManager?.SetMinimized(appId, true);

            // ✅ 최소화 완료 후, 다음 창으로 포커스 넘기기
            if (!suppressAutoFocus)
                FocusNextTopWindow(excludedAppId: appId);

            RequestAutoSave();
        });
    }



    public void Restore(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;

        // 이미 열려있고 최소화가 아니면 그냥 포커스만
        if (!w.IsMinimized) { Focus(appId); return; }

        // 태스크바 버튼 위치를 from으로
        var btnRect = taskbarManager?.GetButtonRect(appId);
        Vector2 from = btnRect != null ? btnRect.anchoredPosition : w.GetWindowRoot().anchoredPosition;

        // ✅ 먼저 보이게
        w.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);

        // ✅ "복원 애니 시작 전에" 최상단으로
        w.transform.SetAsLastSibling();
        activeAppId = appId; // (선택) Focus 호출 전에 active 지정해도 됨

        w.PlayRestore(from, () =>
        {
            // ✅ 복원 끝나고 확실히 Focus (비주얼/태스크바 Active 갱신)
            Focus(appId);
            RequestAutoSave();
        });
    }



    public void ResetWindowsToDefaults()
    {
        // 1) 저장 데이터에서 windows 전부 제거 (다음 실행/다음 오픈부터 기본값 적용)
        var data = PPP.OS.Save.OSSaveSystem.Load();
        if (data != null && data.windows != null)
        {
            data.windows.Clear();
            PPP.OS.Save.OSSaveSystem.Save(data);
            cachedSave = data;
        }

        // 2) 현재 열려있는 창들은 즉시 기본값으로 이동/리사이즈
        foreach (var kv in openWindows)
        {
            string id = kv.Key;
            var wc = kv.Value;
            if (wc == null) continue;

            if (windowDefaults.TryGetValue(id, out var d))
            {
                wc.SetWindowPosition(d.pos);
                wc.SetWindowSize(d.size);
            }
        }

        // 3) 덮어쓰기 저장
        SaveOS();

        Debug.Log("[Desktop] ResetWindowsToDefaults applied.");
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
        spawned.PlayOpen();
    }


    public void Close(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var window) || window == null)
            return;

        EnsureFocused(appId); // ✅ 추가

        window.PlayClose(() =>
        {
            taskbarManager?.Remove(appId);
            openWindows.Remove(appId);

            if (!suppressAutoFocus)
                FocusNextTopWindow(excludedAppId: null);

            Destroy(window.gameObject);
            RequestAutoSave();
        });
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

    private void FocusNextTopWindow(string excludedAppId = null)
    {
        WindowController best = null;
        int bestSibling = -1;

        foreach (var kv in openWindows)
        {
            string id = kv.Key;
            var wc = kv.Value;
            if (wc == null) continue;

            if (!string.IsNullOrEmpty(excludedAppId) && id == excludedAppId) continue;
            if (wc.IsMinimized) continue;

            int sib = wc.transform.GetSiblingIndex();
            if (sib > bestSibling)
            {
                bestSibling = sib;
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



    public void MinimizeAll()
    {
        foreach (var kv in openWindows)
        {
            var w = kv.Value;
            if (w == null) continue;
            if (!w.IsMinimized) Minimize(kv.Key);
        }
    }

    public void RestoreAll()
    {
        foreach (var kv in openWindows)
        {
            var w = kv.Value;
            if (w == null) continue;
            if (w.IsMinimized) Restore(kv.Key);
        }
    }

}
