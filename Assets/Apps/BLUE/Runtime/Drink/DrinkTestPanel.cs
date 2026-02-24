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

        private bool choosing;


        private void Choose(string result)
        {
            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);
            EventSystem.current?.SetSelectedGameObject(null);

            // ✅ 테스트용: 버튼에 따라 lastDrink도 세팅
            if (runner != null)
            {
                int lastDrinkValue = result == "great" ? 1 : (result == "success" ? 2 : 3);
                runner.SetVar("lastDrink", lastDrinkValue);
            }

            runner?.ApplyDrinkResult(result);

            if (root != null) root.SetActive(false);
            policy?.ExitDrinkMode();

            runner?.Next();
        }
    }
}