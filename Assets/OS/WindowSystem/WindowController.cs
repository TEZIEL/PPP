using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowController : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [SerializeField] private string appId;

    [Header("Window Parts")]
    [SerializeField] private RectTransform windowRoot;
    [SerializeField] private RectTransform titleBar;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Drag Bounds")]
    [SerializeField] private float dragOverflow = 250f;
    [SerializeField] private float returnDuration = 0.18f;
    [SerializeField] private float returnSnapEpsilon = 0.5f;
    [SerializeField] private float softSnapDuration = 0.06f;
    [SerializeField] private float softSnapMinDistance = 2f;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinToggleButton;

    [Header("Skin (Optional)")]
    [SerializeField] private UISkin skin;
    [SerializeField] private Image titleBarImage;
    [SerializeField] private Image frameImage;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.12f;
    [SerializeField] private float closeDuration = 0.10f;
    [SerializeField] private Vector3 openFromScale = new Vector3(0.92f, 0.92f, 1f);

    private WindowManager owner;          // WindowManager.Open()에서 Initialize로 주입
    private RectTransform canvasRect;     // WindowManager.Open()에서 Initialize로 주입
    private bool isPinned;

    public string AppId => appId;
    public bool IsMinimized { get; private set; }
    public RectTransform WindowRoot => windowRoot;

    private Vector2 dragOffset;
    private Coroutine returnRoutine;
    private Coroutine animRoutine;

    // ✅ ContentRoot: contentPrefab이 붙을 자리
    [Header("Content")]
    [SerializeField] private RectTransform contentRoot;
    public RectTransform ContentRoot => contentRoot;

    // 최소화/복원 애니용 캐시
    private Vector2 restorePos;
    private bool hasRestorePos;
    private Coroutine moveScaleRoutine;

    public void ForceClampNow(float overflow = 0f)
{
    if (windowRoot == null) return;
    var clamped = ClampWindowAnchoredPosition(windowRoot.anchoredPosition, overflow);
    windowRoot.anchoredPosition = clamped;
}


    private void Awake()
    {
        if (windowRoot == null)
            windowRoot = transform as RectTransform;

        if (canvasGroup == null)
        {
            canvasGroup = windowRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = windowRoot.gameObject.AddComponent<CanvasGroup>();
        }

        HookButtons();
    }

    public void Initialize(WindowManager wm, string id, RectTransform rootCanvas)
    {
        owner = wm;
        appId = id;
        canvasRect = rootCanvas;
    }


    public void InjectManager(WindowManager manager)
    {
        // 예전 코드/호환용. Initialize로 owner가 세팅되지만
        // 기존 시스템에서 InjectManager를 호출하므로 안전하게 받아줌.
        if (owner == null) owner = manager;
    }

    // =========================
    // Pin
    // =========================
    public void TogglePinFromShortcut()
    {
        isPinned = !isPinned;
        // TODO: 핀 아이콘/색 변화는 나중에 여기서 처리
    }

    // =========================
    // Basic Window API
    // =========================
    public string GetAppId() => appId;
    public RectTransform GetWindowRoot() => windowRoot;

    public void SetWindowPosition(Vector2 anchoredPosition)
    {
        if (windowRoot == null) return;
        windowRoot.anchoredPosition = anchoredPosition;
    }

    public void SetWindowSize(Vector2 size)
    {
        if (windowRoot == null) return;
        windowRoot.sizeDelta = size;
    }

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;

        // “완전 숨김” 방식 유지
        canvasGroup.alpha = minimized ? 0f : 1f;
        canvasGroup.interactable = !minimized;
        canvasGroup.blocksRaycasts = !minimized;
    }

    public void SetActiveVisual(bool active)
    {
        if (skin == null) return;

        if (titleBarImage != null)
            titleBarImage.color = active ? skin.titleActiveColor : skin.titleInactiveColor;

        if (frameImage != null)
            frameImage.color = active ? skin.frameActiveColor : skin.frameInactiveColor;
    }

    // =========================
    // Focus / Drag
    // =========================
    public void OnPointerDown(PointerEventData e)
    {
        owner?.Focus(appId);
    }

    private bool TryGetPointerLocal(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = default;
        if (canvasRect == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out localPoint);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 핀 고정: 드래그 금지
        if (canvasRect == null || windowRoot == null) return;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        if (!TryGetPointerLocal(eventData, out var pointerLocal)) return;

        dragOffset = windowRoot.anchoredPosition - pointerLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 핀 고정: 드래그 금지
        if (canvasRect == null || windowRoot == null) return;
        if (!TryGetPointerLocal(eventData, out var pointerLocal)) return;

        Vector2 target = pointerLocal + dragOffset;
        target = ClampWindowAnchoredPosition(target, dragOverflow);

        windowRoot.anchoredPosition = target;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 핀 고정: 드래그 금지
        if (canvasRect == null || windowRoot == null) return;

        Vector2 target = ClampWindowAnchoredPosition(windowRoot.anchoredPosition, dragOverflow);
        float dist = (target - windowRoot.anchoredPosition).magnitude;

        if (dist < returnSnapEpsilon)
        {
            owner?.RequestAutoSave();
            return;
        }

        if (returnRoutine != null) StopCoroutine(returnRoutine);

        float dur = (dist <= softSnapMinDistance) ? softSnapDuration : returnDuration;
        returnRoutine = StartCoroutine(ReturnToBounds(target, dur));
    }

    private IEnumerator ReturnToBounds(Vector2 target, float duration)
    {
        Vector2 start = windowRoot.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            windowRoot.anchoredPosition = Vector2.LerpUnclamped(start, target, smooth);
            yield return null;
        }

        windowRoot.anchoredPosition = target;
        returnRoutine = null;

        owner?.RequestAutoSave();
    }

    private Vector2 ClampWindowAnchoredPosition(Vector2 desired, float allowOverflow)
    {
        if (windowRoot == null || windowRoot.parent == null)
            return desired;

        Rect c = ((RectTransform)windowRoot.parent).rect;

        Vector2 size = windowRoot.rect.size;
        Vector2 pivot = windowRoot.pivot;

        float left = desired.x - size.x * pivot.x;
        float right = desired.x + size.x * (1f - pivot.x);
        float bottom = desired.y - size.y * pivot.y;
        float top = desired.y + size.y * (1f - pivot.y);

        float minX = c.xMin - allowOverflow;
        float maxX = c.xMax + allowOverflow;
        float minY = c.yMin - allowOverflow;
        float maxY = c.yMax + allowOverflow;

        float dx = 0f;
        if (left < minX) dx = minX - left;
        else if (right > maxX) dx = maxX - right;

        float dy = 0f;
        if (bottom < minY) dy = minY - bottom;
        else if (top > maxY) dy = maxY - top;

        return new Vector2(desired.x + dx, desired.y + dy);
    }

    // =========================
    // Animation APIs (WindowManager가 호출)
    // =========================
    public void CacheRestorePos(Vector2 anchoredPos)
    {
        restorePos = anchoredPos;
        hasRestorePos = true;
    }

    public void PlayOpen()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(CoScale(openFromScale, Vector3.one, openDuration, null));
    }

    public void PlayClose(Action onDone)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(CoScale(transform.localScale, openFromScale, closeDuration, onDone));
    }

    public void PlayMinimize(Vector2 targetAnchoredPos, Action onDone, float duration = 0.12f)
    {
        if (moveScaleRoutine != null) StopCoroutine(moveScaleRoutine);
        moveScaleRoutine = StartCoroutine(CoMoveAndScale(
            fromPos: windowRoot.anchoredPosition,
            toPos: targetAnchoredPos,
            fromScale: Vector3.one,
            toScale: new Vector3(0.85f, 0.85f, 1f),
            duration: duration,
            onDone: onDone
        ));
    }

    public void PlayRestore(Vector2 fromAnchoredPos, Action onDone, float duration = 0.12f, bool bringToFront = true)
    {
        // ✅ 필요할 때만 앞으로
        if (bringToFront)
            transform.SetAsLastSibling();

        Vector2 toPos = (restorePos == Vector2.zero) ? windowRoot.anchoredPosition : restorePos;

        // 시작점 세팅
        windowRoot.anchoredPosition = fromAnchoredPos;
        transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        if (bringToFront)
            transform.SetAsLastSibling();

        if (moveScaleRoutine != null) StopCoroutine(moveScaleRoutine);
        moveScaleRoutine = StartCoroutine(CoMoveAndScale(
            fromPos: fromAnchoredPos,
            toPos: toPos,
            fromScale: new Vector3(0.85f, 0.85f, 1f),
            toScale: Vector3.one,
            duration: duration,
            onDone: onDone
        ));
    }

    private IEnumerator CoScale(Vector3 from, Vector3 to, float duration, Action onDone)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float s = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.LerpUnclamped(from, to, s);
            yield return null;
        }

        transform.localScale = to;
        animRoutine = null;
        onDone?.Invoke();
    }

    private IEnumerator CoMoveAndScale(
        Vector2 fromPos, Vector2 toPos,
        Vector3 fromScale, Vector3 toScale,
        float duration, Action onDone)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            if (windowRoot != null)
                windowRoot.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, s);

            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, s);
            yield return null;
        }

        if (windowRoot != null) windowRoot.anchoredPosition = toPos;
        transform.localScale = toScale;

        moveScaleRoutine = null;
        onDone?.Invoke();
    }

    // =========================
    // Buttons
    // =========================
    private void HookButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => owner?.Close(appId));

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(() => owner?.Minimize(appId));

        if (pinToggleButton != null)
            pinToggleButton.onClick.AddListener(() => isPinned = !isPinned);
    }
}


   