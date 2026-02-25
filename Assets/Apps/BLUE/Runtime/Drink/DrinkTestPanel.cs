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

            runner?.StopAutoExternal("DrinkPanel Open");

            // ✅ 드링크 패널 자체 모달 토큰
            policy?.PushModal("DrinkPanel");

            policy?.EnterDrinkMode();

            choosing = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private bool choosing;


        private void Choose(string result)
        {
            if (choosing) return;     // ✅ 중복 클릭 방지
            choosing = true;

            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);
            EventSystem.current?.SetSelectedGameObject(null);

            if (runner != null)
            {
                int lastDrinkValue = result == "great" ? 1 : (result == "success" ? 2 : 3);
                runner.SetVar("lastDrink", lastDrinkValue);
            }

            runner?.ApplyDrinkResult(result);

            if (root != null) root.SetActive(false);

            // ✅ 반드시 1:1로 내린다
            policy?.ExitDrinkMode();
            policy?.PopModal("DrinkPanel");

            runner?.Next();
        }
    }
}