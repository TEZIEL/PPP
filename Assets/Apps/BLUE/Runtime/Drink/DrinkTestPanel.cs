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
        }

        private void Choose(string result)
        {
            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);
            EventSystem.current?.SetSelectedGameObject(null);

            // ✅ 0) 전역 단축키 잠깐 잠그기 (버튼 클릭 직후 이상한 키 입력 방지)
            shortcutController?.LockForSeconds(0.1f);

            // ✅ 0-1) OS 닫기(마우스)도 잠깐 락
            // (윈도우매니저 참조가 없으면 shortcutController 통해서 전달하거나, 여기서 WindowManager를 직접 물려도 됨)
            windowManager?.LockCloseForSeconds(0.15f);

            // ✅ 1) UI 선택 해제 (Space/Enter가 버튼에 다시 먹는 문제 방지)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

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