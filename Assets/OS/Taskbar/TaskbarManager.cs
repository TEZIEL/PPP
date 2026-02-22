using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder, HorizontalLayoutGroup

public class TaskbarManager : MonoBehaviour
{
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private TaskbarButtonController buttonPrefab;
    [SerializeField] private RectTransform buttonRoot;

    [Header("UX")]
    [SerializeField] private float shiftDuration = 0.30f; // ← 여기만 취향대로 조절
    [SerializeField] private AnimationCurve shiftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private HorizontalLayoutGroup _layout;
    private Coroutine _shiftCo;

    private readonly Dictionary<string, TaskbarButtonController> buttons = new();
    private readonly HashSet<string> minimizedApps = new();

    private void Awake()
    {
        if (buttonRoot == null) buttonRoot = (RectTransform)transform;
        _layout = buttonRoot.GetComponent<HorizontalLayoutGroup>();
    }

    public void Add(string appId, string displayName, WindowController window)
    {
        if (buttons.ContainsKey(appId) || buttonPrefab == null) return;

        minimizedApps.Remove(appId);

        Transform root = buttonRoot != null ? buttonRoot : transform;
        var button = Instantiate(buttonPrefab, root);

        button.Initialize(appId, displayName, windowManager, window);

        buttons.Add(appId, button);
        SetState(appId, false, false);

        ForceRebuild();
    }

    public void Remove(string appId)
    {
        minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var removed) || removed == null) return;
        buttons.Remove(appId);

        // ✅ 1) 제거 직전 "현재 위치" 스냅샷
        var fromPos = CaptureCurrentPositions(except: removed);

        // ✅ 2) 레이아웃에서 즉시 빠지게(중요!)
        var le = removed.GetComponent<LayoutElement>();
        if (le == null) le = removed.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        // ✅ 3) 화면에서는 즉시 사라지게(연출은 지금처럼 즉시 OK)
        removed.gameObject.SetActive(false);

        // ✅ 4) 레이아웃 강제 갱신 → 목표 위치 계산
        ForceRebuild();
        var toPos = CaptureCurrentPositions(except: null);

        // ✅ 5) from -> to로 부드럽게 이동 (이동 끝나면 Destroy)
        StartShiftAnimation(fromPos, toPos, removed.gameObject);
    }

    // -------------------------

    public void SetActive(string appId, bool isActive)
    {
        SetState(appId, isActive, minimizedApps.Contains(appId));
    }

    public void SetMinimized(string appId, bool minimized)
    {
        if (minimized) minimizedApps.Add(appId);
        else minimizedApps.Remove(appId);

        if (!buttons.TryGetValue(appId, out var btn) || btn == null) return;
        btn.SetMinimizedVisual(minimized);
    }

    private void SetState(string appId, bool isActive, bool isMinimized)
    {
        if (!buttons.TryGetValue(appId, out var button) || button == null) return;
        button.SetMinimizedVisual(isMinimized);
    }

    public RectTransform GetButtonRect(string appId)
    {
        if (buttons.TryGetValue(appId, out var b) && b != null) return b.Rect;
        return null;
    }

    // -------------------------
    // ✅ Smooth shift helpers
    // -------------------------

    private Dictionary<RectTransform, Vector2> CaptureCurrentPositions(TaskbarButtonController except)
    {
        var map = new Dictionary<RectTransform, Vector2>();

        foreach (Transform child in buttonRoot)
        {
            if (child == null) continue;

            if (!child.gameObject.activeInHierarchy) continue; // ✅ 비활성은 레이아웃/애니 대상 제외

            var tbc = child.GetComponent<TaskbarButtonController>();
            if (except != null && tbc == except) continue;

            var rt = child as RectTransform;
            if (rt == null) continue;

            map[rt] = rt.anchoredPosition;
        }

        return map;
    }

    private void ForceRebuild()
    {
        if (buttonRoot == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRoot);
    }

    private void StartShiftAnimation(
        Dictionary<RectTransform, Vector2> fromPos,
        Dictionary<RectTransform, Vector2> toPos, GameObject removedGo)
    {
        if (_shiftCo != null) StopCoroutine(_shiftCo);
        _shiftCo = StartCoroutine(CoShift(fromPos, toPos, removedGo));
    }

    private IEnumerator CoShift(
        Dictionary<RectTransform, Vector2> fromPos,
        Dictionary<RectTransform, Vector2> toPos, GameObject removedGo)
    {
        // LayoutGroup이 애니 중에 위치를 계속 덮어쓰는 걸 방지
        if (_layout != null) _layout.enabled = false;

        // 시작 위치를 from으로 강제 세팅 (레이아웃이 이미 to로 배치해둔 상태라 되돌려야 함)
        foreach (var kv in toPos)
        {
            var rt = kv.Key;
            if (rt == null) continue;

            if (fromPos.TryGetValue(rt, out var fp))
                rt.anchoredPosition = fp;
        }

        float dur = Mathf.Max(0.01f, shiftDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float s = shiftCurve != null ? shiftCurve.Evaluate(Mathf.Clamp01(t)) : Mathf.SmoothStep(0f, 1f, t);

            foreach (var kv in toPos)
            {
                var rt = kv.Key;
                if (rt == null) continue;

                Vector2 a = fromPos.TryGetValue(rt, out var fp) ? fp : rt.anchoredPosition;
                Vector2 b = kv.Value;
                rt.anchoredPosition = Vector2.LerpUnclamped(a, b, s);
            }

            yield return null;
        }

        // 최종 위치 고정
        foreach (var kv in toPos)
        {
            var rt = kv.Key;
            if (rt == null) continue;
            rt.anchoredPosition = kv.Value;
        }

        // LayoutGroup 다시 켜고 한번 더 정리
        if (_layout != null) _layout.enabled = true;
        ForceRebuild();


        if (removedGo != null) Destroy(removedGo);
        _shiftCo = null;
    }
}