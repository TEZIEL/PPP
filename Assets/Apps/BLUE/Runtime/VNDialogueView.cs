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
           
        }

        private bool CanAcceptVNInput()
        {
            // 1) 팝업 떠있으면 VN 진행 입력 막기
            // (너 ClosePopup이 active면)
            // popupRoot 참조 없으면, 일단 아래 줄은 나중에 연결해도 됨
            // if (closePopupRoot != null && closePopupRoot.activeSelf) return false;

            // 2) 타이핑 처리/모드 처리는 Update에서 이미 하니까 여기선 최소만
            return true;
        }

        private void Awake()
        {
            if (typer != null) typer.SetTarget(dialogueText);
            if (runner == null) runner = GetComponentInParent<VNRunner>(true);
            choicePanel = GetComponentInChildren<VNChoicePanel>(true); // 같은 윈도우 트리에서 찾기
            Debug.Log($"[VN_UI] bind runner={(runner ? runner.name : "NULL")} choicePanel={(choicePanel ? choicePanel.name : "NULL")}");
        }

        private void OnEnable()
        {

            if (runner != null) runner.OnChoice += HandleChoice;
            if (subscribed) return;
            runner.OnSay += HandleSay;
            runner.OnEnd += HandleEnd;
            runner.OnChoice += HandleChoice;   // ✅ 추가
            subscribed = true;
        }

        private void OnDisable()
        {
            if (runner != null) runner.OnChoice -= HandleChoice;
            if (!subscribed) return;
            runner.OnSay -= HandleSay;
            runner.OnEnd -= HandleEnd;
            runner.OnChoice -= HandleChoice;   // ✅ 추가
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
            if (!CanAcceptVNInput()) return;

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
            runner.Next();
        }


        private void HandleSay(string speakerId, string text, string lineId)
        {
            Debug.Log($"HandleSay called by {gameObject.name}");
            
            inputLockFrames = 1;
            // 새 라인이 오면, 일단 "진행 금지" 상태로 만들고 타이핑 시작
            lineCompleted = false;
            currentFullText = text ?? "";

            if (nameText != null) nameText.text = speakerId ?? "";
            if (dialogueText != null) dialogueText.text = "";

            if (typer == null)
            {
                // 타이퍼가 없으면 즉시 출력
                if (dialogueText != null) dialogueText.text = currentFullText;
                lineCompleted = true;
                return;
            }

            // 타이핑 속도 적용
            // (typer 내부 변수를 public set으로 바꾸거나, 여기서 직접 값을 넘기는 방식 중 택1)
            // 간단히: inspector에서 charsPerSecond 맞춰두고 사용

            typer.StartTyping(currentFullText, onCompleted: () =>
            {
                lineCompleted = true;
                runner?.MarkSaveAllowed();
            });

            runner.MarkSaveAllowed();
            Debug.Log("[VN] SaveAllowed TRUE (Typing End)");

            // 임시: 특정 라인에서 드링크 패널 열기
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