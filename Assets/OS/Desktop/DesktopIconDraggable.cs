using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopIconDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string iconId;      // 저장 키
    [SerializeField] private RectTransform rect; // 비어있으면 자동 할당

    private RectTransform desktopRect;           // iconsRoot(DesktopIconBG) 주입
    private Vector2 dragOffset;
    private WindowManager windowManager;
    private DesktopGridManager gridManager;

    public bool IsDragging { get; private set; }
    public float LastDragEndTime { get; private set; } = -999f;

    public string GetId() => iconId;
    public RectTransform GetRect() => rect;

    private void Awake()
    {
        if (rect == null)
            rect = (RectTransform)transform;
    }

    private void OnDisable()
    {
        // 드래그 중 비활성화/씬 전환 대비
        IsDragging = false;
    }

    public void Initialize(WindowManager wm, RectTransform desktopRoot, DesktopGridManager grid)
    {
        windowManager = wm;
        desktopRect = desktopRoot;
        gridManager = grid;

        if (rect == null)
            rect = (RectTransform)transform;
    }

    private bool TryPointerLocal(PointerEventData e, out Vector2 local)
    {
        if (desktopRect == null)
        {
            local = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            desktopRect, e.position, e.pressEventCamera, out local);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;

        IsDragging = true;
        dragOffset = rect.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;

        Vector2 target = p + dragOffset;

        Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
        rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(target, rect, allowed);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // ✅ 무조건 드래그 상태 종료 + 시간 갱신
        IsDragging = false;
        LastDragEndTime = Time.unscaledTime;

        // ✅ 놓을 때도 한번 더 clamp (desktopRect 기준 통일)
        if (desktopRect != null)
        {
            Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
            rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(rect.anchoredPosition, rect, allowed);
        }

        // ✅ Grid 모드면 슬롯 스냅
        if (gridManager != null && gridManager.LayoutMode == DesktopGridManager.DesktopLayoutMode.Grid)
        {
            Vector2 snapped = gridManager.GetNearestSlotPosition(rect.anchoredPosition);

            Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);
            rect.anchoredPosition = DesktopBounds.ClampAnchoredPosition(snapped, rect, allowed);
        }

        // ✅ autosave는 딱 1번
        windowManager?.RequestAutoSave();
    }
}

