using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class UIDragMoveClamped : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private RectTransform parentArea;
        [SerializeField] private Button pinToggleButton;
        [SerializeField] private string pinStateVarKey;

        private RectTransform rect;
        private RectTransform dragParent;
        private Vector2 startMouseLocal;
        private Vector2 startAnchoredPosition;
        private VNRunner runner;
        private bool isPinned;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            dragParent = rect != null ? rect.parent as RectTransform : null;
            runner = GetComponentInParent<VNRunner>(true);

            if (pinToggleButton != null)
                pinToggleButton.onClick.AddListener(TogglePinned);

            if (runner != null && !string.IsNullOrEmpty(pinStateVarKey))
                isPinned = runner.GetVar(pinStateVarKey, 0) == 1;
        }

        private void OnDestroy()
        {
            if (pinToggleButton != null)
                pinToggleButton.onClick.RemoveListener(TogglePinned);
        }

        public void SetParentArea(RectTransform area)
        {
            parentArea = area;
        }

        public void TogglePinned()
        {
            SetPinned(!isPinned);
        }

        public void SetPinned(bool pinned)
        {
            isPinned = pinned;
            SavePinnedState();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isPinned)
                return;

            if (!CanDrag(eventData))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragParent,
                eventData.position,
                eventData.pressEventCamera,
                out startMouseLocal);

            startAnchoredPosition = rect.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isPinned)
                return;

            if (!CanDrag(eventData))
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragParent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var currentMouseLocal))
                return;

            Vector2 delta = currentMouseLocal - startMouseLocal;
            Vector2 newAnchoredPosition = startAnchoredPosition + delta;
            rect.anchoredPosition = ClampAnchoredPositionToParentArea(newAnchoredPosition);
        }

        private void SavePinnedState()
        {
            if (runner == null || string.IsNullOrEmpty(pinStateVarKey))
                return;

            runner.SetVar(pinStateVarKey, isPinned ? 1 : 0);
        }

        private bool CanDrag(PointerEventData eventData)
        {
            return eventData != null && rect != null && dragParent != null && parentArea != null;
        }

        private Vector2 ClampAnchoredPositionToParentArea(Vector2 anchoredPosition)
        {
            Vector3[] areaCorners = new Vector3[4];
            Vector3[] panelCorners = new Vector3[4];
            parentArea.GetWorldCorners(areaCorners);
            rect.GetWorldCorners(panelCorners);

            Vector2 toLeftBottom = dragParent.InverseTransformVector(panelCorners[0] - rect.position);
            Vector2 toRightTop = dragParent.InverseTransformVector(panelCorners[2] - rect.position);
            Vector2 areaLeftBottom = dragParent.InverseTransformPoint(areaCorners[0]);
            Vector2 areaRightTop = dragParent.InverseTransformPoint(areaCorners[2]);

            float minX = areaLeftBottom.x - toLeftBottom.x;
            float maxX = areaRightTop.x - toRightTop.x;
            float minY = areaLeftBottom.y - toLeftBottom.y;
            float maxY = areaRightTop.y - toRightTop.y;

            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);

            return anchoredPosition;
        }
    }
}
