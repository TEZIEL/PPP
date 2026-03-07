using TMPro;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNDialogueView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNTextTyper typer;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private VNChoicePanel choicePanel;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private VNOSBridge osBridge;
        [SerializeField] private VNOSBridge bridge;
        [SerializeField] private DrinkTestPanel drinkTestPanel;
        [SerializeField] private RectTransform advanceClickArea;
        [SerializeField] private Camera uiCamera;

        [Header("Typing")]
        [SerializeField] private float charsPerSecond = 40f;

        [Header("Skip Preview")]
        [SerializeField, Range(0.05f, 1f)] private float skipPreviewRatio = 0.2f;
        [SerializeField] private int skipPreviewMinChars = 2;

        // 현재 라인의 풀텍스트 (SkipToEnd용)
        private string currentFullText = "";
        private bool lineCompleted = true; // true면 Next로 "다음 라인" 가능
        private int inputLockFrames = 0;
        private bool subscribed;

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
            if (typer != null) typer.SetTarget(dialogueText);
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            choicePanel = GetComponentInChildren<VNChoicePanel>(true); // 같은 윈도우 트리에서 찾기
            if (advanceClickArea == null && dialogueText != null)
                advanceClickArea = dialogueText.rectTransform;
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")} choicePanel={(choicePanel ? choicePanel.name : "NULL")}");
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

        private void OnEnable()
        {
            LockInputFrames(5); // 여기 추가 (2 말고 5 추천)

            if (subscribed) return;
            if (runner == null) return;

            runner.OnSay += HandleSay;
            runner.OnEnd += HandleEnd;
            runner.OnChoice += HandleChoice;
            runner.OnCall += HandleCall;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            if (runner == null) return;

            runner.OnSay -= HandleSay;
            runner.OnEnd -= HandleEnd;
            runner.OnChoice -= HandleChoice;
            runner.OnCall -= HandleCall;
            subscribed = false;
        }

        private void HandleChoice(VNNode.ChoiceOption[] choices)
        {
            Debug.Log($"[VN_UI] HandleChoice choices={choices?.Length ?? -1}");

            if (choicePanel == null)
            {
                Debug.LogError("[VN] ChoicePanel missing — cannot present choices");
                return;
            }

            choicePanel.Open(choices);
        }

        private void HandleCall(string callTarget, string callArg)
        {
            if (!string.Equals(callTarget, "Drink", System.StringComparison.OrdinalIgnoreCase))
                return;

            bool fromRestore = runner != null && runner.IsDispatchingRestoredCall;
            if (fromRestore)
                Debug.Log($"[VN] Drink restore requested target={callTarget} arg={callArg}");

            drinkTestPanel?.Open(callArg);
        }

        private void Update()
        {
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

            if (clicked && !IsPointerInsideAdvanceArea())
                return;

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

          


        public void LockInputFrames(int frames = 2)
        {
            inputLockFrames = Mathf.Max(inputLockFrames, frames);
        }

        private void HandleSay(string speakerId, string text, string lineId)
        {
            Debug.Log($"HandleSay called by {gameObject.name}");
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

            // ✅ 타이핑 시작
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

            // 일반 스킵은 "전체 문장 강제 출력" 대신 "부분 프리뷰 후 다음 줄" UX를 사용한다.
            // (한 줄씩 훑어 지나가되, 근거 없이 통째로 점프하지 않도록 유지)
            typer.CompleteWithPreview(skipPreviewRatio, skipPreviewMinChars, "...");
            lineCompleted = true;
            return true;
        }

        private void HandleEnd()
        {
            // 엔드 처리 (지금은 비워도 됨)
        }
    }
}
