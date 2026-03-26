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
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")}");
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

            if (subscribed) return;
            if (runner == null) return;

            runner.OnSay += HandleSay;
            runner.OnEnd += HandleEnd;
            subscribed = true;
        }

        private void OnDisable()
        {
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

            if (!CanAcceptVNInput()) return;
            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (!runner.HasScript) return;

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

        private void HandleControlButtonState()
        {
            bool isDrinkMode = policy != null && policy.IsDrinkPanelOpen;
            bool skipAutoInteractable = !isDrinkMode && (policy == null || VNInputGate.CanUseSkipOrAuto(policy));
            bool exitInteractable = !isDrinkMode;

            SetButtonInteractable(skipButton, skipAutoInteractable, ref lastSkipButtonInteractable);
            SetButtonInteractable(autoPlayButton, skipAutoInteractable, ref lastAutoButtonInteractable);
            SetButtonInteractable(exitButton, exitInteractable, ref lastExitButtonInteractable);
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

            if (runner != null && runner.IsSkipMode)
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
            if (policy != null && policy.IsDrinkPanelOpen)
                return;

            runner?.RequestSkipStep("VNDialogueView Skip Button");
        }

        public void OnAutoPlayButtonClicked()
        {
            ToggleAuto();
        }

        public void OnExitButtonClicked()
        {
            if (policy != null && policy.IsDrinkPanelOpen)
                return;

            closePopupController?.Show();
        }

        private void HandleEnd()
        {
            // 엔드 처리 (지금은 비워도 됨)
        }
    }
}
