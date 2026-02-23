using UnityEngine;
using UnityEngine.EventSystems;

public class FidgetRightClickDislike : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private FidgetShortformController controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            controller.Dislike();
        else if (eventData.button == PointerEventData.InputButton.Left)
            controller.Like(); // 원하면 삭제 (왼클릭 싫어요 막고 싶으면)
    }
}