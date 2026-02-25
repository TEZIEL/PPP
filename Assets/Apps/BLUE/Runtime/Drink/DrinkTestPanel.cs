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

        private Coroutine openCo;
        private bool isOpen;        // root active 기준으로 잡아도 됨
        private bool isOpening;

        public bool IsOpenOrOpening => isOpen || isOpening;

        
        private int openDelayFrames = 0;
        private bool waitOneFrameAfterModal;

        private void Awake()
        {
            if (root != null) root.SetActive(false);

            if (btnFail != null) btnFail.onClick.AddListener(() => Choose("fail"));
            if (btnSuccess != null) btnSuccess.onClick.AddListener(() => Choose("success"));
            if (btnGreat != null) btnGreat.onClick.AddListener(() => Choose("great"));
        }

        public void Open(string orderId)
        {
            // ✅ 이미 열려있거나 열리는 중이면 무시
            if (IsOpenOrOpening)
            {
                Debug.Log("[DrinkPanel] Open ignored (already open/opening)");
                return;
            }

            runner?.StopAutoExternal("DrinkPanel Open:" + (orderId ?? string.Empty));

            // 혹시 남아있는 코루틴이 있으면 정리
            if (openCo != null) StopCoroutine(openCo);

            openCo = StartCoroutine(CoOpenSafe());
        }


        private void OnDisable()
        {
            if (isOpen)
            {
                policy?.ExitDrinkMode();
                policy?.PopModal("DrinkPanel");
                isOpen = false;
            }
            isOpening = false;
            if (openCo != null) { StopCoroutine(openCo); openCo = null; }
            if (root != null) root.SetActive(false);
        }

        private IEnumerator CoOpenSafe()
        {
            isOpening = true;

            // 다른 modal 완전 종료까지 대기
            while (policy != null && policy.IsModalOpen)
                yield return null;

            if (isOpen) { isOpening = false; openCo = null; yield break; }

            if (root != null) root.SetActive(true);
            isOpen = true;

            policy?.PushModal("DrinkPanel");
            policy?.EnterDrinkMode();

            choosing = false;
            EventSystem.current?.SetSelectedGameObject(null);

            isOpening = false;
            openCo = null;
        }


        private bool choosing;


        private void Choose(string result)
        {
            if (choosing) return;
            choosing = true;

            windowManager?.LockCloseForSeconds(0.15f);
            shortcutController?.LockForSeconds(0.15f);
            EventSystem.current?.SetSelectedGameObject(null);

            StartCoroutine(CoClose(result));
        }

        private IEnumerator CoClose(string result)
        {
            if (root != null) root.SetActive(false);

            // 🔑 여기서 즉시 false 내리지 말고 대기
            yield return null;

            isOpen = false;

            policy?.ExitDrinkMode();
            policy?.PopModal("DrinkPanel");

            runner?.ForceAutoOff("Drink Finished");
            runner?.ReturnFromCall(result);
        }
    }






}
