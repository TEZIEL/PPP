using UnityEngine;
using UnityEngine.EventSystems;

public class ConsumeDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        eventData.Use();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        eventData.Use();
    }
}