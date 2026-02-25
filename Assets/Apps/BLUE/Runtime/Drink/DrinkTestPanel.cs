using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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

        private bool pendingOpen;
        public bool IsPendingOpen => pendingOpen;
        private int openDelayFrames = 0;
        private bool waitOneFrameAfterModal;

        private void Awake()
        {
            if (root != null) root.SetActive(false);

            if (btnFail != null) btnFail.onClick.AddListener(() => Choose("fail"));
            if (btnSuccess != null) btnSuccess.onClick.AddListener(() => Choose("success"));
            if (btnGreat != null) btnGreat.onClick.AddListener(() => Choose("great"));
        }

        public void Open()
        {
            runner?.StopAutoExternal("DrinkPanel Open");
            StartCoroutine(CoOpenSafe());
        }

        private IEnumerator CoOpenSafe()
        {
            // 다른 modal 완전 종료까지 대기
            while (policy != null && policy.IsModalOpen)
                yield return null;

            if (root != null)
                root.SetActive(true);

            policy?.PushModal("DrinkPanel");
            policy?.EnterDrinkMode();

            choosing = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private bool choosing;

        private void Update()
        {
            if (!pendingOpen) return;

            // 다른 모달 있을 때만 대기
            if (policy != null && policy.IsModalOpen)
                return;

            pendingOpen = false;

            if (root != null) root.SetActive(true);

            runner?.StopAutoExternal("DrinkPanel Open");

            // ✅ 여기서 modal 잡아야 함
            policy?.PushModal("DrinkPanel");
            policy?.EnterDrinkMode();

            choosing = false;
        }

        private void Choose(string result)
        {
            if (choosing) return;
            choosing = true;

            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);
            EventSystem.current?.SetSelectedGameObject(null);

            if (runner != null)
            {
                int lastDrinkValue = result == "great" ? 1 : (result == "success" ? 2 : 3);
                runner.SetVar("lastDrink", lastDrinkValue);   // ✅ 분기 변수 세팅
                                                              // runner.ApplyDrinkResult(result);            // ❌ 지금은 빼도 됨(중복 방지)
            }

            if (root != null) root.SetActive(false);

            policy?.ExitDrinkMode();
            policy?.PopModal("DrinkPanel");

            runner?.Next();
        }
    }
}