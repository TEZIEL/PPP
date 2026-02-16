using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;


public class WindowController : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler , IEndDragHandler
{
    [Header("Identity")]
    [SerializeField] private string appId;

    [Header("Window Parts")]
    [SerializeField] private RectTransform windowRoot;
    [SerializeField] private RectTransform titleBar;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private WindowManager windowManager;

    [Header("Drag Bounds")]
    [SerializeField] private float dragOverflow = 250f;     // 드래그 중 허용 바깥
    [SerializeField] private float returnDuration = 0.18f;  // 복귀 애니 시간
    [SerializeField] private float returnSnapEpsilon = 0.5f;
    [SerializeField] private float softSnapDuration = 0.06f; // 아주 짧게
    [SerializeField] private float softSnapMinDistance = 2f; // 2px 이상일 때만 스무딩


    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button minimizeButton;
    [SerializeField] private Button pinToggleButton;


    [Header("Skin")]
    [SerializeField] private UISkin skin;
    [SerializeField] private Image titleBarImage;
    [SerializeField] private Image frameImage;

    private WindowManager owner;
    private RectTransform canvasRect;
    private bool isPinned;
    public bool IsPinned => isPinned;

    public string AppId => appId;
    public bool IsMinimized { get; private set; }
    public RectTransform WindowRoot => windowRoot;

    private Vector2 dragOffset;              // 창 위치 - 마우스(local) 위치
    private Coroutine returnRoutine;
    
    private bool TryGetPointerLocal(PointerEventData eventData, out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
    }


    public void SetWindowPosition(Vector2 anchoredPosition)
    {
        if (windowRoot == null)
            return;

        windowRoot.anchoredPosition = anchoredPosition;
    }

    public void InjectManager(WindowManager manager)
    {
        windowManager = manager;
    }


    public void TogglePinFromShortcut()
    {
        isPinned = !isPinned;

        // 버튼 색/아이콘 바꾸고 싶으면 여기
    }

    public string GetAppId() => appId;

    public RectTransform GetWindowRoot() => windowRoot;



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

    public void SetMinimized(bool minimized)
    {
        IsMinimized = minimized;

        canvasGroup.alpha = minimized ? 0f : 1f;
        canvasGroup.interactable = !minimized;
        canvasGroup.blocksRaycasts = !minimized;
    }

    public void OnPointerDown(PointerEventData e)
    {
        owner?.Focus(appId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 핀 상태면 드래그 시작 자체를 막음
        
        if (canvasRect == null || windowRoot == null) return;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        if (!TryGetPointerLocal(eventData, out var pointerLocal)) return;

        // 창의 현재 anchoredPosition은 canvasRect 로컬 좌표와 같은 체계라고 가정 (보통 동일)
        dragOffset = windowRoot.anchoredPosition - pointerLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 혹시 BeginDrag가 뚫려도 여기서 막음
        if (canvasRect == null || windowRoot == null) return;
        if (!TryGetPointerLocal(eventData, out var pointerLocal)) return;

        Vector2 target = pointerLocal + dragOffset;

        // 드래그 중에는 overflow 허용
        target = ClampWindowAnchoredPosition(target, allowOverflow: dragOverflow);

        windowRoot.anchoredPosition = target;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPinned) return; // ✅ 핀 상태에서 EndDrag 처리로 위치 튐/세이브 방지
        if (canvasRect == null || windowRoot == null) return;

        // 놓았을 때도 250px까지는 유지
        Vector2 target = ClampWindowAnchoredPosition(windowRoot.anchoredPosition, allowOverflow: dragOverflow);

        float dist = (target - windowRoot.anchoredPosition).magnitude;

        // ✅ 허용 범위 안이면: 원래는 즉시 return이었는데,
        // 2px 이상 차이가 있을 때만 "아주 짧게" 스르륵 정리
        if (dist < returnSnapEpsilon)
        {
            windowManager?.RequestAutoSave();
            return;
        }

        if (returnRoutine != null) StopCoroutine(returnRoutine);

        if (dist <= softSnapMinDistance)
        {
            // 미세한 보정만: 매우 짧은 스무딩
            returnRoutine = StartCoroutine(ReturnToBounds(target, softSnapDuration));
        }
        else
        {
            // 250 경계를 넘겼거나, 큰 보정이 필요할 때: 기존 복귀 속도
            returnRoutine = StartCoroutine(ReturnToBounds(target, returnDuration));
        }
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

        windowManager?.RequestAutoSave();
    }


    private Vector2 ClampWindowAnchoredPosition(Vector2 desired, float allowOverflow)
    {

        if (windowRoot == null || windowRoot.parent == null)
            return desired;

        // canvasRect 기준 영역(로컬 좌표)
        Rect c = ((RectTransform)windowRoot.parent).rect;

        // 창의 크기/피벗
        Vector2 size = windowRoot.rect.size;
        Vector2 pivot = windowRoot.pivot;

        // 창의 각 변이 desired anchoredPosition에서 어디까지 뻗는지 계산
        float left = desired.x - size.x * pivot.x;
        float right = desired.x + size.x * (1f - pivot.x);
        float bottom = desired.y - size.y * pivot.y;
        float top = desired.y + size.y * (1f - pivot.y);

        // canvasRect 안쪽 경계(overflow 허용)
        float minX = c.xMin - allowOverflow;
        float maxX = c.xMax + allowOverflow;
        float minY = c.yMin - allowOverflow;
        float maxY = c.yMax + allowOverflow;

        // desired를 이동시켜 창이 허용 경계 안에 들어오게 만든다
        float dx = 0f;
        if (left < minX) dx = minX - left;
        else if (right > maxX) dx = maxX - right;

        float dy = 0f;
        if (bottom < minY) dy = minY - bottom;
        else if (top > maxY) dy = maxY - top;

        return new Vector2(desired.x + dx, desired.y + dy);
    }


    public void SetActiveVisual(bool active)
    {
        if (skin == null) return;

        if (titleBarImage != null)
            titleBarImage.color =
                active ? skin.titleActiveColor : skin.titleInactiveColor;

        if (frameImage != null)
            frameImage.color =
                active ? skin.frameActiveColor : skin.frameInactiveColor;
    }

    private void HookButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => owner?.Close(appId));

        if (minimizeButton != null)
            minimizeButton.onClick.AddListener(() => owner?.Minimize(appId));

        if (pinToggleButton != null)
            pinToggleButton.onClick.AddListener(() => isPinned = !isPinned);
    }

    public void SetWindowSize(Vector2 size)
    {
        windowRoot.sizeDelta = size;
    }
}
