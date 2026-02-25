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
            subscribed = true;
        }

        private void OnDisable()
        {
            if (!subscribed) return;
            if (runner == null) return;

            runner.OnSay -= HandleSay;
            runner.OnEnd -= HandleEnd;
            runner.OnChoice -= HandleChoice;
            subscribed = false;
        }

        private void HandleChoice(VNNode.BranchRule[] rules)
        {
            if (choicePanel == null)
            {
                Debug.LogError("[VNDialogueView] choicePanel is NULL. Assign it in Inspector.");
                // 패널 없으면 진행 막히니까 최소 안전장치
                runner.Next();
                return;
            }

            choicePanel.Open(rules);
        }

        private void Update()
        {

            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (!runner.HasScript) return;

            if (policy != null && policy.IsModalOpen) return; // ✅ 팝업 떠있으면 VN 진행 금지

            
            // ✅ 0) 입력 자체 허용 여부 (포커스/최소화/팝업 등)
            if (policy != null)
            {
                var st = policy.GetWindowState();
                if (!st.IsFocused || st.IsMinimized) return;
            }

            // ✅ 1) Drink 모드면 진행 입력 금지 (Space가 뭐든 먹지 않게)
            if (policy != null && policy.IsInDrinkMode) return;

            // ✅ 2) Next 입력
            if (!Input.GetKeyDown(KeyCode.Space)) return;

            // ✅ 3) 타이핑 중이면 "완성"만 하고 다음 라인으로 넘어가진 않음
            if (!lineCompleted && typer != null && typer.IsTyping)
            {
                ForceCompleteLine();
                return;
            }

            // ✅ 4) 그 외에는 다음 라인
            runner.CancelAuto();
            runner.Next();
        }


        public void LockInputFrames(int frames = 2)
        {
            inputLockFrames = Mathf.Max(inputLockFrames, frames);
        }

        private void HandleSay(string speakerId, string text, string lineId)
        {
            Debug.Log($"HandleSay called by {gameObject.name}");

            inputLockFrames = 1;

            // 새 라인 시작 = 기본적으로 아직 저장 금지(타이핑 끝나기 전)
            lineCompleted = false;
            currentFullText = text ?? "";

            // seen 기록은 "말풍선에 라인이 표시되기 시작한 순간(타이핑 시작)"에만 수행
            runner?.MarkSeen(lineId);

            if (nameText != null) nameText.text = speakerId ?? "";
            if (dialogueText != null) dialogueText.text = "";

            // ✅ 타이퍼 없으면 즉시 출력 + 즉시 SaveAllowed
            if (typer == null)
            {
                if (dialogueText != null) dialogueText.text = currentFullText;
                lineCompleted = true;
                runner?.MarkSaveAllowed();
                Debug.Log("[VN] SaveAllowed TRUE (No Typer => Immediate)");

                // t.drink 트리거도 동일하게 적용
                if (lineId == "t.drink")
                    drinkTestPanel?.Open();

                return;
            }

            // ✅ 타이핑 시작: 끝날 때만 SaveAllowed TRUE
            typer.StartTyping(currentFullText, onCompleted: () =>
            {
                lineCompleted = true;
                runner?.MarkSaveAllowed();
                runner?.NotifyLineTypedEnd();
                
                Debug.Log("[VN] SaveAllowed TRUE (Typing End)");

                // (선택) 타이핑이 끝난 다음 드링크 패널 띄우고 싶으면 여기로 옮기면 됨
                // if (lineId == "t.drink") drinkTestPanel?.Open();
            });

            // ✅ 지금은 여기서 MarkSaveAllowed() 찍지 않는다!
            // ✅ 드링크 패널을 "라인 표시 순간"에 띄우고 싶으면 아래 유지
            if (lineId == "t.drink")
            {
                if (drinkTestPanel == null)
                    Debug.LogError("[VNDialogueView] drinkTestPanel is NULL. Assign it in Inspector.");
                else
                    drinkTestPanel.Open();
            }
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
