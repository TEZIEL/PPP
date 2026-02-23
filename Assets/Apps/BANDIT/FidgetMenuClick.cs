using UnityEngine;
using UnityEngine.EventSystems;

public class FidgetMenuClick : MonoBehaviour, IPointerClickHandler
{
    public enum ActionType { Like, Dislike, Reset, Gallery }

    [SerializeField] private FidgetShortsController controller;
    [SerializeField] private ActionType action;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null) return;

        switch (action)
        {
            case ActionType.Like:
                // 좋아요는 좌클릭만
                if (eventData.button == PointerEventData.InputButton.Left)
                    controller.AddLike();
                break;

            case ActionType.Dislike:
                // 싫어요는 우클릭만
                if (eventData.button == PointerEventData.InputButton.Right)
                    controller.AddDislike();
                break;

            case ActionType.Reset:
                if (eventData.button == PointerEventData.InputButton.Left)
                    controller.ResetCounts();
                break;

            case ActionType.Gallery:
                if (eventData.button == PointerEventData.InputButton.Left)
                    controller.OpenGallery();
                break;
        }
    }
}