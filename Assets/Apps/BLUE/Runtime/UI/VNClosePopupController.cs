using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNClosePopupController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;
        [SerializeField] private VNPolicyController policy;
        [SerializeField] private WindowShortcutController shortcutController;

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text messageText;

        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnExit;

        [Header("Text")]
        [TextArea]
        [SerializeField] private string defaultMessage = "Are you sure you want to quit?";

        private bool isShowing;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);

            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);
            if (policy == null) policy = GetComponentInChildren<VNPolicyController>(true);

            if (popupRoot != null) popupRoot.SetActive(false);

            btnCancel?.onClick.AddListener(Hide);
            btnExit?.onClick.AddListener(ForceExit);

            if (bridge != null)
                bridge.OnCloseRequested += Show;
        }

        private void OnDestroy()
        {
            if (bridge != null)
                bridge.OnCloseRequested -= Show;
        }

        public void Show()
        {
            if (popupRoot == null) return;
            if (isShowing) return;

            isShowing = true;

            if (messageText != null)
                messageText.text = defaultMessage;

            popupRoot.SetActive(true);

            policy?.PushModal("ClosePopup");

            // 🔥 Close 입력 잠금
            shortcutController?.LockForSeconds(0.25f);

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            if (popupRoot == null) return;
            if (!isShowing) return;

            isShowing = false;
            popupRoot.SetActive(false);

            policy?.PopModal("ClosePopup");

            bridge?.ClearCloseRequestPending();

            // 🔥 재입력 방지
            shortcutController?.LockForSeconds(0.15f);

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void OnCloseCancel()
        {
            Hide(); // 이미 내부에서 modal off + clearPending 처리함
        }

        public void OnCloseConfirm()
        {
            bridge?.RequestForceClose(); // OS에 강제 닫기 요청
            Hide();                      // 팝업 닫기
        }

        private void ForceExit()
        {
            bridge?.RequestForceClose();
            Hide();
        }
    }
}