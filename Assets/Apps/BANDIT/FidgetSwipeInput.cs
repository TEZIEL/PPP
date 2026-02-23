using UnityEngine;
using UnityEngine.EventSystems;

public class FidgetSwipeInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private FidgetShortsController controller;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null) return;
        controller.BeginDrag(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller == null) return;
        controller.Drag(eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (controller == null) return;
        controller.EndDrag();
    }
}