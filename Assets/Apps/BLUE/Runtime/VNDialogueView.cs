using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNDialogueView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNTextTyper typer;
        [SerializeField] private VNPolicyController policy;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private VNOSBridge osBridge;
        [SerializeField] private VNOSBridge bridge;
        [SerializeField] private RectTransform advanceClickArea;
        [SerializeField] private RectTransform buttonContainerRoot;
        [SerializeField] private GraphicRaycaster graphicRaycaster;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private VNClosePopupController closePopupController;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button autoPlayButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button saveLoadButton;
        [SerializeField] private Button hideUIButton;
        [SerializeField] private VNSaveLoadWindow saveLoadWindow;
        [SerializeField] private VNBacklogView backlogView;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private RectTransform dialogueRoot;
        [SerializeField] private GameObject minimizedUIRoot;
        [SerializeField] private UIDragMoveClamped dialogueWindowDragStateSource;
        [SerializeField] private UIDragMoveClamped minimizedDialogueDragStateSource;
        private VNBacklogKey lastHandledKey;
        private VNBacklogKey currentLineBacklogKey;
        private string lastHandledLineId;
       

        [Header("Hide/Show Animation (Window Style)")]
        [SerializeField] private float dialogueShowDuration = 0.12f;
        [SerializeField] private float dialogueHideDuration = 0.10f;
        [SerializeField] private float minimizedShowDuration = 0.12f;
        [SerializeField] private float minimizedHideDuration = 0.10f;
        [SerializeField] private Vector3 animFromScale = new Vector3(0.92f, 0.92f, 1f);
        [Header("Button Image States")]
        [SerializeField] private ButtonVisualBinding[] buttonVisualBindings = System.Array.Empty<ButtonVisualBinding>();
        // Legacy compatibility: kept hidden so partial merges referencing old fields still compile.
        [SerializeField, HideInInspector] private bool autoPlayEnabled;
        [SerializeField, Min(0f)] private float closeActionLockSeconds = 0.15f;

        [Header("Typing")]
        [SerializeField] private float charsPerSecond = 40f;

        // 현재 라인의 풀텍스트 (SkipToEnd용)
        private string currentFullText = "";
        private bool lineCompleted = true; // true면 Next로 "다음 라인" 가능
        private bool lineDisplayed;
        private int currentLineIndex = -1;
        private bool inputLocked = true;
        private int inputLockFrames = 0;
        private bool subscribed;
        private bool isUIDisappearing = false;
        private bool isUIHidden = false;
        private bool isUIAnimating = false;
        private bool? lastSkipButtonInteractable;
        private bool? lastAutoButtonInteractable;
        private bool? lastExitButtonInteractable;
        private bool? lastSaveLoadButtonInteractable;
        private bool? lastHideUIButtonInteractable;
        private bool skipHoldBindingApplied;
        private readonly HashSet<Button> interactableVisualBindingEventBoundButtons = new();
        private readonly Dictionary<Button, bool> interactableVisualPressedStates = new();
        private float controlActionLockedUntil;
        private Coroutine waitAndRefreshCoroutine;
        private VNBacklogView backlogStateObservedView;
        private static readonly HashSet<VNDialogueView> activeDialogueViews = new();
        public const string DialogueWindowId = "vn_dialogue";
        public const string HiddenDialogueWindowId = "vn_dialogue_hidden";
        public static bool IsAnyBacklogOpen
        {
            get
            {
                foreach (var view in activeDialogueViews)
                {
                    if (view != null && view.IsBacklogOpen)
                        return true;
                }

                return false;
            }
        }
        public bool IsBacklogOpen => backlogView != null && backlogView.IsOpen;

        private enum ButtonVisualMode
        {
            Interactable = 0,
            ToggleAutoPlay = 1,
            HoldSkip = 2,
            InteractableEnabled = 3
        }

        [System.Serializable]
        private struct ButtonVisualBinding
        {
            public string label;
            public Button button;
            public Image targetImage;
            public Sprite activeSprite;
            public Sprite inactiveSprite;
            public ButtonVisualMode visualMode;
        }

        private void ApplyCurrentTheme()
        {
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager == null || themeManager.CurrentTheme == null || buttonVisualBindings == null)
                return;

            var visualTheme = themeManager.CurrentTheme.vn.dialogueButtonVisual;
            for (int i = 0; i < buttonVisualBindings.Length; i++)
            {
                var binding = buttonVisualBindings[i];
                ApplyBindingSpriteFromTheme(ref binding, visualTheme);
                buttonVisualBindings[i] = binding;
            }

            RefreshButtonVisualStates();
        }

        private static void ApplyBindingSpriteFromTheme(ref ButtonVisualBinding binding, AppUIThemeData.VNDialogueButtonVisualTheme visualTheme)
        {
            switch (binding.visualMode)
            {
                case ButtonVisualMode.ToggleAutoPlay:
                    if (visualTheme.toggleAutoPlayActiveSprite != null)
                        binding.activeSprite = visualTheme.toggleAutoPlayActiveSprite;
                    if (visualTheme.toggleAutoPlayInactiveSprite != null)
                        binding.inactiveSprite = visualTheme.toggleAutoPlayInactiveSprite;
                    break;
                case ButtonVisualMode.HoldSkip:
                    if (visualTheme.holdSkipActiveSprite != null)
                        binding.activeSprite = visualTheme.holdSkipActiveSprite;
                    if (visualTheme.holdSkipInactiveSprite != null)
                        binding.inactiveSprite = visualTheme.holdSkipInactiveSprite;
                    break;
                case ButtonVisualMode.InteractableEnabled:
                    if (visualTheme.interactableEnabledActiveSprite != null)
                        binding.activeSprite = visualTheme.interactableEnabledActiveSprite;
                    if (visualTheme.interactableEnabledInactiveSprite != null)
                        binding.inactiveSprite = visualTheme.interactableEnabledInactiveSprite;
                    break;
                default:
                    if (visualTheme.interactableActiveSprite != null)
                        binding.activeSprite = visualTheme.interactableActiveSprite;
                    if (visualTheme.interactableInactiveSprite != null)
                        binding.inactiveSprite = visualTheme.interactableInactiveSprite;
                    break;
            }
        }

        private void HandleThemeChanged()
        {
            ApplyCurrentTheme();
        }

        private readonly struct ButtonVisualState
        {
            public readonly bool AutoEnabled;
            public readonly bool HoldSkipEnabled;

            public ButtonVisualState(bool autoEnabled, bool holdSkipEnabled)
            {
                AutoEnabled = autoEnabled;
                HoldSkipEnabled = holdSkipEnabled;
            }
        }

        private void Start()
        {
            LockInputFrames(2);
          
        }

        private bool CanAcceptVNInput()
        {
            if (policy != null)
                return VNInputGate.CanRouteInput(policy);

            // policy 미주입 시 보수적 폴백
            if (bridge != null)
            {
                var st = bridge.GetWindowState();
                if (!st.IsFocused) return false;
                if (st.IsMinimized) return false;
            }

            return true;
        }

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (closePopupController == null) closePopupController = GetComponentInParent<VNClosePopupController>(true);
            if (closePopupController == null) closePopupController = GetComponentInChildren<VNClosePopupController>(true);
            if (typer != null) typer.SetTarget(dialogueText);
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            if (backlogView == null) backlogView = GetComponentInChildren<VNBacklogView>(true);
            EnsureBacklogBinding("Awake");
            if (advanceClickArea == null && dialogueText != null)
                advanceClickArea = dialogueText.rectTransform;
            if (graphicRaycaster == null)
                graphicRaycaster = GetComponentInParent<GraphicRaycaster>(true);
            ResolveDialogueUIRefs();
            ResolveMinimizedUIRefs();
            SetMinimizedUIVisible(false);
            AutoBindSaveLoadButton();
            AutoBindHideUIButton();
            SetupSkipHoldBinding();
            SetupInteractableVisualBindingEvents();
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")}");
        }

        private void ResolveDialogueUIRefs()
        {
            if (dialogueRoot == null)
            {
                var selfRect = transform as RectTransform;
                var namedDialogRoot = transform.Find("DialogRoot") as RectTransform;
                dialogueRoot = namedDialogRoot != null ? namedDialogRoot : selfRect;
            }

            if (dialogueRoot == null && dialogueText != null)
            {
                var walker = dialogueText.rectTransform;
                while (walker != null)
                {
                    if (walker.name.IndexOf("dialogroot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dialogueRoot = walker;
                        break;
                    }

                    walker = walker.parent as RectTransform;
                }
            }

            if (dialogueCanvasGroup == null && dialogueRoot != null)
                dialogueCanvasGroup = dialogueRoot.GetComponent<CanvasGroup>();

            if (dialogueCanvasGroup == null)
                dialogueCanvasGroup = GetComponentInParent<CanvasGroup>(true);

            if (dialogueCanvasGroup == null && dialogueRoot != null)
                dialogueCanvasGroup = dialogueRoot.gameObject.AddComponent<CanvasGroup>();
        }

        private void ResolveMinimizedUIRefs()
        {
            if (minimizedUIRoot != null)
                return;

            var root = transform.root;
            if (root == null)
                return;

            var candidates = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null)
                    continue;

                string lower = candidate.name.ToLowerInvariant();
                bool isMinimizedName = lower.Contains("mini") || lower.Contains("minimized");
                bool isShowUIName = lower.Contains("showui") || lower.Contains("restoreui") || lower.Contains("ui_show");
                if (!isMinimizedName && !isShowUIName)
                    continue;

                if (dialogueRoot != null && (candidate == dialogueRoot || candidate.IsChildOf(dialogueRoot)))
                    continue;

                minimizedUIRoot = candidate.gameObject;
                break;
            }
        }

        private void SetMinimizedUIVisible(bool visible)
        {
            if (minimizedUIRoot == null)
                return;

            minimizedUIRoot.SetActive(visible);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
                return null;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
                group = target.AddComponent<CanvasGroup>();
            return group;
        }

        private static IEnumerator CoAnimateScaleAlpha(
            RectTransform targetRoot,
            CanvasGroup targetGroup,
            Vector3 fromScale,
            Vector3 toScale,
            float fromAlpha,
            float toAlpha,
            float duration)
        {
            if (targetRoot == null || targetGroup == null)
                yield break;

            float t = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            targetRoot.localScale = fromScale;
            targetGroup.alpha = fromAlpha;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / safeDuration;
                float s = Mathf.SmoothStep(0f, 1f, t);

                targetRoot.localScale = Vector3.LerpUnclamped(fromScale, toScale, s);
                targetGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, s);
                yield return null;
            }

            targetRoot.localScale = toScale;
            targetGroup.alpha = toAlpha;
        }

        private void AutoBindSaveLoadButton()
        {
            if (saveLoadButton != null)
                return;

            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                    continue;

                var onClick = button.onClick;
                int count = onClick.GetPersistentEventCount();
                for (int j = 0; j < count; j++)
                {
                    if (onClick.GetPersistentTarget(j) != (Object)this)
                        continue;

                    if (onClick.GetPersistentMethodName(j) != nameof(OpenSaveLoadWindow))
                        continue;

                    saveLoadButton = button;
                    return;
                }
            }
        }

        private void AutoBindHideUIButton()
        {
            if (hideUIButton != null)
                return;

            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                    continue;

                var onClick = button.onClick;
                int count = onClick.GetPersistentEventCount();
                for (int j = 0; j < count; j++)
                {
                    if (onClick.GetPersistentTarget(j) != (Object)this)
                        continue;

                    if (onClick.GetPersistentMethodName(j) != nameof(HideUI))
                        continue;

                    hideUIButton = button;
                    return;
                }
            }
        }

        private void SetupSkipHoldBinding()
        {
            if (skipHoldBindingApplied || skipButton == null)
                return;

            var trigger = skipButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = skipButton.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(trigger, EventTriggerType.PointerDown, _ => OnSkipButtonPointerDown());
            AddEventTrigger(trigger, EventTriggerType.PointerUp, _ => OnSkipButtonPointerUp());
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => OnSkipButtonPointerUp());
            skipHoldBindingApplied = true;
        }

        private void SetupInteractableVisualBindingEvents()
        {
            if (buttonVisualBindings == null || buttonVisualBindings.Length == 0)
                return;

            for (int i = 0; i < buttonVisualBindings.Length; i++)
            {
                var binding = buttonVisualBindings[i];
                var button = binding.button;
                if (button == null || !NeedsPointerVisualEvents(binding.visualMode))
                    continue;

                if (!interactableVisualPressedStates.ContainsKey(button))
                    interactableVisualPressedStates.Add(button, false);

                if (interactableVisualBindingEventBoundButtons.Contains(button))
                    continue;

                var trigger = button.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = button.gameObject.AddComponent<EventTrigger>();

                AddEventTrigger(trigger, EventTriggerType.PointerDown, _ => OnInteractableVisualPointerDown(button));
                AddEventTrigger(trigger, EventTriggerType.PointerUp, _ => OnInteractableVisualPointerUp(button));
                AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => OnInteractableVisualPointerUp(button));
                interactableVisualBindingEventBoundButtons.Add(button);
            }
        }

        private static bool NeedsPointerVisualEvents(ButtonVisualMode mode)
        {
            return mode == ButtonVisualMode.Interactable;
        }

        private void OnInteractableVisualPointerDown(Button button)
        {
            if (button == null || !interactableVisualPressedStates.ContainsKey(button))
                return;

            interactableVisualPressedStates[button] = true;
            RefreshButtonVisualStates();
        }

        private void OnInteractableVisualPointerUp(Button button)
        {
            if (button == null || !interactableVisualPressedStates.ContainsKey(button))
                return;

            interactableVisualPressedStates[button] = false;
            RefreshButtonVisualStates();
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            if (trigger == null)
                return;

            if (trigger.triggers == null)
                trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

            for (int i = 0; i < trigger.triggers.Count; i++)
            {
                if (trigger.triggers[i].eventID != eventType)
                    continue;

                trigger.triggers[i].callback.AddListener(callback);
                return;
            }

            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private bool IsPointerInsideAdvanceArea()
        {
            if (advanceClickArea == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                advanceClickArea,
                Input.mousePosition,
                uiCamera);
        }

        private bool IsPointerOverBlockedButtonContainer()
        {
            if (buttonContainerRoot == null || graphicRaycaster == null || EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            graphicRaycaster.Raycast(eventData, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                var go = hits[i].gameObject;
                if (go == null) continue;
                if (go.transform == buttonContainerRoot || go.transform.IsChildOf(buttonContainerRoot))
                    return true;
            }

            return false;
        }

        private void OnEnable()
        {
            activeDialogueViews.Add(this);
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged += HandleThemeChanged;

            ApplyCurrentTheme();
            inputLocked = true;
            LockInputFrames(5); // 여기 추가 (2 말고 5 추천)
            Debug.Log($"[VN] DialogueView OnEnable text={(dialogueText != null ? dialogueText.text : "<null>")}");
            if (dialogueText != null && dialogueText.text == "New Text")
                dialogueText.text = string.Empty;

            if (subscribed) return;
            if (runner == null) return;

            runner.OnSay += HandleSay;
            runner.OnEnd += HandleEnd;
            subscribed = true;

            EnsureBacklogBinding("OnEnable");
            StartWaitAndRefresh();
            RefreshButtonVisualStates();
        }

        private void OnDisable()
        {
            activeDialogueViews.Remove(this);
            var themeManager = AppUIThemeManager.Instance;
            if (themeManager != null)
                themeManager.OnThemeChanged -= HandleThemeChanged;

            OnSkipButtonPointerUp();
            if (interactableVisualPressedStates.Count > 0)
            {
                var keys = new List<Button>(interactableVisualPressedStates.Keys);
                for (int i = 0; i < keys.Count; i++)
                    interactableVisualPressedStates[keys[i]] = false;
            }
            inputLocked = true;
            StopWaitAndRefresh();

            if (!subscribed) return;
            if (runner == null) return;

            runner.OnSay -= HandleSay;
            runner.OnEnd -= HandleEnd;
            subscribed = false;

            if (backlogStateObservedView != null)
            {
                backlogStateObservedView.OnOpenStateChanged -= HandleBacklogOpenStateChanged;
                backlogStateObservedView = null;
            }
        }

        private void OnDestroy()
        {
            backlogView?.UnbindManager();
        }

        public void EnsureBacklogBindingFromRunner()
        {
            EnsureBacklogBinding("Runner");
        }

        private void EnsureBacklogBinding(string reason)
        {
            if (runner == null)
                runner = GetComponentInParent<VNRunner>(true);
            if (backlogView == null)
                backlogView = GetComponentInChildren<VNBacklogView>(true);
            if (backlogView == null)
                backlogView = CreateRuntimeBacklogView();

            if (backlogView == null)
            {
                Debug.LogWarning($"[VN_UI] Backlog bind skipped ({reason}): backlogView is null");
                return;
            }

            if (runner == null)
            {
                Debug.LogWarning($"[VN_UI] Backlog bind skipped ({reason}): runner is null");
                backlogView.BindManager(null);
                return;
            }

            Debug.Log($"[VN_UI] Binding backlog view ({reason}) with runner={runner.name}");
            backlogView.ConfigureFallbackTextTemplates(nameText, dialogueText);
            backlogView.BindManager(runner.BacklogManager);
            ObserveBacklogState(backlogView);
        }

        private VNBacklogView CreateRuntimeBacklogView()
        {
            RectTransform parent = transform as RectTransform;
            if (parent == null)
                return null;

            var backlogRootGo = new GameObject("Backlog_Runtime", typeof(RectTransform), typeof(Image));
            var backlogRootRect = backlogRootGo.GetComponent<RectTransform>();
            backlogRootRect.SetParent(parent, false);
            backlogRootRect.anchorMin = new Vector2(0f, 0f);
            backlogRootRect.anchorMax = new Vector2(1f, 1f);
            backlogRootRect.offsetMin = new Vector2(80f, 60f);
            backlogRootRect.offsetMax = new Vector2(-80f, -60f);

            var bg = backlogRootGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(backlogRootRect, false);
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(20f, 20f);
            viewportRect.offsetMax = new Vector2(-20f, -20f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = backlogRootGo.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var view = backlogRootGo.AddComponent<VNBacklogView>();
            backlogRootGo.SetActive(false);
            Debug.LogWarning("[VN_UI] VNBacklogView missing in scene/prefab. Runtime backlog UI fallback was created.");
            return view;
        }

        private void ObserveBacklogState(VNBacklogView view)
        {
            if (view == null)
                return;

            if (backlogStateObservedView == view)
                return;

            if (backlogStateObservedView != null)
            {
                backlogStateObservedView.OnOpenStateChanged -= HandleBacklogOpenStateChanged;
            }

            backlogStateObservedView = view;
            backlogStateObservedView.OnOpenStateChanged += HandleBacklogOpenStateChanged;
        }

        private void HandleBacklogOpenStateChanged(bool isOpenNow)
        {
            if (isOpenNow)
            {
                runner?.SetUiSkipHeld(false, "Backlog Open");
                EventSystem.current?.SetSelectedGameObject(null);
            }

            HandleControlButtonState();
        }

        private bool IsBacklogInputBlocked()
        {
            return IsBacklogOpen;
        }

        private void Update()
        {
            if (IsAnyBacklogOpen)
            {
                if (Input.GetKeyDown(KeyCode.LeftAlt) && IsBacklogOpen)
                    backlogView?.Toggle();
                EventSystem.current?.SetSelectedGameObject(null);
                return;
            }

            if (IsBacklogOpen)
            {
                HandleControlButtonState();
                EventSystem.current?.SetSelectedGameObject(null);
                return;
            }

            HandleControlButtonState();
            if (isUIHidden && !isUIAnimating && Input.GetKeyDown(KeyCode.V))
            {
                ShowUI();
                return;
            }
            if (isUIHidden || isUIAnimating)
                return;

            bool manualInputBlockedByBacklog = IsAnyBacklogOpen || IsBacklogOpen;
            if (manualInputBlockedByBacklog)
                EventSystem.current?.SetSelectedGameObject(null);

            if (runner != null && runner.IsWaiting && runner.CallStackCount > 0)
            {
                return;
            }

            if (policy != null && policy.IsDrinkPanelOpen && autoPlayEnabled)
            {
                autoPlayEnabled = false;
                runner?.SetAutoPlay(false, "Drink Mode Auto Off");
            }

            if (manualInputBlockedByBacklog)
                return;

            if (!CanAcceptVNInput()) return;
            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (runner.JustForceCompletedThisFrame) return;
            if (!runner.HasScript) return;
            if (IsBacklogInputBlocked()) return;
            HandleSkipAutoState();
            if (HandleVNHotkeys()) return;

            // InputGate를 통과한 입력만 대사 진행에 사용
            if (policy != null && !VNInputGate.CanAdvanceDialogue(policy))
                return;

            // ✅ Next 입력
            bool pressedSpace = Input.GetKeyDown(KeyCode.Space);
            bool clicked = Input.GetMouseButtonDown(0);
            if (!pressedSpace && !clicked) return;

            if (clicked)
            {
                if (IsPointerOverBlockedButtonContainer())
                    return;

                // dialog 영역 클릭만 허용
                if (!IsPointerInsideAdvanceArea())
                    return;
            }

            // ✅ 유저 입력이면 무조건 Auto OFF (타이핑완료/Next 둘 다 포함)
            if (inputLocked)
            {
                Debug.Log("[VN] input blocked (not ready)");
                return;
            }

            if (!runner.HasValidNode())
            {
                Debug.Log("[VN] input blocked (no node)");
                return;
            }

            runner.ForceAutoOff(pressedSpace ? "User input (Space)" : "User input (Click)");
            if (runner.TryGetCurrentSayState(out _, out var debugLineIndex, out _, out _))
                currentLineIndex = debugLineIndex;
            Debug.Log($"[VN] input lineIndex={currentLineIndex}, displayed={lineDisplayed}");
            Debug.Log($"[VN] lineIndex={currentLineIndex}, displayed={lineDisplayed}");

            // ✅ 타이핑 중이면 "완성"만 하고 다음 라인으로 넘어가진 않음
            if (!lineCompleted && typer != null && typer.IsTyping)
            {
                ForceCompleteLine();
                return;
            }

            // ✅ 그 외에는 다음 라인
            if (runner != null && runner.JustForceCompletedThisFrame)
                return;

            lineDisplayed = false;
            runner.Next();
            Debug.Log("[VN_UI] Next input detected -> runner.Next()");
        }

        private bool HandleVNHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                if (backlogView != null)
                {
                    backlogView.Toggle();
                    return true;
                }
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                OpenSaveLoadWindow();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                if (isUIHidden)
                    ShowUI();
                else
                    HideUI();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnExitButtonClicked();
                return true;
            }

            return false;
        }

        // Legacy compatibility: older branches may still call this.
        private void HandleSkipAutoState()
        {
            // Intentionally empty. Skip/Auto behavior is runner-owned.
        }

        private void HandleControlButtonState()
        {
            bool isDrinkMode = policy != null && policy.IsDrinkModeActive();
            bool controlsBlockedByUI = isUIHidden || isUIAnimating;
            bool backlogOpen = IsBacklogOpen;
            bool skipAutoInteractable = !isDrinkMode && !controlsBlockedByUI && !backlogOpen;
            bool exitInteractable = !isDrinkMode && !controlsBlockedByUI && !backlogOpen;
            bool hideUIInteractable = !isDrinkMode && !controlsBlockedByUI;
            bool typingInProgress = (typer != null && typer.IsTyping) || !lineCompleted || inputLocked;
            bool saveAllowedByRunner = runner == null || runner.SaveAllowed;
            bool saveLoadInteractable = !isDrinkMode && !typingInProgress && saveAllowedByRunner && !controlsBlockedByUI;
            bool controlLockActive = Time.unscaledTime < controlActionLockedUntil;

            SetButtonInteractable(skipButton, skipAutoInteractable && !controlLockActive, ref lastSkipButtonInteractable);
            SetButtonInteractable(autoPlayButton, skipAutoInteractable && !controlLockActive, ref lastAutoButtonInteractable);
            SetButtonInteractable(exitButton, exitInteractable && !controlLockActive, ref lastExitButtonInteractable);
            SetButtonInteractable(saveLoadButton, saveLoadInteractable && !controlLockActive, ref lastSaveLoadButtonInteractable);
            SetButtonInteractable(hideUIButton, hideUIInteractable && !controlLockActive, ref lastHideUIButtonInteractable);
            RefreshButtonVisualStates();
        }

        private static void SetButtonInteractable(Button button, bool interactable, ref bool? cachedState)
        {
            if (button == null)
                return;

            if (cachedState.HasValue && cachedState.Value == interactable)
                return;

            button.interactable = interactable;
            cachedState = interactable;
        }

        private void RefreshButtonVisualStates()
        {
            if (buttonVisualBindings == null || buttonVisualBindings.Length == 0)
                return;

            var state = new ButtonVisualState(
                runner != null && runner.IsAutoPlayEnabled,
                runner != null && runner.IsHoldSkipInputActive);

            for (int i = 0; i < buttonVisualBindings.Length; i++)
            {
                var binding = buttonVisualBindings[i];
                var button = binding.button;
                if (button == null)
                    continue;

                var targetImage = ResolveBindingTargetImage(binding, button);
                if (targetImage == null)
                    continue;

                bool isActive = EvaluateBindingState(binding, button, state);

                var nextSprite = isActive ? binding.activeSprite : binding.inactiveSprite;
                if (nextSprite == null || targetImage.sprite == nextSprite)
                    continue;

                targetImage.sprite = nextSprite;
            }
        }

        private static Image ResolveBindingTargetImage(ButtonVisualBinding binding, Button button)
        {
            return binding.targetImage != null ? binding.targetImage : button.image;
        }

        private bool EvaluateBindingState(ButtonVisualBinding binding, Button button, ButtonVisualState state)
        {
            return binding.visualMode switch
            {
                ButtonVisualMode.ToggleAutoPlay => state.AutoEnabled && button.interactable,
                ButtonVisualMode.HoldSkip => state.HoldSkipEnabled && button.interactable,
                ButtonVisualMode.InteractableEnabled => button.interactable,
                _ => button.interactable
                     && interactableVisualPressedStates.TryGetValue(button, out var isPressed)
                     && isPressed
            };
        }



        public void LockInputFrames(int frames = 2)
        {
            inputLockFrames = Mathf.Max(inputLockFrames, frames);
        }

        public void Open()
        {
            Debug.Log("[VN] Open called → forcing refresh");
            gameObject.SetActive(true);
            StartWaitAndRefresh();
        }

        private void RefreshCurrentLine()
        {
            ForceRefreshCurrentLine();
        }

        private void StartWaitAndRefresh()
        {
            StopWaitAndRefresh();
            waitAndRefreshCoroutine = StartCoroutine(WaitAndRefresh());
        }

        private void StopWaitAndRefresh()
        {
            if (waitAndRefreshCoroutine == null)
                return;

            StopCoroutine(waitAndRefreshCoroutine);
            waitAndRefreshCoroutine = null;
        }

        private IEnumerator WaitAndRefresh()
        {
            const int maxWaitFrames = 10;
            int remaining = maxWaitFrames;
            bool loggedNullState = false;
            Debug.Log("[VN] WaitAndRefresh start");

            while (runner == null || !runner.HasValidNode())
            {
                if (!loggedNullState)
                {
                    Debug.Log($"[VN] WaitAndRefresh pending runner={(runner != null)} hasValidNode={(runner != null && runner.HasValidNode())}");
                    loggedNullState = true;
                }

                yield return null;
                remaining--;
                if (remaining <= 0)
                {
                    Debug.LogError("[VN] Runner not ready after wait");
                    waitAndRefreshCoroutine = null;
                    yield break;
                }
            }

            Debug.Log("[VN] WaitAndRefresh ready -> ForceRefreshCurrentLine");
            ForceRefreshCurrentLine();
            waitAndRefreshCoroutine = null;
        }

        private void ForceRefreshCurrentLine()
        {
            if (runner == null)
            {
                Debug.LogError("[VN] ForceRefresh currentNode is unavailable (runner null)");
                return;
            }

            if (!runner.TryGetCurrentSayState(out var currentNodeId, out var refreshedLineIndex, out var currentText, out var currentSpeaker))
            {
                Debug.LogWarning("[VN] no node → retry next frame");
                StartWaitAndRefresh();

                if (dialogueText != null && !string.IsNullOrEmpty(currentFullText))
                {
                    dialogueText.text = currentFullText;
                    Debug.Log($"[VN] Reopen fallback text={currentFullText}");
                }
                else if (dialogueText != null)
                {
                    dialogueText.text = string.Empty;
                }

                lineDisplayed = !string.IsNullOrEmpty(dialogueText != null ? dialogueText.text : string.Empty);
                lineCompleted = true;

                Debug.LogWarning("[VN] ForceRefresh skipped: no current Say node");
                Debug.Log($"[VN] lineIndex={currentLineIndex}, displayed={lineDisplayed}");
                return;
            }

            if (nameText != null)
                nameText.text = currentSpeaker;

            if (dialogueText != null)
                dialogueText.text = currentText;

            currentFullText = currentText ?? string.Empty;
            currentLineIndex = refreshedLineIndex;
            lineDisplayed = true;
            lineCompleted = true;
            runner.SyncPointerAfterRefresh(refreshedLineIndex);
            if (typer != null)
            {
                typer.ForceComplete(); // 무조건 죽여
            }

            inputLocked = false;

            bool isTyping = typer != null && typer.IsTyping;
            bool isWaitingInput = lineCompleted;
            Debug.Log($"[VN] Reopen currentNode={currentNodeId}, lineIndex={refreshedLineIndex}, text={currentFullText}");
            Debug.Log($"[VN] CurrentNode={currentNodeId}, LineIndex={refreshedLineIndex}");
            Debug.Log($"[VN] CurrentText={currentFullText}");
            Debug.Log($"[VN] dialogueText.text={dialogueText?.text}");
            Debug.Log($"[VN] isWaitingInput={isWaitingInput}, isTyping={isTyping}");
            Debug.Log($"[VN] lineIndex={currentLineIndex}, displayed={lineDisplayed}");
            Debug.Log($"[VN] ForceRefresh → {currentFullText}");
        }

        public void OnStateLoadedForValidation()
        {
            lastHandledLineId = null;
            ForceRefreshCurrentLine();

            bool isTyping = typer != null && typer.IsTyping;
            Debug.Log($"[CHECK] displayed={lineDisplayed}");
            Debug.Log($"[CHECK] isTyping={isTyping}");
            Debug.Log($"[CHECK] inputLocked={inputLocked}");
        }

        private void HandleSay(string speakerId, string text, string lineId, VNBacklogKey backlogKey)
        {
            if (typer != null)
            {
                typer.ForceComplete(); // 기존 타이퍼 무조건 죽여
            }

            if (lastHandledLineId == lineId)
            {
                Debug.Log("[VN] HandleSay duplicate blocked");
                return;
            }
            lastHandledLineId = lineId;

            inputLockFrames = 1;

            lineCompleted = false;
            lineDisplayed = true;
            currentFullText = text ?? "";
            currentLineBacklogKey = backlogKey;

            runner?.MarkSeen(lineId);
            runner?.BacklogSetCurrentLineTyping(true);
            if (backlogKey != null)
                runner?.BacklogUpdateLineText(backlogKey, string.Empty);

            if (nameText != null) nameText.text = speakerId ?? "";
            if (dialogueText != null && !lineDisplayed)
                dialogueText.text = "";

            runner?.MarkSaveAllowed(false, "Typing Start");

            // ✅ 타이퍼 없으면 즉시 출력
            if (typer == null)
            {
                if (dialogueText != null) dialogueText.text = currentFullText;
                lineCompleted = true;
                lineDisplayed = true;
                runner?.BacklogUpdateLineText(currentLineBacklogKey, currentFullText);
                runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);

                runner?.MarkSaveAllowed(true, "No Typer => Immediate");
                runner?.NotifyLineTypedEnd();
                Debug.Log("[VN] SaveAllowed TRUE (No Typer => Immediate)");
                return;
            }

            if (runner != null && runner.IsSkipMode && !runner.IsHoldSkipInputActive)
            {
                dialogueText.text = currentFullText;
                lineCompleted = true;
                lineDisplayed = true;
                runner?.BacklogUpdateLineText(currentLineBacklogKey, currentFullText);
                runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);

                runner?.NotifyLineTypedEnd();
                runner?.MarkSaveAllowed(true, "Skip Immediate");
                return;
            }

            // ✅ 타이핑 시작
            lineCompleted = false;
            typer.StartTyping(currentFullText, onCompleted: () =>
            {
                lineCompleted = true;
                lineDisplayed = true;
                runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);
                runner?.NotifyLineTypedEnd();

                runner?.MarkSaveAllowed(true, "Typing Completed");
                Debug.Log("[VN] SaveAllowed TRUE (Typing Completed)");
            }, onUpdated: partial =>
            {
                runner?.BacklogUpdateLineText(currentLineBacklogKey, partial);
            });
        }

        private void ForceCompleteLine()
        {
            if (typer != null) typer.ForceComplete();
            else if (dialogueText != null) dialogueText.text = currentFullText;

            lineDisplayed = true;
            lineCompleted = true; // 안전하게 유지
            runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);
            runner?.MarkJustForceCompletedThisFrame();
        }

        public bool TryCompleteCurrentLineForSkip()
        {
            if (typer == null || !typer.IsTyping) return false;

            typer.ForceComplete();
            lineDisplayed = true;
            lineCompleted = true;
            runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);
            runner?.MarkJustForceCompletedThisFrame();
            return true;
        }

        public bool IsCurrentLineTyping()
        {
            return typer != null && typer.IsTyping;
        }

        public void FinalizeCurrentLineAfterForceComplete()
        {
            lineDisplayed = true;
            lineCompleted = true;
            runner?.BacklogFinalizeLine(currentLineBacklogKey, currentFullText);
            runner?.MarkJustForceCompletedThisFrame();
        }

        public void SetSkip(bool value)
        {
            if (!value) return;
            OnSkipButtonClicked();
        }

        public void ToggleSkip()
        {
            if (IsAnyBacklogOpen)
                return;

            OnSkipButtonClicked();
        }

        public void SetAutoPlay(bool value)
        {
            if (IsAnyBacklogOpen)
                return;

            if ((isUIHidden || isUIAnimating) && value)
                return;
            if (IsBacklogInputBlocked())
                return;

            autoPlayEnabled = value;
            runner?.SetAutoPlay(value, "VNDialogueView UI");
        }

        public void ToggleAuto()
        {
            if (IsAnyBacklogOpen)
                return;

            if (isUIHidden || isUIAnimating)
                return;
            if (IsBacklogInputBlocked())
                return;

            bool nextValue = !(runner != null && runner.IsAutoPlayEnabled);
            SetAutoPlay(nextValue);
        }

        public void OnSkipButtonClicked()
        {
            if (IsAnyBacklogOpen)
                return;

            if (TryCompleteCurrentLineForSkip())
                return;
        }

        public void OnSkipButtonPointerDown()
        {
            if (IsAnyBacklogOpen)
                return;

            if (isUIHidden || isUIAnimating)
                return;
            if (IsBacklogInputBlocked())
                return;
            if (policy != null && policy.IsDrinkPanelOpen)
                return;
            if (policy != null && !VNInputGate.CanUseSkipOrAuto(policy))
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;

            runner?.ForceAutoOff("Skip Hold Button");
            runner?.SetUiSkipHeld(true, "VNDialogueView Skip Hold");
            RefreshButtonVisualStates();
        }

        public void OnSkipButtonPointerUp()
        {
            TryCompleteCurrentLineForSkip();

            runner?.SetUiSkipHeld(false, "VNDialogueView Skip Hold");
            RefreshButtonVisualStates();
        }

        public void OnAutoPlayButtonClicked()
        {
            if (IsAnyBacklogOpen)
                return;

            if (isUIHidden || isUIAnimating)
                return;
            if (IsBacklogInputBlocked())
                return;
            if (policy != null && !VNInputGate.CanUseSkipOrAuto(policy))
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;
            ToggleAuto();
            RefreshButtonVisualStates();
        }

        public void OnExitButtonClicked()
        {
            if (isUIHidden || isUIAnimating)
                return;
            if (IsBacklogInputBlocked())
                return;
            if (policy != null && policy.IsDrinkPanelOpen)
                return;
            if (policy != null && policy.IsSaveLoadModalOpen)
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;
            controlActionLockedUntil = Time.unscaledTime + closeActionLockSeconds;
            // 키보드 3 / 우측 상단 닫기와 동일한 OS RequestClose 경로 사용.
            if (bridge != null)
                bridge.RequestCloseFromUI();
            else if (osBridge != null)
                osBridge.RequestCloseFromUI();
            else
                closePopupController?.Show(); // legacy fallback
        }

        public void OpenSaveLoadWindow()
        {
            if (isUIHidden || isUIAnimating)
                return;
            if (policy != null && policy.IsDrinkModeActive())
                return;

            if (saveLoadWindow == null)
            {
                Debug.LogWarning("[VN_UI] SaveLoadWindow reference missing.");
                return;
            }

            runner?.ForceAutoOff("Open SaveLoad Window");
            runner?.SetUiSkipHeld(false, "Open SaveLoad Window");
            saveLoadWindow.Open();
        }

        public void HideUI()
        {
            SoundManager.Instance.PlayOS(OSSoundEvent.Minimize);

            if (isUIAnimating || isUIHidden)
                return;
            if (policy != null && policy.IsDrinkModeActive())
                return;

            ResolveDialogueUIRefs();
            ResolveMinimizedUIRefs();
            StartCoroutine(HideUICoroutine());
        }

        public void ShowUI()
        {
            SoundManager.Instance.PlayOS(OSSoundEvent.Restore);

            if (isUIAnimating || !isUIHidden)
                return;

            ResolveDialogueUIRefs();
            ResolveMinimizedUIRefs();
            StartCoroutine(ShowUICoroutine());
        }

        private IEnumerator HideUICoroutine()
        {
            if (dialogueRoot == null || dialogueCanvasGroup == null)
            {
                Debug.LogWarning("[VN_UI] HideUI failed: dialogueRoot/dialogueCanvasGroup not resolved.");
                yield break;
            }

            isUIAnimating = true;
            isUIDisappearing = true;
            LockAllInput(true);
            runner?.SetUiSkipHeld(false, "Hide UI");
            SetAutoPlay(false);

            yield return CoAnimateScaleAlpha(
                dialogueRoot,
                dialogueCanvasGroup,
                fromScale: Vector3.one,
                toScale: animFromScale,
                fromAlpha: 1f,
                toAlpha: 0f,
                duration: dialogueHideDuration);

            dialogueRoot.gameObject.SetActive(false);

            if (minimizedUIRoot != null)
            {
                minimizedUIRoot.SetActive(true);
                var miniRoot = minimizedUIRoot.transform as RectTransform;
                var miniGroup = GetOrAddCanvasGroup(minimizedUIRoot);

                if (miniRoot != null && miniGroup != null)
                {
                    miniGroup.interactable = false;
                    miniGroup.blocksRaycasts = false;
                    yield return CoAnimateScaleAlpha(
                        miniRoot,
                        miniGroup,
                        fromScale: animFromScale,
                        toScale: Vector3.one,
                        fromAlpha: 0f,
                        toAlpha: 1f,
                        duration: minimizedShowDuration);
                    miniGroup.interactable = true;
                    miniGroup.blocksRaycasts = true;
                }
            }

            isUIHidden = true;
            isUIAnimating = false;
        }

        private IEnumerator ShowUICoroutine()
        {
            if (dialogueRoot == null || dialogueCanvasGroup == null)
            {
                Debug.LogWarning("[VN_UI] ShowUI failed: dialogueRoot/dialogueCanvasGroup not resolved.");
                yield break;
            }

            isUIAnimating = true;
            isUIDisappearing = false;
            LockAllInput(true);

            if (minimizedUIRoot != null && minimizedUIRoot.activeSelf)
            {
                var miniRoot = minimizedUIRoot.transform as RectTransform;
                var miniGroup = GetOrAddCanvasGroup(minimizedUIRoot);
                if (miniRoot != null && miniGroup != null)
                {
                    miniGroup.interactable = false;
                    miniGroup.blocksRaycasts = false;
                    yield return CoAnimateScaleAlpha(
                        miniRoot,
                        miniGroup,
                        fromScale: Vector3.one,
                        toScale: animFromScale,
                        fromAlpha: miniGroup.alpha,
                        toAlpha: 0f,
                        duration: minimizedHideDuration);
                }

                minimizedUIRoot.SetActive(false);
            }

            dialogueRoot.gameObject.SetActive(true);
            yield return CoAnimateScaleAlpha(
                dialogueRoot,
                dialogueCanvasGroup,
                fromScale: animFromScale,
                toScale: Vector3.one,
                fromAlpha: 0f,
                toAlpha: 1f,
                duration: dialogueShowDuration);

            isUIHidden = false;
            isUIAnimating = false;
            LockAllInput(false);
        }

        private void LockAllInput(bool locked)
        {
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.interactable = !locked;
                dialogueCanvasGroup.blocksRaycasts = !locked;
            }

            inputLocked = locked;
            runner?.SetUiInputBlocked(locked, "Dialogue UI Hide/Show");
            if (locked)
                OnSkipButtonPointerUp();
        }

        public void CollectWindowStates(List<VNWindowStateData> states)
        {
            if (states == null)
                return;

            CollectSingleWindowState(dialogueRoot, dialogueWindowDragStateSource, DialogueWindowId, states);
            CollectSingleWindowState(minimizedUIRoot != null ? minimizedUIRoot.transform as RectTransform : null, minimizedDialogueDragStateSource, HiddenDialogueWindowId, states);
        }

        public void ApplyWindowStates(IReadOnlyList<VNWindowStateData> states)
        {
            if (states == null || states.Count == 0)
                return;

            ApplySingleWindowState(dialogueRoot, dialogueWindowDragStateSource, DialogueWindowId, states);
            ApplySingleWindowState(minimizedUIRoot != null ? minimizedUIRoot.transform as RectTransform : null, minimizedDialogueDragStateSource, HiddenDialogueWindowId, states);
        }

        private static void CollectSingleWindowState(RectTransform rectTransform, UIDragMoveClamped dragSource, string windowId, List<VNWindowStateData> states)
        {
            if (rectTransform == null || string.IsNullOrWhiteSpace(windowId))
                return;

            if (dragSource == null)
                dragSource = rectTransform.GetComponent<UIDragMoveClamped>();

            if (dragSource != null && dragSource.TryGetWindowState(windowId, out var windowState))
            {
                states.Add(windowState);
                return;
            }

            states.Add(new VNWindowStateData
            {
                windowId = windowId,
                anchoredX = rectTransform.anchoredPosition.x,
                anchoredY = rectTransform.anchoredPosition.y,
                isPinned = false,
                siblingIndex = rectTransform.GetSiblingIndex()
            });
        }

        private static void ApplySingleWindowState(RectTransform rectTransform, UIDragMoveClamped dragSource, string windowId, IReadOnlyList<VNWindowStateData> states)
        {
            if (rectTransform == null || string.IsNullOrWhiteSpace(windowId))
                return;

            var saved = FindWindowState(states, windowId);
            if (saved == null)
                return;

            if (dragSource == null)
                dragSource = rectTransform.GetComponent<UIDragMoveClamped>();

            if (dragSource != null)
            {
                dragSource.ApplyWindowState(saved);
                return;
            }

            rectTransform.anchoredPosition = new Vector2(saved.anchoredX, saved.anchoredY);
            if (rectTransform.parent != null)
            {
                int clampedIndex = Mathf.Clamp(saved.siblingIndex, 0, rectTransform.parent.childCount - 1);
                rectTransform.SetSiblingIndex(clampedIndex);
            }
        }

        private static VNWindowStateData FindWindowState(IReadOnlyList<VNWindowStateData> states, string windowId)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var row = states[i];
                if (row == null || string.IsNullOrWhiteSpace(row.windowId))
                    continue;

                if (string.Equals(row.windowId, windowId, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }

        private IEnumerator ReplayClick()
        {
            yield return null;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                yield break;

            var pointerData = new PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null)
                    continue;

                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }

        private void HandleEnd()
        {
            // 엔드 처리 (지금은 비워도 됨)
        }
    }
}
