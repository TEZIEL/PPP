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

        [Header("Typing")]
        [SerializeField] private float charsPerSecond = 40f;

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
            // 1) 팝업/모달이면 입력 금지 (이미 위에서 막고 있지만 안전)
            if (policy != null && policy.IsModalOpen) return false;

            // 2) 브릿지가 있으면 “포커스/최소화”로 컷
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
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")} choicePanel={(choicePanel ? choicePanel.name : "NULL")}");
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
                Debug.LogError("[VNDialogueView] choicePanel is NULL. Assign it in Inspector.");
                runner.Next(); // 패널 없으면 안전하게 진행
                return;
            }

            choicePanel.Open(choices);
        }

        private void HandleCall(string callTarget, string callArg)
        {
            if (!string.Equals(callTarget, "Drink", System.StringComparison.OrdinalIgnoreCase))
                return;

            drinkTestPanel?.Open(callArg);
        }

        private void Update()
        {
            
            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (!runner.HasScript) return;

            // ✅ 모달/드링크 중에는 진행 입력 금지
            if (policy != null && (policy.IsModalOpen || policy.IsInDrinkMode))
                return;

            // ✅ 최소화 상태면 진행 입력 금지 (포커스는 "무시"한다)
            if (policy != null)
            {
                var st = policy.GetWindowState();
                if (st.IsMinimized) return;
            }

            // ✅ Next 입력
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            // ✅ 유저 입력이면 무조건 Auto OFF (타이핑완료/Next 둘 다 포함)
            runner.ForceAutoOff("User input (Space)");

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

        private void HandleEnd()
        {
            // 엔드 처리 (지금은 비워도 됨)
        }
    }
}
