using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class UIDragMoveClamped : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform parentArea;
        [SerializeField] private Button pinToggleButton;
        [SerializeField] private string pinStateVarKey;
        [Header("Z-Order")]
        [SerializeField] private bool bringToFrontOnPointerDown = true;
        [SerializeField] private bool applyInitialSiblingIndexOnEnable;
        [SerializeField] private int initialSiblingIndex = -1;

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

        private void OnEnable()
        {
            ApplyInitialSiblingIndex();
        }

        public void SetParentArea(RectTransform area)
        {
            parentArea = area;
        }

        public void ConfigureZOrder(bool bringToFront, bool applyInitialIndex, int siblingIndex)
        {
            bringToFrontOnPointerDown = bringToFront;
            applyInitialSiblingIndexOnEnable = applyInitialIndex;
            initialSiblingIndex = siblingIndex;
            ApplyInitialSiblingIndex();
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

            BringToFront();

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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag(eventData))
                return;

            BringToFront();
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

        private void BringToFront()
        {
            if (!bringToFrontOnPointerDown || rect == null)
                return;

            rect.SetAsLastSibling();
        }

        private void ApplyInitialSiblingIndex()
        {
            if (!applyInitialSiblingIndexOnEnable || rect == null || rect.parent == null || initialSiblingIndex < 0)
                return;

            int clampedIndex = Mathf.Clamp(initialSiblingIndex, 0, rect.parent.childCount - 1);
            rect.SetSiblingIndex(clampedIndex);
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
