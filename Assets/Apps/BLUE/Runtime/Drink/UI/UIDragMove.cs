using UnityEngine;
using UnityEngine.EventSystems;

namespace PPP.BLUE.VN
{
    public sealed class UIDragMove : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform rect;
        private Vector2 offset;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                eventData.position,
                eventData.pressEventCamera,
                out offset
            );
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            RectTransform parentRect = rect.parent as RectTransform;
            if (parentRect == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                rect.localPosition = localPoint - offset;
            }
        }
    }
}
