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
        [SerializeField] private Button openBacklogButton;
        [SerializeField] private Button closeBacklogButton;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private RectTransform dialogueRoot;
        [SerializeField] private GameObject minimizedUIRoot;
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

        private enum ButtonVisualMode
        {
            Interactable = 0,
            ToggleAutoPlay = 1,
            HoldSkip = 2
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
            EnsureBacklogRuntimeSetup();
            if (backlogView == null) backlogView = GetComponentInChildren<VNBacklogView>(true);
            backlogView?.BindManager(runner != null ? runner.BacklogManager : null);
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
            BindBacklogButtons();
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")}");
        }

        private void EnsureBacklogRuntimeSetup()
        {
            if (backlogView != null)
                return;

            var existing = transform.Find("BacklogWindow");
            if (existing != null)
            {
                backlogView = existing.GetComponent<VNBacklogView>();
                if (backlogView != null)
                    return;
            }

            var backlogWindow = new GameObject("BacklogWindow", typeof(RectTransform), typeof(CanvasGroup), typeof(VNBacklogView));
            var backlogRect = backlogWindow.GetComponent<RectTransform>();
            backlogRect.SetParent(transform, false);
            backlogRect.anchorMin = Vector2.zero;
            backlogRect.anchorMax = Vector2.one;
            backlogRect.offsetMin = Vector2.zero;
            backlogRect.offsetMax = Vector2.zero;

            var group = backlogWindow.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var dimmer = new GameObject("BacklogDimmer", typeof(RectTransform), typeof(Image), typeof(Button));
            var dimmerRect = dimmer.GetComponent<RectTransform>();
            dimmerRect.SetParent(backlogRect, false);
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;
            var dimmerImage = dimmer.GetComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.55f);
            var dimmerButton = dimmer.GetComponent<Button>();
            dimmerButton.onClick.AddListener(CloseBacklogWindow);

            var panel = new GameObject("BacklogPanel", typeof(RectTransform), typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(backlogRect, false);
            panelRect.anchorMin = new Vector2(0.12f, 0.15f);
            panelRect.anchorMax = new Vector2(0.88f, 0.85f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.91f, 0.91f, 0.91f, 1f);

            var header = CreateTMPText("Header", panelRect, "Backlog", 30f, 1f, 1f, 56f, FontStyles.Bold);
            header.alignment = TextAlignmentOptions.Left;
            header.margin = new Vector4(18f, 10f, 0f, 0f);

            var closeBtn = CreateButton("CloseButton", panelRect, new Vector2(64f, 42f), new Vector2(1f, 1f), new Vector2(-12f, -8f), "닫기");
            closeBtn.onClick.AddListener(CloseBacklogWindow);
            closeBacklogButton = closeBtn;

            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollRect.SetParent(panelRect, false);
            scrollRect.anchorMin = new Vector2(0.03f, 0.08f);
            scrollRect.anchorMax = new Vector2(0.97f, 0.88f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            var viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.SetParent(scrollRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var emptyText = CreateTMPText("EmptyText", panelRect, "기록이 없습니다.", 28f, 0f, 1f, 46f, FontStyles.Normal);
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = new Color(0f, 0f, 0f, 0.7f);

            var template = CreateBacklogItemTemplate(contentRect);
            template.gameObject.SetActive(false);

            backlogView = backlogWindow.GetComponent<VNBacklogView>();
            backlogView.Configure(backlogWindow, scroll, contentRect, template, emptyText, group);
            backlogView.Close();
        }

        private VNBacklogItemView CreateBacklogItemTemplate(Transform parent)
        {
            var item = new GameObject("BacklogItemTemplate", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(VNBacklogItemView));
            var rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 110f);

            var layout = item.GetComponent<LayoutElement>();
            layout.minHeight = 96f;
            layout.preferredHeight = 110f;
            layout.flexibleHeight = 0f;

            var bg = item.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f);

            var speaker = CreateTMPText("SpeakerText", rect, "Speaker", 24f, 0f, 1f, 34f, FontStyles.Bold);
            speaker.alignment = TextAlignmentOptions.TopLeft;
            speaker.margin = new Vector4(12f, 6f, 0f, 0f);

            var body = CreateTMPText("BodyText", rect, "Dialogue", 22f, 0f, 1f, 72f, FontStyles.Normal);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.margin = new Vector4(12f, 40f, 12f, 0f);
            body.enableWordWrapping = true;

            var itemView = item.GetComponent<VNBacklogItemView>();
            itemView.Configure(speaker, body);
            return itemView;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 anchoredPos, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            go.GetComponent<Image>().color = new Color(0.86f, 0.86f, 0.86f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var text = CreateTMPText("Text", rect, label, 20f, 0f, 1f, size.y, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TextMeshProUGUI CreateTMPText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            float anchorMinY,
            float anchorMaxY,
            float height,
            FontStyles fontStyle)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, anchorMinY);
            rect.anchorMax = new Vector2(1f, anchorMaxY);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, height * -1f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = Color.black;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void BindBacklogButtons()
        {
            if (openBacklogButton == null)
                openBacklogButton = FindButtonByLabel("이전대사");

            if (openBacklogButton != null)
            {
                openBacklogButton.onClick.RemoveListener(ToggleBacklogWindow);
                openBacklogButton.onClick.AddListener(ToggleBacklogWindow);
            }

            if (closeBacklogButton != null)
            {
                closeBacklogButton.onClick.RemoveListener(CloseBacklogWindow);
                closeBacklogButton.onClick.AddListener(CloseBacklogWindow);
            }
        }

        private Button FindButtonByLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return null;

            var buttons = buttonContainerRoot != null
                ? buttonContainerRoot.GetComponentsInChildren<Button>(true)
                : GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null)
                    continue;

                var text = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null && text.text != null && text.text.IndexOf(label, System.StringComparison.Ordinal) >= 0)
                    return btn;
            }

            return null;
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
                if (button == null || binding.visualMode != ButtonVisualMode.Interactable)
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

            StartWaitAndRefresh();
            RefreshButtonVisualStates();
        }

        private void OnDisable()
        {
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
        }

        private void OnDestroy()
        {
            backlogView?.UnbindManager();
        }

        private void Update()
        {
            HandleControlButtonState();
            if (isUIHidden || isUIAnimating)
                return;

            if (runner != null && runner.IsWaiting && runner.CallStackCount > 0)
            {
                return;
            }

            if (policy != null && policy.IsDrinkPanelOpen && autoPlayEnabled)
            {
                autoPlayEnabled = false;
                runner?.SetAutoPlay(false, "Drink Mode Auto Off");
            }

            if (!CanAcceptVNInput()) return;
            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (!runner.HasScript) return;
            HandleSkipAutoState();

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
            lineDisplayed = false;
            runner.Next();
            Debug.Log("[VN_UI] Next input detected -> runner.Next()");
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
            bool skipAutoInteractable = !isDrinkMode && !controlsBlockedByUI;
            bool exitInteractable = !isDrinkMode && !controlsBlockedByUI;
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

            for (int i = 0; i < buttonVisualBindings.Length; i++)
            {
                var binding = buttonVisualBindings[i];
                var button = binding.button;
                if (button == null)
                    continue;

                var targetImage = binding.targetImage != null ? binding.targetImage : button.image;
                if (targetImage == null)
                    continue;

                bool isActive = binding.visualMode switch
                {
                    ButtonVisualMode.ToggleAutoPlay => runner != null && runner.IsAutoPlayEnabled && button.interactable,
                    ButtonVisualMode.HoldSkip => runner != null && runner.IsHoldSkipInputActive && button.interactable,
                    _ => button.interactable
                         && interactableVisualPressedStates.TryGetValue(button, out var isPressed)
                         && isPressed
                };

                var nextSprite = isActive ? binding.activeSprite : binding.inactiveSprite;
                if (nextSprite == null || targetImage.sprite == nextSprite)
                    continue;

                targetImage.sprite = nextSprite;
            }
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
            if (typer != null && typer.IsTyping)
                typer.ForceComplete();
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
            ForceRefreshCurrentLine();

            bool isTyping = typer != null && typer.IsTyping;
            Debug.Log($"[CHECK] displayed={lineDisplayed}");
            Debug.Log($"[CHECK] isTyping={isTyping}");
            Debug.Log($"[CHECK] inputLocked={inputLocked}");
        }

        private void HandleSay(string speakerId, string text, string lineId, VNBacklogKey backlogKey)
        {
          
            inputLockFrames = 1;

            lineCompleted = false;
            lineDisplayed = true;
            currentFullText = text ?? "";

            runner?.MarkSeen(lineId);
            runner?.BacklogSetCurrentLineTyping(true);
            if (backlogKey != null)
                runner?.BacklogUpdateCurrentLineText(string.Empty);

            if (nameText != null) nameText.text = speakerId ?? "";
            if (dialogueText != null) dialogueText.text = "";

            runner?.MarkSaveAllowed(false, "Typing Start");

            // ✅ 타이퍼 없으면 즉시 출력
            if (typer == null)
            {
                if (dialogueText != null) dialogueText.text = currentFullText;
                lineCompleted = true;
                lineDisplayed = true;
                runner?.BacklogUpdateCurrentLineText(currentFullText);
                runner?.BacklogFinalizeCurrentLine(currentFullText);

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
                runner?.BacklogUpdateCurrentLineText(currentFullText);
                runner?.BacklogFinalizeCurrentLine(currentFullText);

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
                runner?.BacklogFinalizeCurrentLine(currentFullText);
                runner?.NotifyLineTypedEnd();

                runner?.MarkSaveAllowed(true, "Typing Completed");
                Debug.Log("[VN] SaveAllowed TRUE (Typing Completed)");
            }, onUpdated: partial =>
            {
                runner?.BacklogUpdateCurrentLineText(partial);
            });
        }

        private void ForceCompleteLine()
        {
            if (typer != null) typer.ForceComplete();
            else if (dialogueText != null) dialogueText.text = currentFullText;

            lineDisplayed = true;
            lineCompleted = true; // 안전하게 유지
            runner?.BacklogFinalizeCurrentLine(currentFullText);
        }

        public bool TryCompleteCurrentLineForSkip()
        {
            if (lineCompleted) return false;
            if (typer == null || !typer.IsTyping) return false;

            typer.ForceComplete();
            lineCompleted = true;
            return true;
        }

        public void SetSkip(bool value)
        {
            if (!value) return;
            OnSkipButtonClicked();
        }

        public void ToggleSkip()
        {
            OnSkipButtonClicked();
        }

        public void SetAutoPlay(bool value)
        {
            if ((isUIHidden || isUIAnimating) && value)
                return;

            autoPlayEnabled = value;
            runner?.SetAutoPlay(value, "VNDialogueView UI");
        }

        public void ToggleAuto()
        {
            if (isUIHidden || isUIAnimating)
                return;

            bool nextValue = !(runner != null && runner.IsAutoPlayEnabled);
            SetAutoPlay(nextValue);
        }

        public void OnSkipButtonClicked()
        {
            // Hold-based skip only. Keep empty to avoid one-shot skip on click.
        }

        public void OnSkipButtonPointerDown()
        {
            if (isUIHidden || isUIAnimating)
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
            runner?.SetUiSkipHeld(false, "VNDialogueView Skip Hold");
            RefreshButtonVisualStates();
        }

        public void OnAutoPlayButtonClicked()
        {
            if (isUIHidden || isUIAnimating)
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
            if (policy != null && policy.IsDrinkPanelOpen)
                return;
            if (policy != null && policy.IsSaveLoadModalOpen)
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
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

        public void ToggleBacklogWindow()
        {
            backlogView?.Toggle();
        }

        public void OpenBacklogWindow()
        {
            backlogView?.Open();
        }

        public void CloseBacklogWindow()
        {
            backlogView?.Close();
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
