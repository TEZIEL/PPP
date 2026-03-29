using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // ✅ 추가

public class WindowController : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [SerializeField] private string appId;

    [Header("Title UI")]
    [SerializeField] private TMP_Text titleText; // ✅ 인스펙터에 타이틀바 TMP 연결
    [SerializeField] private Image titleIconImage; // (옵션) 앱별 타이틀 아이콘

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
    [SerializeField] private Image underImage;
    [SerializeField] private Image sideImage;
    [SerializeField] private Image shadowImage; // ✅ 포커스 그림자
    [SerializeField] private Color fallbackWindowTint = Color.white;

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

    public bool IsAnimating { get; private set; }

    [Header("Focus")]
    [SerializeField] private PPP.BLUE.VN.VNPolicyController vnPolicy;

    private static readonly List<RaycastResult> focusRaycastResults = new();
    private static int cachedRaycastFrame = -1;
    private static GameObject cachedTopRaycastHit;
    private int lastFocusHandledFrame = -1;
    private Color _themeWindowTint = Color.white;
    private bool _hasThemeWindowTint;
    private bool _isActiveVisual;


    private bool TryBeginAnim()
    {
        if (IsAnimating) return false;
        IsAnimating = true;
        return true;
    }


    private void BeginAnim()
    {
        IsAnimating = true;

        // ✅ 애니 중 클릭/드래그 입력 차단(원하면)
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void EndAnim()
    {
        IsAnimating = false;

        // 최소화 상태면 계속 차단, 아니면 다시 입력 허용
        if (canvasGroup != null)
        {
            bool blocked = IsMinimized;
            canvasGroup.interactable = !blocked;
            canvasGroup.blocksRaycasts = !blocked;
        }
    }

    public void ForceClampNow(float overflow = 0f)
    {
        if (windowRoot == null) return;
        var clamped = ClampWindowAnchoredPosition(windowRoot.anchoredPosition, overflow);
        windowRoot.anchoredPosition = clamped;
    }

    private bool CanAcceptCommand()
    {
        // 애니 중이면 입력/버튼/드래그/포커스 요청 무시
        return !IsAnimating;
    }

    public void Initialize(WindowManager wm, string id, RectTransform rootCanvas, string displayName, Sprite appIcon = null)
    {
        owner = wm;
        appId = id;
        canvasRect = rootCanvas;

        // ✅ 표시이름 주입
        if (titleText != null)
            titleText.text = displayName;

        SetTitleIcon(appIcon);
    }

    public void SetTitleIcon(Sprite icon)
    {
        if (titleIconImage == null) return;
        titleIconImage.sprite = icon;
        titleIconImage.enabled = icon != null;
    }

    // (기존 Initialize 시그니처를 쓰는 곳이 많으면 오버로드로 유지)

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

        if (vnPolicy == null)
        {
            vnPolicy = GetComponentInChildren<PPP.BLUE.VN.VNPolicyController>(true);
            if (vnPolicy == null)
                vnPolicy = GetComponentInParent<PPP.BLUE.VN.VNPolicyController>(true);
        }

        HookButtons();

        _themeWindowTint = fallbackWindowTint;
        _hasThemeWindowTint = false;
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
        SoundManager.Instance.PlayOS(OSSoundEvent.Pin); // 🔥 여기
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



        // “완전 숨김”
        canvasGroup.alpha = minimized ? 0f : 1f;
        canvasGroup.interactable = !minimized;
        canvasGroup.blocksRaycasts = !minimized;

        // ✅ 중요: 최소화/복원 어느 쪽이든, UI Selected를 끊어준다
        // 그래야 Space/Enter(Submit)가 작업표시줄 버튼/창 버튼에 먹지 않음
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void SetActiveVisual(bool active)
    {
        _isActiveVisual = active;
        var tint = ResolveWindowTint();

        if (titleBarImage != null)
        {
            var spr = skin != null ? (active ? skin.titleActive : skin.titleInactive) : null;
            if (spr != null) titleBarImage.sprite = spr;
            titleBarImage.color = tint;
        }

        if (underImage != null)
        {
            var spr = skin != null ? (active ? skin.underActive : skin.underInactive) : null;
            if (spr != null) underImage.sprite = spr;
            underImage.color = tint;
        }

        if (frameImage != null) frameImage.color = tint;
        if (sideImage != null) sideImage.color = tint;

        if (shadowImage != null)
            shadowImage.gameObject.SetActive(active);
    }

    public void ApplyTheme(ThemeData theme)
    {
        if (theme != null)
        {
            _themeWindowTint = theme.windowTint;
            _hasThemeWindowTint = true;
        }
        else
        {
            _themeWindowTint = fallbackWindowTint;
            _hasThemeWindowTint = false;
        }

        SetActiveVisual(_isActiveVisual);
    }

    private Color ResolveWindowTint()
    {
        if (_hasThemeWindowTint) return _themeWindowTint;
        if (skin != null) return skin.tint;
        return fallbackWindowTint;
    }

    // =========================
    // Focus / Drag
    // =========================
    public void OnPointerDown(PointerEventData e)
    {
        TryFocusFromPointerDown(e != null ? e.pointerPressRaycast.gameObject : gameObject);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        TryFocusFromPointerDown(GetTopRaycastHitObject());
    }

    private void TryFocusFromPointerDown(GameObject hitObject)
    {
        if (!CanAcceptCommand()) return;
        if (lastFocusHandledFrame == Time.frameCount) return;
        if (owner == null || windowRoot == null) return;
        if (hitObject == null) return;

        Transform hit = hitObject.transform;
        if (!hit.IsChildOf(windowRoot)) return;
        if (ShouldSkipFocusForModalHit(hit)) return;

        owner.Focus(appId);
        lastFocusHandledFrame = Time.frameCount;
    }

    private GameObject GetTopRaycastHitObject()
    {
        if (cachedRaycastFrame == Time.frameCount)
            return cachedTopRaycastHit;

        cachedRaycastFrame = Time.frameCount;
        cachedTopRaycastHit = null;

        var es = EventSystem.current;
        if (es == null) return null;

        var data = new PointerEventData(es) { position = Input.mousePosition };
        focusRaycastResults.Clear();
        es.RaycastAll(data, focusRaycastResults);

        if (focusRaycastResults.Count > 0)
            cachedTopRaycastHit = focusRaycastResults[0].gameObject;

        return cachedTopRaycastHit;
    }

    private bool ShouldSkipFocusForModalHit(Transform hit)
    {
        if (hit == null || vnPolicy == null) return false;

        if (vnPolicy.IsModalReasonOpen("ClosePopup") &&
            hit.GetComponentInParent<PPP.BLUE.VN.VNClosePopupController>(true) != null)
            return true;

               

        return false;
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
        if (IsAnimating) return;  // ✅ 추가
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
        if (IsAnimating) return;  // ✅ 추가
        if (isPinned) return; // ✅ 핀 고정: 드래그 금지
        if (canvasRect == null || windowRoot == null) return;
        if (!TryGetPointerLocal(eventData, out var pointerLocal)) return;

        Vector2 target = pointerLocal + dragOffset;
        target = ClampWindowAnchoredPosition(target, dragOverflow);

        windowRoot.anchoredPosition = target;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsAnimating) return;  // ✅ 추가
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
        if (!TryBeginAnim()) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(CoScale(openFromScale, Vector3.one, openDuration, () =>
        {
            animRoutine = null;
            EndAnim();
        }));
    }

    public void PlayClose(Action onDone)
    {
        if (!TryBeginAnim()) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(CoScale(transform.localScale, openFromScale, closeDuration, () =>
        {
            animRoutine = null;
            EndAnim();
            onDone?.Invoke();
        }));
    }

    public void PlayMinimize(Vector2 targetAnchoredPos, Action onDone, float duration = 0.12f)
    {
        if (!TryBeginAnim()) return;

        if (moveScaleRoutine != null) StopCoroutine(moveScaleRoutine);
        moveScaleRoutine = StartCoroutine(CoMoveAndScale(
            fromPos: windowRoot.anchoredPosition,
            toPos: targetAnchoredPos,
            fromScale: Vector3.one,
            toScale: new Vector3(0.85f, 0.85f, 1f),
            duration: duration,
            onDone: () =>
            {
                moveScaleRoutine = null;
                EndAnim();
                onDone?.Invoke();
            }
        ));
    }

    public void PlayRestore(Vector2 fromAnchoredPos, Action onDone, float duration = 0.12f, bool bringToFront = true)
    {
        if (!TryBeginAnim()) return;

        if (bringToFront)
            transform.SetAsLastSibling();

        Vector2 toPos = hasRestorePos ? restorePos : windowRoot.anchoredPosition;

        windowRoot.anchoredPosition = fromAnchoredPos;
        transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        if (moveScaleRoutine != null) StopCoroutine(moveScaleRoutine);
        moveScaleRoutine = StartCoroutine(CoMoveAndScale(
            fromPos: fromAnchoredPos,
            toPos: toPos,
            fromScale: new Vector3(0.85f, 0.85f, 1f),
            toScale: Vector3.one,
            duration: duration,
            onDone: () =>
            {
                moveScaleRoutine = null;
                EndAnim();
                onDone?.Invoke();
            }
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


        onDone?.Invoke();
    }

    // =========================
    // Buttons
    // =========================
    private void HookButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() =>
            {
                if (!CanAcceptCommand()) return;   // ✅ 추가
                owner?.RequestClose(appId);
            });

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(() =>
            {
                if (!CanAcceptCommand()) return;   // ✅ 추가
                owner?.Minimize(appId);
            });

        if (pinToggleButton != null)
            pinToggleButton.onClick.AddListener(() =>
            {
                if (!CanAcceptCommand()) return;   // ✅ 추가(선택인데 넣는 게 안전)
                isPinned = !isPinned;
                SoundManager.Instance.PlayOS(OSSoundEvent.Pin); // 🔥 여기

            });
    }
}
