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
        [SerializeField] private VNSaveLoadWindow saveLoadWindow;
        // Legacy compatibility: kept hidden so partial merges referencing old fields still compile.
        [SerializeField, HideInInspector] private bool autoPlayEnabled;
        [SerializeField, Min(0f)] private float closeActionLockSeconds = 0.15f;

        [Header("Typing")]
        [SerializeField] private float charsPerSecond = 40f;

        // 현재 라인의 풀텍스트 (SkipToEnd용)
        private string currentFullText = "";
        private bool lineCompleted = true; // true면 Next로 "다음 라인" 가능
        private int inputLockFrames = 0;
        private bool subscribed;
        private bool? lastSkipButtonInteractable;
        private bool? lastAutoButtonInteractable;
        private bool? lastExitButtonInteractable;
        private bool? lastSaveLoadButtonInteractable;
        private bool skipHoldBindingApplied;
        private float controlActionLockedUntil;

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
            if (advanceClickArea == null && dialogueText != null)
                advanceClickArea = dialogueText.rectTransform;
            if (graphicRaycaster == null)
                graphicRaycaster = GetComponentInParent<GraphicRaycaster>(true);
            AutoBindSaveLoadButton();
            SetupSkipHoldBinding();
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")}");
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
            LockInputFrames(5); // 여기 추가 (2 말고 5 추천)
            Debug.Log($"[VN] DialogueView OnEnable text={(dialogueText != null ? dialogueText.text : "<null>")}");

            RefreshCurrentLine();

            if (subscribed) return;
            if (runner == null) return;

            runner.OnSay += HandleSay;
            runner.OnEnd += HandleEnd;
            subscribed = true;
        }

        private void OnDisable()
        {
            OnSkipButtonPointerUp();

            if (!subscribed) return;
            if (runner == null) return;

            runner.OnSay -= HandleSay;
            runner.OnEnd -= HandleEnd;
            subscribed = false;
        }

        private void Update()
        {
            HandleControlButtonState();

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
            runner.ForceAutoOff(pressedSpace ? "User input (Space)" : "User input (Click)");

            // ✅ 타이핑 중이면 "완성"만 하고 다음 라인으로 넘어가진 않음
            if (!lineCompleted && typer != null && typer.IsTyping)
            {
                ForceCompleteLine();
                return;
            }

            // ✅ 그 외에는 다음 라인
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
            bool isDrinkMode = policy != null && policy.IsDrinkPanelOpen;
            bool skipAutoInteractable = !isDrinkMode && (policy == null || VNInputGate.CanUseSkipOrAuto(policy));
            bool exitInteractable = !isDrinkMode;
            bool saveLoadInteractable = !isDrinkMode;
            bool controlLockActive = Time.unscaledTime < controlActionLockedUntil;

            SetButtonInteractable(skipButton, skipAutoInteractable && !controlLockActive, ref lastSkipButtonInteractable);
            SetButtonInteractable(autoPlayButton, skipAutoInteractable && !controlLockActive, ref lastAutoButtonInteractable);
            SetButtonInteractable(exitButton, exitInteractable && !controlLockActive, ref lastExitButtonInteractable);
            SetButtonInteractable(saveLoadButton, saveLoadInteractable && !controlLockActive, ref lastSaveLoadButtonInteractable);
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



        public void LockInputFrames(int frames = 2)
        {
            inputLockFrames = Mathf.Max(inputLockFrames, frames);
        }

        public void Open()
        {
            Debug.Log("[VN] Open called → forcing refresh");
            gameObject.SetActive(true);
            ForceRefreshCurrentLine();
        }

        private void RefreshCurrentLine()
        {
            ForceRefreshCurrentLine();
        }

        private void ForceRefreshCurrentLine()
        {
            if (runner == null)
            {
                Debug.LogError("[VN] ForceRefresh currentNode is unavailable (runner null)");
                return;
            }

            if (!runner.TryGetCurrentSayState(out var currentNodeId, out var currentLineIndex, out var currentText, out var currentSpeaker))
            {
                if (dialogueText != null && !string.IsNullOrEmpty(currentFullText))
                {
                    dialogueText.text = currentFullText;
                    Debug.Log($"[VN] Reopen fallback text={currentFullText}");
                }
                else if (dialogueText != null)
                {
                    dialogueText.text = string.Empty;
                }

                Debug.LogWarning("[VN] ForceRefresh skipped: no current Say node");
                return;
            }

            if (nameText != null)
                nameText.text = currentSpeaker;

            if (dialogueText != null)
                dialogueText.text = currentText;

            currentFullText = currentText ?? string.Empty;
            lineCompleted = true;

            bool isTyping = typer != null && typer.IsTyping;
            bool isWaitingInput = lineCompleted;
            Debug.Log($"[VN] Reopen currentNode={currentNodeId}, lineIndex={currentLineIndex}, text={currentFullText}");
            Debug.Log($"[VN] CurrentNode={currentNodeId}, LineIndex={currentLineIndex}");
            Debug.Log($"[VN] CurrentText={currentFullText}");
            Debug.Log($"[VN] dialogueText.text={dialogueText?.text}");
            Debug.Log($"[VN] isWaitingInput={isWaitingInput}, isTyping={isTyping}");
            Debug.Log($"[VN] ForceRefresh → {currentFullText}");
        }

        private void HandleSay(string speakerId, string text, string lineId)
        {
          
            inputLockFrames = 1;

            lineCompleted = false;
            currentFullText = text ?? "";

            runner?.MarkSeen(lineId);

            if (nameText != null) nameText.text = speakerId ?? "";
            if (dialogueText != null) dialogueText.text = "";

            runner?.MarkSaveAllowed(false, "Typing Start");

            // ✅ 타이퍼 없으면 즉시 출력
            if (typer == null)
            {
                if (dialogueText != null) dialogueText.text = currentFullText;
                lineCompleted = true;

                runner?.MarkSaveAllowed(true, "No Typer => Immediate");
                runner?.NotifyLineTypedEnd();
                Debug.Log("[VN] SaveAllowed TRUE (No Typer => Immediate)");
                return;
            }

            if (runner != null && runner.IsSkipMode && !runner.IsHoldSkipInputActive)
            {
                dialogueText.text = currentFullText;
                lineCompleted = true;

                runner?.NotifyLineTypedEnd();
                runner?.MarkSaveAllowed(true, "Skip Immediate");
                return;
            }

            // ✅ 타이핑 시작
            lineCompleted = false;
            typer.StartTyping(currentFullText, onCompleted: () =>
            {
                lineCompleted = true;
                runner?.NotifyLineTypedEnd();

                runner?.MarkSaveAllowed(true, "Typing Completed");
                Debug.Log("[VN] SaveAllowed TRUE (Typing Completed)");
            });
        }

        private void ForceCompleteLine()
        {
            if (typer != null) typer.ForceComplete();
            else if (dialogueText != null) dialogueText.text = currentFullText;

            lineCompleted = true; // 안전하게 유지
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
            runner?.SetAutoPlay(value, "VNDialogueView UI");
        }

        public void ToggleAuto()
        {
            runner?.ToggleAuto("VNDialogueView UI");
        }

        public void OnSkipButtonClicked()
        {
            // Hold-based skip only. Keep empty to avoid one-shot skip on click.
        }

        public void OnSkipButtonPointerDown()
        {
            if (policy != null && policy.IsDrinkPanelOpen)
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;

            runner?.ForceAutoOff("Skip Hold Button");
            runner?.SetUiSkipHeld(true, "VNDialogueView Skip Hold");
        }

        public void OnSkipButtonPointerUp()
        {
            runner?.SetUiSkipHeld(false, "VNDialogueView Skip Hold");
        }

        public void OnAutoPlayButtonClicked()
        {
            if (Time.unscaledTime < controlActionLockedUntil)
                return;
            ToggleAuto();
        }

        public void OnExitButtonClicked()
        {
            if (policy != null && policy.IsDrinkPanelOpen)
                return;
            if (policy != null && policy.IsSaveLoadModalOpen)
                return;
            if (Time.unscaledTime < controlActionLockedUntil)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                return;

            controlActionLockedUntil = Time.unscaledTime + closeActionLockSeconds;

            closePopupController?.Show();
        }

        public void OpenSaveLoadWindow()
        {
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
