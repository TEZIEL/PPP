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

    private string lastActiveBeforeMinimizeAll;

    private string activeAppId;
    public string ActiveAppId => activeAppId;
    private OSSaveData cachedSave;

    private bool suppressAutoFocus;
    public void BeginBatch() => suppressAutoFocus = true;
    public void EndBatch() => suppressAutoFocus = false;
    private bool isShowDesktop;
    private readonly List<string> lastShownDesktop = new(); // 직전에 "전체 최소화"로 내려간 창들

    public void ToggleShowDesktop()
    {
        if (!isShowDesktop)
            ShowDesktop();
        else
            RestoreDesktop();
    }



    public void OpenSimple(string appId, WindowController prefab)
    {
        Open(appId, prefab, null, Vector2.zero, new Vector2(600, 400));
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


    public void OnTaskbarButtonPressed(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;

        if (w.IsMinimized) Restore(appId);
        else Focus(appId); // (윈도우식으로 “이미 활성창이면 최소화”로 바꾸고 싶으면 여기만 수정)
    }


    public bool IsMinimized(string appId)
    {
        return openWindows.TryGetValue(appId, out var target) && target != null && target.IsMinimized;
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
        // 루프 중 포커스 자동 이동 막기
        BeginBatch();

        // 딕셔너리 foreach 도중 상태 변해도 안전하도록 키 복사
        var ids = new List<string>(openWindows.Keys);

        foreach (var id in ids)
        {
            if (!openWindows.TryGetValue(id, out var w) || w == null) continue;
            if (w.IsMinimized) continue;

            // ✅ 애니 버전: Focus/FocusNextTopWindow 절대 호출하지 않는다
            MinimizeNoFocusAnimated(id);
        }


        // 전체 최소화면 active는 꺼도 됨(원하면)
        activeAppId = null;
        foreach (var id in ids) taskbarManager?.SetActive(id, false);

        EndBatch();
    }


    public void MinimizeNoFocusAnimated(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (w.IsMinimized) return;

        w.CacheRestorePos(w.GetWindowRoot().anchoredPosition);

        var btnRect = taskbarManager?.GetButtonRect(appId);
        Vector2 target =
            btnRect != null ? ConvertToWindowLocal(btnRect)
                            : new Vector2(0, -500);

        w.PlayMinimize(target, () =>
        {
            w.SetMinimized(true);
            taskbarManager?.SetMinimized(appId, true);
            RequestAutoSave();
        });
    }


    public void RestoreAllAnimated()
    {
        BeginBatch();

        var ids = new List<string>(openWindows.Keys);

        foreach (var id in ids)
        {
            if (!openWindows.TryGetValue(id, out var w) || w == null) continue;
            if (!w.IsMinimized) continue;

            RestoreNoFocusAnimated(id);
        }

        EndBatch();
    }

    public void RestoreNoFocusAnimated(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (!w.IsMinimized) return;

        var btnRect = taskbarManager?.GetButtonRect(appId);
        Vector2 from =
            btnRect != null ? ConvertToWindowLocal(btnRect)
                            : w.GetWindowRoot().anchoredPosition;

        w.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);

        // ✅ Z-order 안 건드림 (SetAsLastSibling 금지)
        w.PlayRestore(from, () =>
        {
            RequestAutoSave();
        }, 0.12f, bringToFront: false);
    }



    public void ShowDesktop()
    {
        if (isShowDesktop) return;

        isShowDesktop = true;
        lastShownDesktop.Clear();

        // 포커스 흔들림 방지
        BeginBatch();

        foreach (var kv in openWindows)
        {
            string id = kv.Key;
            var w = kv.Value;
            if (w == null) continue;
            if (w.IsMinimized) continue;        // 이미 내려간 건 제외

            // (선택) 핀된 창은 제외하고 싶으면 WindowController에 IsPinned 노출해서 여기서 continue
            // if (w.IsPinned) continue;

            lastShownDesktop.Add(id);

            // 목표: 태스크바 버튼 위치
            var btnRect = taskbarManager?.GetButtonRect(id);
            Vector2 target = (btnRect != null)
                ? ConvertToWindowsRootLocal(btnRect)
                : new Vector2(0, -500);

            // 개별 최소화 애니
            w.CacheRestorePos(w.GetWindowRoot().anchoredPosition);

            w.PlayMinimize(target, () =>
            {
                w.SetMinimized(true);
                taskbarManager?.SetMinimized(id, true);
            });
        }

        EndBatch();

        // 바탕화면 상태이므로 activeAppId 비우고 taskbar active도 끄기
        activeAppId = null;
        foreach (var kv in openWindows)
            taskbarManager?.SetActive(kv.Key, false);

        RequestAutoSave();
    }


    public void RestoreDesktop()
    {
        if (!isShowDesktop) return;

        isShowDesktop = false;

        BeginBatch();

        // 마지막에 활성화되던 창을 기억하고 싶으면 별도 저장해도 됨.
        // 일단 "마지막 리스트의 끝"을 최종 포커스로 잡는 식이 자연스러움.
        string focusId = null;

        foreach (var id in lastShownDesktop)
        {
            if (!openWindows.TryGetValue(id, out var w) || w == null) continue;
            if (!w.IsMinimized) continue;

            var btnRect = taskbarManager?.GetButtonRect(id);
            Vector2 from = (btnRect != null)
                ? ConvertToWindowsRootLocal(btnRect)
                : w.GetWindowRoot().anchoredPosition;

            // 보이게 먼저 풀고(알파/레이캐스트)
            w.SetMinimized(false);
            taskbarManager?.SetMinimized(id, false);

            // 복원 시 위로 올라오게
           
            w.PlayRestore(from, () =>
            {
                // 개별 복원 완료 콜백(필요하면)
            });

            focusId = id; // 마지막 것을 포커스로
        }

        EndBatch();

        if (!string.IsNullOrEmpty(focusId))
            Focus(focusId);

        lastShownDesktop.Clear();
        RequestAutoSave();
    }

    private void Minimize_NoFocusAnimated(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (w.IsMinimized) return;

        w.CacheRestorePos(w.GetWindowRoot().anchoredPosition);

        var btnRect = taskbarManager?.GetButtonRect(appId);

        Vector2 target =
            btnRect != null ? ConvertToWindowLocal(btnRect)
                            : new Vector2(0, -500);

        w.PlayMinimize(target, () =>
        {
            w.SetMinimized(true);
            taskbarManager?.SetMinimized(appId, true);
            RequestAutoSave();
        });
    }


    private void Restore_NoFocusAnimated(string appId)
    {
        if (!openWindows.TryGetValue(appId, out var w) || w == null) return;
        if (!w.IsMinimized) return;

        var btnRect = taskbarManager?.GetButtonRect(appId);

        Vector2 from =
            btnRect != null ? ConvertToWindowLocal(btnRect)
                            : w.GetWindowRoot().anchoredPosition;

        w.SetMinimized(false);
        taskbarManager?.SetMinimized(appId, false);

        // ✅ Z-order 건드리지 않음 (SetAsLastSibling 금지)

        w.PlayRestore(from, () =>
        {
            RequestAutoSave();
        }, 0.12f, bringToFront: false);
    }



    public void MinimizeAll()
    {
        // ✅ 현재 포커스 저장 (복원 때 그대로 되돌릴 거)
        lastActiveBeforeMinimizeAll = activeAppId;

        BeginBatch(); // suppressAutoFocus = true

        var ids = new List<string>(openWindows.Keys);
        foreach (var id in ids)
            Minimize_NoFocusAnimated(id);

        EndBatch();

        // ✅ Win+D처럼: 일단 active는 비워두고(복원 시 되돌림)
        activeAppId = null;
        foreach (var pair in openWindows)
            taskbarManager?.SetActive(pair.Key, false);
    }

    public void RestoreAll()
    {
        BeginBatch();

        var ids = new List<string>(openWindows.Keys);
        foreach (var id in ids)
            Restore_NoFocusAnimated(id);

        EndBatch();

        // ✅ “원래 활성 창”을 기존 규칙(Focus)으로 그대로 복원
        if (!string.IsNullOrEmpty(lastActiveBeforeMinimizeAll) &&
            openWindows.TryGetValue(lastActiveBeforeMinimizeAll, out var w) &&
            w != null)
        {
            // Focus()가 기존 규칙(=SetAsLastSibling + ActiveVisual + TaskbarActive)을 책임짐
            Focus(lastActiveBeforeMinimizeAll);
        }
        else
        {
            // ✅ 원래 활성 창이 없으면 기존 규칙대로 “가장 위 창”으로 포커스
            FocusNextTopWindow(excludedAppId: null);
        }

        lastActiveBeforeMinimizeAll = null;
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


}
