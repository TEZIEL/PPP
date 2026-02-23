using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PPP.BLUE.VN
{
    public sealed class DrinkTestPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject root; // 패널 전체
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private WindowShortcutController shortcutController;
        [SerializeField] private WindowManager windowManager;

        [Header("Buttons")]
        [SerializeField] private Button btnFail;
        [SerializeField] private Button btnSuccess;
        [SerializeField] private Button btnGreat;

        private void Awake()
        {
            if (root != null) root.SetActive(false);

            if (btnFail != null) btnFail.onClick.AddListener(() => Choose("fail"));
            if (btnSuccess != null) btnSuccess.onClick.AddListener(() => Choose("success"));
            if (btnGreat != null) btnGreat.onClick.AddListener(() => Choose("great"));
        }

        public void Open()
        {
            if (root != null) root.SetActive(true);
            policy?.EnterDrinkMode();

            // (선택) 드링크 패널 열릴 때도 선택 해제해두면 안전
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Choose(string result)
        {
            // 0) 입력/닫기 잠금 (버튼 클릭 직후 입력 잔상 방지)
            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);

            // 1) UI 선택 해제 (Space/Enter가 버튼에 다시 먹는 문제 방지)
            EventSystem.current?.SetSelectedGameObject(null);

            // 2) 결과 반영
            runner?.ApplyDrinkResult(result);

            // 3) 닫기 + 모드 종료
            if (root != null) root.SetActive(false);
            policy?.ExitDrinkMode();

            // 4) VN 계속 진행
            runner?.Next();
        }
    }
}