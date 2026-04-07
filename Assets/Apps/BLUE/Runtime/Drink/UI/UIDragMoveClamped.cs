using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    public sealed class UIDragMoveClamped : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform parentArea;
        [SerializeField] private Button pinToggleButton;
        [SerializeField] private string pinStateVarKey;
        [Header("Z-Order")]
        [SerializeField] private bool bringToFrontOnPointerDown = true;
        [SerializeField] private bool bringToFrontOnChildPointerDown = true;
        [SerializeField] private bool applyInitialSiblingIndexOnEnable;
        [SerializeField] private int initialSiblingIndex = -1;
        [SerializeField] private RectTransform[] blockDragAreas;


        private RectTransform rect;
        private RectTransform dragParent;
        private Vector2 startMouseLocal;
        private Vector2 startAnchoredPosition;
        private VNRunner runner;
        private bool isPinned;

        public bool IsPinned => isPinned;
        public event Action<UIDragMoveClamped, bool> PinnedStateChanged;
        private static readonly List<RaycastResult> RaycastResultsBuffer = new List<RaycastResult>(16);

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            dragParent = rect != null ? rect.parent as RectTransform : null;
            runner = GetComponentInParent<VNRunner>(true);

            if (pinToggleButton != null)
                pinToggleButton.onClick.AddListener(TogglePinned);

            if (runner != null && !string.IsNullOrEmpty(pinStateVarKey))
                isPinned = runner.GetVar(pinStateVarKey, 0) == 1;

            NotifyPinnedStateChanged();
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

        private void Update()
        {
            if (!bringToFrontOnChildPointerDown || rect == null)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            TryBringToFrontFromPointerPosition(Input.mousePosition);
        }

        public void SetParentArea(RectTransform area)
        {
            parentArea = area;
        }


        private bool IsPointerOverBlockedArea(PointerEventData eventData)
        {
            if (blockDragAreas == null || blockDragAreas.Length == 0)
                return false;

            foreach (var rect in blockDragAreas)
            {
                if (rect == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(
                    rect,
                    eventData.position,
                    eventData.pressEventCamera))
                {
                    return true;
                }
            }

            return false;
        }


        public void ConfigureZOrder(bool bringToFront, bool applyInitialIndex, int siblingIndex)
        {
            bringToFrontOnPointerDown = bringToFront;
            bringToFrontOnChildPointerDown = bringToFront;
            applyInitialSiblingIndexOnEnable = applyInitialIndex;
            initialSiblingIndex = siblingIndex;
            ApplyInitialSiblingIndex();
        }

        public void TogglePinned()
        {
            SoundManager.Instance.PlayOS(OSSoundEvent.Pin); // 🔥 추가

            SetPinned(!isPinned);
        }

        public void SetPinned(bool pinned)
        {
            if (isPinned == pinned)
                return;

            isPinned = pinned;
            SavePinnedState();
            NotifyPinnedStateChanged();
        }

        public bool TryGetWindowState(string windowId, out VNWindowStateData state)
        {
            state = null;
            if (rect == null || string.IsNullOrWhiteSpace(windowId))
                return false;

            state = new VNWindowStateData
            {
                windowId = windowId,
                anchoredX = rect.anchoredPosition.x,
                anchoredY = rect.anchoredPosition.y,
                isPinned = isPinned,
                siblingIndex = rect.GetSiblingIndex()
            };
            return true;
        }

        public void ApplyWindowState(VNWindowStateData state)
        {
            if (state == null || rect == null)
                return;

            SetPinned(state.isPinned);

            Vector2 next = new Vector2(state.anchoredX, state.anchoredY);
            if (dragParent != null && parentArea != null)
                next = ClampAnchoredPositionToParentArea(next);

            rect.anchoredPosition = next;
            if (rect.parent != null)
            {
                int clampedIndex = Mathf.Clamp(state.siblingIndex, 0, rect.parent.childCount - 1);
                rect.SetSiblingIndex(clampedIndex);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isPinned)
                return;

            if (!CanDrag(eventData))
                return;

            if (IsPointerOnButton(eventData))
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

            if (IsPointerOnButton(eventData))
                return;

            if (IsPointerOverBlockedArea(eventData))
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

        private void NotifyPinnedStateChanged()
        {
            PinnedStateChanged?.Invoke(this, isPinned);
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

        private static bool IsPointerOnButton(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            GameObject target = eventData.pointerPressRaycast.gameObject;
            if (target == null)
                target = eventData.pointerCurrentRaycast.gameObject;
            if (target == null)
                return false;

            return target.GetComponentInParent<Button>() != null;
        }

        private void BringToFront()
        {
            if (!bringToFrontOnPointerDown || rect == null)
                return;

            rect.SetAsLastSibling();
        }

        private void TryBringToFrontFromPointerPosition(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return;

            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            RaycastResultsBuffer.Clear();
            EventSystem.current.RaycastAll(pointerEventData, RaycastResultsBuffer);

            for (int i = 0; i < RaycastResultsBuffer.Count; i++)
            {
                var hit = RaycastResultsBuffer[i];
                if (hit.gameObject == null)
                    continue;

                Transform hitTransform = hit.gameObject.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    BringToFront();
                    return;
                }
            }
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
