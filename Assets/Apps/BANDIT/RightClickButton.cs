using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RightClickButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private UnityEvent onRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        onRightClick?.Invoke();
    }
}