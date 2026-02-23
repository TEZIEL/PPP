using UnityEngine;
using UnityEngine.EventSystems;

public class DislikeRightClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private FidgetShortsController controller; // 네 컨트롤러 타입명으로 맞춰
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        controller.AddDislike(); // 컨트롤러에 있는 함수명으로 맞춰
    }
}