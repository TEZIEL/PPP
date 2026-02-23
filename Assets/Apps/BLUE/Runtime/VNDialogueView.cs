using TMPro;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNDialogueView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNTextTyper typer;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Typing")]
        [SerializeField] private float charsPerSecond = 40f;

        // 현재 라인의 풀텍스트 (SkipToEnd용)
        private string currentFullText = "";
        private bool lineCompleted = true; // true면 Next로 "다음 라인" 가능
        private int inputLockFrames = 0;

        private void Start()
        {
           
        }

        private void Awake()
        {
            if (typer != null) typer.SetTarget(dialogueText);
        }

        private void OnEnable()
        {
            if (runner != null)
            {
                runner.OnSay += HandleSay;
                runner.OnEnd += HandleEnd;
            }
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.OnSay -= HandleSay;
                runner.OnEnd -= HandleEnd;
            }
        }

        private void Update()
        {
            if (inputLockFrames > 0) { inputLockFrames--; return; }
            if (runner == null) return;
            if (!runner.HasScript) return;

            if (!Input.GetKeyDown(KeyCode.Space)) return;
            Debug.Log($"[INPUT] visibleLen={dialogueText.text.Length} fullLen={currentFullText.Length} isTyping={(typer != null && typer.IsTyping)}");
            // ✅ “지금 화면에 찍힌 텍스트가 풀텍스트와 다르면” = 아직 타이핑 진행 중(또는 미완료)
            bool visibleNotComplete =
                dialogueText != null &&
                currentFullText != null &&
                dialogueText.text != currentFullText;

            if (visibleNotComplete)
            {
                ForceCompleteLine();   // 한 번 누르면 무조건 완성
                return;
            }

            // ✅ 이미 완성이면 바로 다음 줄
            runner.Next();
        }

        private void HandleSay(string speakerId, string text, string lineId)
        {
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