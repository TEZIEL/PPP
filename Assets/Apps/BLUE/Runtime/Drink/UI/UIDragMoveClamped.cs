using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class UIDragMoveClamped : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        [Serializable]
        private struct PinVisualBinding
        {
            public string label;
            public Button button;
            public Image targetImage;
            public UIDragMoveClamped stateSource;
            public Sprite activeSprite;
            public Sprite inactiveSprite;
        }

        [SerializeField] private RectTransform parentArea;
        [SerializeField] private Button pinToggleButton;
        [SerializeField] private string pinStateVarKey;
        [Header("Pin Visual Bindings (VN)")]
        [SerializeField] private PinVisualBinding[] pinVisualBindings = Array.Empty<PinVisualBinding>();
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
        public event Action<bool> OnPinnedStateChanged;

        private static readonly List<RaycastResult> RaycastResultsBuffer = new List<RaycastResult>(16);
        private readonly HashSet<UIDragMoveClamped> subscribedPinSources = new HashSet<UIDragMoveClamped>();

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            dragParent = rect != null ? rect.parent as RectTransform : null;
            runner = GetComponentInParent<VNRunner>(true);

            if (pinToggleButton != null)
                pinToggleButton.onClick.AddListener(TogglePinned);

            if (runner != null && !string.IsNullOrEmpty(pinStateVarKey))
                isPinned = runner.GetVar(pinStateVarKey, 0) == 1;

            SubscribePinVisualSources();
            ApplyCurrentTheme();
            RefreshPinVisualBindings();
        }

        private void OnEnable()
        {
            ApplyInitialSiblingIndex();

            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged += HandleThemeChanged;

            SubscribePinVisualSources();
            ApplyCurrentTheme();
            RefreshPinVisualBindings();
        }

        private void OnDisable()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged -= HandleThemeChanged;

            UnsubscribePinVisualSources();
        }

        private void OnDestroy()
        {
            if (pinToggleButton != null)
                pinToggleButton.onClick.RemoveListener(TogglePinned);

            UnsubscribePinVisualSources();
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

            foreach (var blockedRect in blockDragAreas)
            {
                if (blockedRect == null)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(
                        blockedRect,
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
            SoundManager.Instance.PlayOS(OSSoundEvent.Pin);
            SetPinned(!isPinned);
        }

        public void SetPinned(bool pinned)
        {
            if (isPinned == pinned)
                return;

            isPinned = pinned;
            SavePinnedState();
            OnPinnedStateChanged?.Invoke(isPinned);
            RefreshPinVisualBindings();
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

        private void HandleThemeChanged()
        {
            ApplyCurrentTheme();
            RefreshPinVisualBindings();
        }

        private void ApplyCurrentTheme()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager == null || themeManager.CurrentTheme == null || pinVisualBindings == null)
                return;

            var theme = themeManager.CurrentTheme.vn;
            for (int i = 0; i < pinVisualBindings.Length; i++)
            {
                var binding = pinVisualBindings[i];
                if (theme.vnPinActiveSprite != null)
                    binding.activeSprite = theme.vnPinActiveSprite;
                if (theme.vnPinInactiveSprite != null)
                    binding.inactiveSprite = theme.vnPinInactiveSprite;
                pinVisualBindings[i] = binding;
            }
        }

        private void RefreshPinVisualBindings()
        {
            if (pinVisualBindings == null || pinVisualBindings.Length == 0)
                return;

            for (int i = 0; i < pinVisualBindings.Length; i++)
            {
                var binding = pinVisualBindings[i];
                var image = ResolveTargetImage(binding);
                if (image == null)
                    continue;

                bool active = ResolvePinActiveState(binding);
                Sprite next = active ? binding.activeSprite : binding.inactiveSprite;
                if (next == null || image.sprite == next)
                    continue;

                image.sprite = next;
            }
        }

        private static Image ResolveTargetImage(PinVisualBinding binding)
        {
            if (binding.targetImage != null)
                return binding.targetImage;

            return binding.button != null ? binding.button.image : null;
        }

        private bool ResolvePinActiveState(PinVisualBinding binding)
        {
            var source = binding.stateSource != null ? binding.stateSource : this;
            return source != null && source.IsPinned;
        }

        private void SubscribePinVisualSources()
        {
            if (pinVisualBindings == null || pinVisualBindings.Length == 0)
                return;

            for (int i = 0; i < pinVisualBindings.Length; i++)
            {
                var source = pinVisualBindings[i].stateSource != null ? pinVisualBindings[i].stateSource : this;
                if (source == null || subscribedPinSources.Contains(source))
                    continue;

                source.OnPinnedStateChanged += HandleAnyPinStateChanged;
                subscribedPinSources.Add(source);
            }
        }

        private void UnsubscribePinVisualSources()
        {
            if (subscribedPinSources.Count == 0)
                return;

            foreach (var source in subscribedPinSources)
            {
                if (source != null)
                    source.OnPinnedStateChanged -= HandleAnyPinStateChanged;
            }

            subscribedPinSources.Clear();
        }

        private void HandleAnyPinStateChanged(bool _)
        {
            RefreshPinVisualBindings();
        }
    }
}
