using System.Collections.Generic;
using System.Collections;   
using UnityEngine;
using PPP.OS.Save;
using UnityEngine.EventSystems;
using PPP.BLUE.VN;



public class WindowManager : MonoBehaviour, IVNHostOS
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

    private readonly Dictionary<string, bool> minimizedByAppId = new();
    private readonly Dictionary<string, bool> exitLockedByAppId = new();

    private readonly Dictionary<string, WindowDefault> windowDefaults = new();
    private readonly Dictionary<string, VNWindowState> _lastStates = new();
    private readonly Dictionary<string, WindowController> openWindows = new();

    private string activeAppId;
    public string ActiveAppId => activeAppId;
    private OSSaveData cachedSave;

    [SerializeField] private bool logWindowState = false; // 필요할 때만 Inspector에서 켜기

    private bool isAnimating;
    public bool IsAnimating => isAnimating;

    private bool suppressAutoFocus;
    public void BeginBatch() => suppressAutoFocus = true;
    public void EndBatch() => suppressAutoFocus = false;
    private float closeLockUntil;
    public void LockCloseForSeconds(float seconds)
    {
        closeLockUntil = Mathf.Max(closeLockUntil, Time.unscaledTime + seconds);
    }
    private bool IsCloseLocked() => Time.unscaledTime < closeLockUntil;


    public void SetMinimized(string appId, bool minimized)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;

        minimizedByAppId[appId] = minimized;
        w.SetMinimized(minimized);

        // (선택) 최소화/복원 직후 잔입력 방지
        // LockCloseForSeconds(0.1f);
    }

    public bool IsFocused(string appId)
    {

        // 네 시스템에서 activeAppId 같은 걸 쓰는 듯
        return activeAppId == appId;
    }

    public void ToggleShowDesktop()
    {
        Debug.Log("[OS] ToggleShowDesktop disabled");
    }

    public void Open(AppDefinition def)
    {
        if (def == null) return;

        Open(
            def.AppId,
            def.DisplayName,
            def.WindowPrefab,
            def.ContentPrefab,
            def.DefaultPos,
            def.DefaultSize
        );
    }

  
    // ✅ 새로 추가: 위치+사이즈까지 받는 버전(아이콘에서 이걸 쓸 것)
    public void Open(
    string appId,
    string displayName,
    WindowController windowPrefab,
    GameObject contentPrefab,
    Vector2 defaultPos,
    Vector2 defaultSize)
    {
        if (string.IsNullOrWhiteSpace(appId) || windowPrefab == null) return;
        if (openWindows.ContainsKey(appId)) return;

        Transform parent = windowsRoot != null ? windowsRoot : transform;
        var spawned = Instantiate(windowPrefab, parent);

        spawned.Initialize(this, appId, canvasRect, displayName);
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
        taskbarManager?.Add(appId, displayName, spawned);
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

        // ✅ (1) RectTransform 풀스트레치
        if (content.transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        // ✅ (2) VNOSBridge가 있으면 Host 주입
        var bridge = content.GetComponentInChildren<PPP.BLUE.VN.VNOSBridge>(true);
        var runner = content.GetComponentInChildren<PPP.BLUE.VN.VNRunner>(true);

        if (bridge != null)
        {
            bridge.InjectHost(this, spawned.AppId); // ← AppId 필드명은 네 프로젝트에 맞춰
            Debug.Log($"[OS] InjectHost -> {spawned.AppId}");
        }

        if (runner != null && bridge != null)
        {
            runner.InjectBridge(bridge); // 아래 메서드 추가
        }
    }


    private Vector2 ConvertToWindowLocal(RectTransform from)
    {
        if (from == null) return Vector2.zero;

        RectTransform parent = windowsRoot != null
            ? windowsRoot
            : (RectTransform)transform;

        // 버튼 월드 → 스크린
        Vector2 screen =
            RectTransformUtility.WorldToScreenPoint(null, from.position);

        // 스크린 → 윈도우 부모 로컬
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screen,
            null,
            out var local);

        return local;
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

    public void ShowConfirmClose(string appId)
    {
        // TODO: 네 팝업 시스템 호출
        Debug.Log($"[OS] Confirm close popup for {appId}");
    }


    private float nextAutoSaveTime;
    private const float autoSaveCooldown = 0.3f;


    public void InternalRequestAutoSave()
    {
        RequestAutoSave(); // 네가 이미 가진 함수라면 이름 충돌 나니까
        // 만약 동일 이름이면 내부용을 다른 이름으로 바꿔:
        
    }

    public void RequestAutoSave()
    {
        if (Time.unscaledTime < nextAutoSaveTime) return;
        nextAutoSaveTime = Time.unscaledTime + autoSaveCooldown;
        SaveOS();
    }


    public void SaveOS()
    {
        Debug.Log($"[OS SAVE] persistentDataPath={Application.persistentDataPath}");
        var saveData = new OSSaveData();

        // 1) windows/icons 먼저
        CollectWindows(saveData);
        CollectIcons(saveData);

        // 2) ✅ subBlocks 채우기 (같은 saveData에!)
        saveData.subBlocks.Clear();
        foreach (var kv in subBlockJsonByKey)
        {
            saveData.subBlocks.Add(new OSSubBlockData
            {
                key = kv.Key,
                json = kv.Value ?? ""
            });
        }

        OSSaveSystem.Save(saveData);
        cachedSave = saveData;

        Debug.Log($"[OS] SaveOS completed. subBlocks={saveData.subBlocks.Count}");
    }


    private IEnumerator CoPostApplyLayoutSanityNextFrame()
    {
        yield return null;              // ✅ 1프레임 대기
        PostApplyLayoutSanity();        // ✅ 한 번 더
    }

    public void LoadOS()
    {
        var data = OSSaveSystem.Load();
        if (data == null) return;

        // subBlocks 먼저 복원
        subBlockJsonByKey.Clear();
        if (data.subBlocks != null)
            foreach (var sb in data.subBlocks)
                if (sb != null && !string.IsNullOrEmpty(sb.key))
                    subBlockJsonByKey[sb.key] = sb.json ?? "";

        ApplyWindows(data);
        ApplyIcons(data);
        cachedSave = data;

        PostApplyLayoutSanity();
        StartCoroutine(CoPostApplyLayoutSanityNextFrame());

        Debug.Log($"[OS] LoadOS applied. subBlocks={subBlockJsonByKey.Count}");
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

    private Vector2 ConvertToWindowsRootLocal(RectTransform from)
    {
        if (from == null) return Vector2.zero;

        RectTransform parent = windowsRoot != null
            ? windowsRoot
            : (RectTransform)transform;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, from.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            screen,
            null,
            out var local);

        return local;
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

        if (w.IsAnimating) return; // ✅ 애니 중이면 최소화 요청 무시(레이스 방지)

        EnsureFocused(appId);

        w.CacheRestorePos(w.GetWindowRoot().anchoredPosition);

        var btnRect = taskbarManager?.GetButtonRect(appId);

        Vector2 target =
            btnRect != null
            ? ConvertToWindowLocal(btnRect)   // ✅ 변환
            : new Vector2(0, -500);

        w.PlayMinimize(target, () =>
        {
            w.SetMinimized(true);
            taskbarManager?.SetMinimized(appId, true);

            if (!suppressAutoFocus)
                FocusNextTopWindow(appId);





            RequestAutoSave();
        });
    }




    public void Restore(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (!w.IsMinimized) { Focus(appId); return; }

        if (w.IsAnimating) return; // ✅ 애니 중이면 최소화 요청 무시(레이스 방지)

        var btnRect = taskbarManager?.GetButtonRect(appId);

        Vector2 from =
            btnRect != null
            ? ConvertToWindowLocal(btnRect)
            : w.GetWindowRoot().anchoredPosition;

        w.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);


        w.transform.SetAsLastSibling();

        w.PlayRestore(from, () =>
        {
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




    private VNOSBridge FindBridge(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var wc) || wc == null) return null;
        return wc.GetComponentInChildren<VNOSBridge>(true);
    }


    public void Close(string appId)
    {

        // 1.5) ✅ ExitLocked면 닫기 무시 (VN이 강제 잠금 걸어둔 상태)
        if (IsExitLocked(appId))
        {
            Debug.Log("[OS] Close ignored (ExitLocked).");
            return;
        }
        // 0) Close 입력락
        if (IsCloseLocked())
        {
            Debug.Log("[OS] Close ignored (close lock).");
            return;
        }

        // 1) 대상 창 확인
        if (!openWindows.TryGetValue(appId, out var window) || window == null)
            return;

        // 2) ✅ VN Drink 모드면: 닫기 자체 무시(팝업도 금지)
        if (IsVNInDrinkMode(appId))
        {
            Debug.Log("[OS] Close ignored (VN drink mode).");
            return;
        }

        // 3) (선택) 닫기 요청 시 포커스 확보
        EnsureFocused(appId);

        // 4) 브릿지 찾기
        closeHandlers.TryGetValue(appId, out var handler); // handler: IVNCloseRequestHandler (or VNOSBridge)

        // 5) VN이 닫기를 막는 상태면: OS는 닫지 않고, VN에게 “닫기 요청”만 전달
        if (handler != null && !handler.CanCloseNow())
        {
            Debug.Log("[OS] Close blocked by VN.");

            // ✅ OnForceCloseRequested는 "VNOSBridge에만" 있으니까 캐스팅 필요
            if (handler is PPP.BLUE.VN.VNOSBridge bridge)
            {
                void OnForce()
                {
                    bridge.OnForceCloseRequested -= OnForce;

                    if (!openWindows.TryGetValue(appId, out var w2) || w2 == null)
                        return;

                    PerformClose(w2, appId);
                }

                bridge.OnForceCloseRequested -= OnForce;
                bridge.OnForceCloseRequested += OnForce;
            }

            // ✅ Notify는 인터페이스에 있으니 handler로 호출
            handler.NotifyCloseRequested();
            return;
        }

        // 6) 검증 통과 → 실제 닫기
        PerformClose(window, appId);
    }

    private void CloseInternal(string appId, WindowController window)
    {
        // window가 이미 Destroy 되었을 수도 있으니 가드(안전)
        if (window == null) return;

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

    private void PerformClose(WindowController window, string appId)
    {
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


    public void OnTaskbarButtonPressed(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;

        if (w.IsMinimized) Restore(appId);
        else Focus(appId); // (윈도우식으로 “이미 활성창이면 최소화”로 바꾸고 싶으면 여기만 수정)
    }


    public bool IsMinimized(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return true;
        return w.IsMinimized;
    }

    public void MinimizeNoFocus(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var target) || target == null)
            return;

    
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
        RequestAutoSave();
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

    public void MinimizeAllAnimated()
    {
        Debug.Log("[OS] MinimizeAllAnimated disabled");
    }


    public void MinimizeNoFocusAnimated(string appId)
    {
        Debug.Log("[OS] MinimizeNoFocusAnimated disabled");
    }


    public void RestoreAllAnimated()
    {
        Debug.Log("[OS] RestoreAllAnimated disabled");
    }

    public void RestoreNoFocusAnimated(string appId)
    {
        Debug.Log("[OS] RestoreNoFocusAnimated disabled");
    }



    public void ShowDesktop()
    {
        Debug.Log("[OS] ShowDesktop disabled");
    }


    public void RestoreDesktop()
    {
        Debug.Log("[OS] RestoreDesktop disabled");
    }

    private void Minimize_NoFocusAnimated(string appId)
    {
        Debug.Log("[OS] Minimize_NoFocusAnimated disabled");
    }


    private void Restore_NoFocusAnimated(string appId)
    {
        Debug.Log("[OS] Restore_NoFocusAnimated disabled");
    }



    public void MinimizeAll()
    {
        Debug.Log("[OS] MinimizeAll disabled");
    }

    public void RestoreAll()
    {
        Debug.Log("[OS] RestoreAll disabled");
    }


    private bool IsVNInDrinkMode(string appId)
    {
        // VN 앱만 검사하고 싶으면 appId 비교해도 됨(선택)
        if (!openWindows.TryGetValue(appId, out var wc) || wc == null) return false;

        var policy = wc.GetComponentInChildren<PPP.BLUE.VN.VNPolicyController>(true);
        if (policy == null) return false;

        return policy.IsInDrinkMode;
    }



    private Vector2 GetTaskbarTargetInWindowsRoot(string appId, Vector2 fallback)
    {
        if (taskbarManager == null) return fallback;

        var btnRect = taskbarManager.GetButtonRect(appId);
        if (btnRect == null) return fallback;

        // 버튼 월드 좌표 → windowsRoot 로컬 좌표 변환
        Vector3 world = btnRect.TransformPoint(btnRect.rect.center);
        Vector2 local;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowsRoot,
            RectTransformUtility.WorldToScreenPoint(null, world),
            null,
            out local
        );

        return local;
    }

    private IEnumerator CoFinalizeSpawn(WindowController w)
    {
        yield return null; // 1프레임 대기 (Canvas/Layout 안정화)

        if (w == null) yield break;

        var rect = w.GetWindowRoot();
        if (rect == null) yield break;

        rect.anchoredPosition =
            ClampToCanvas(rect.anchoredPosition, rect);
    }

    private Vector2 ClampToCanvas(Vector2 pos, RectTransform rect)
    {
        if (canvasRect == null) return pos;

        Rect c = canvasRect.rect;
        Vector2 size = rect.rect.size;
        Vector2 pivot = rect.pivot;

        float minX = c.xMin + size.x * pivot.x;
        float maxX = c.xMax - size.x * (1f - pivot.x);
        float minY = c.yMin + size.y * pivot.y;
        float maxY = c.yMax - size.y * (1f - pivot.y);

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }

    // ✅ 실행 중 캐시(저장 전까지 유지)
    private readonly Dictionary<string, string> subBlockJsonByKey = new();
    private readonly Dictionary<string, IVNCloseRequestHandler> closeHandlers = new();
    private readonly HashSet<string> exitLockedApps = new();

    public void SetCloseHandler(string appId, IVNCloseRequestHandler handler)
    {
        if (string.IsNullOrEmpty(appId) || handler == null) return;
        closeHandlers[appId] = handler; // appId당 1개만 유지
    }

    public void ClearCloseHandler(string appId, IVNCloseRequestHandler handler)
    {
        if (string.IsNullOrEmpty(appId) || handler == null) return;
        if (closeHandlers.TryGetValue(appId, out var cur) && ReferenceEquals(cur, handler))
            closeHandlers.Remove(appId);
    }


    public VNWindowState GetWindowState(string appId)
    {
        // ----------------------------
        // 0) not found → minimized 처리
        // ----------------------------
        if (string.IsNullOrEmpty(appId) ||
            !openWindows.TryGetValue(appId, out var wc) ||
            wc == null)
        {
            var st = new VNWindowState(isFocused: false, isMinimized: true);

            // 상태 변화 있을 때만 로그
            if (logWindowState && ShouldLogState(appId, st))
            {
                Debug.Log($"[OS] GetWindowState appId={appId} -> not found (treat minimized)");
            }

            _lastStates[appId] = st;
            return st;
        }

        // ----------------------------
        // 1) 정상 상태 계산
        // ----------------------------
        bool minimized = wc.IsMinimized;
        bool focused = !minimized;

        var state = new VNWindowState(focused, minimized);

        // ----------------------------
        // 2) 상태 변화 있을 때만 로그
        // ----------------------------
        if (logWindowState && ShouldLogState(appId, state))
        {
            Debug.Log($"[OS] GetWindowState appId={appId} min={minimized} focused={focused}");
        }

        _lastStates[appId] = state;
        return state;
    }

    private bool ShouldLogState(string appId, VNWindowState newState)
    {
        if (!_lastStates.TryGetValue(appId, out var oldState))
            return true;

        return oldState.IsFocused != newState.IsFocused ||
               oldState.IsMinimized != newState.IsMinimized;
    }


    private WindowController FindWindowByAppId(string appId)
    {
        // ✅ 너희가 이미 appId로 찾는 코드가 있을 거야:
        // - Dictionary<string, WindowController> windowsByAppId
        // - 열린 창 리스트에서 wc.AppId == appId 찾기
        //
        // 예시(리스트):
        foreach (var wc in FindObjectsOfType<WindowController>(true))
            if (wc != null && wc.AppId == appId) return wc;

        return null;
    }

    private bool IsWindowFocused(WindowController wc)
    {
        // ✅ 너희 포커스 시스템이 있으면 거기 사용
        // 예: return focusedWindow == wc;
        return true; // 포커스 무시하고 싶으면 true로 둬도 됨(단, minimized일 때는 위에서 false 처리됨)
    }


    public void SaveSubBlock(string key, object data)
    {
        if (string.IsNullOrEmpty(key) || data == null) return;

        var json = JsonUtility.ToJson(data);
        subBlockJsonByKey[key] = json;

        Debug.Log($"[OS] SaveSubBlock key={key} len={json?.Length ?? 0} total={subBlockJsonByKey.Count}");
    }

    public T LoadSubBlock<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!subBlockJsonByKey.TryGetValue(key, out var json)) return null;
        if (string.IsNullOrEmpty(json)) return null;

        Debug.Log($"[OS] LoadSubBlock key={key} len={json.Length} total={subBlockJsonByKey.Count}");
        return JsonUtility.FromJson<T>(json);
    }

    public bool IsExitLocked(string appId)
    {
        if (string.IsNullOrEmpty(appId)) return false;
        return exitLockedByAppId.TryGetValue(appId, out var v) && v;
    }

    // IVNHostOS 구현
    public void SetExitLocked(string appId, bool locked)
    {
        if (string.IsNullOrEmpty(appId)) return;
        exitLockedByAppId[appId] = locked;
    }


}
