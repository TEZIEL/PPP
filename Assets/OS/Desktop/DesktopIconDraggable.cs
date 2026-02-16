using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopIconDraggable : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string iconId;           // 저장 키 (appId랑 동일하게 써도 됨)
    [SerializeField] private RectTransform rect;      // 없으면 자동 할당

    private RectTransform canvasRect;                 // WindowManager가 주입
    private Vector2 dragOffset;
    private WindowManager windowManager;             // 저장 트리거용

    public string GetId() => iconId;
    public RectTransform GetRect() => rect;

    public void Initialize(WindowManager wm, RectTransform rootCanvas)
    {
        windowManager = wm;
        canvasRect = rootCanvas;
        if (rect == null) rect = (RectTransform)transform;
    }

    private bool TryPointerLocal(PointerEventData e, out Vector2 local)
    {
        if (canvasRect == null)
        {
            local = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out local);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;
        dragOffset = rect.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryPointerLocal(eventData, out var p)) return;
        rect.anchoredPosition = p + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 시 자동 저장 트리거
        windowManager?.RequestAutoSave();
    }
}
