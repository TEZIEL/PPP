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
            Debug.Log("[VNClosePopup] Show()");
            if (popupRoot != null && popupRoot.activeSelf) return;
            if (policy != null && policy.IsModalOpen) return;


            if (policy != null && policy.IsInDrinkMode) return;
            if (popupRoot == null) return;

            if (isShowing) return;          // ✅ 중복 방지
            isShowing = true;

            if (messageText != null) messageText.text = defaultMessage;
            popupRoot.SetActive(true);

            policy?.SetModalOpen(true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            if (popupRoot == null) return;

            if (!isShowing) return;
            isShowing = false;

            popupRoot.SetActive(false);
            policy?.SetModalOpen(false);
            bridge?.ClearCloseRequestPending(); // ✅ 여기
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ForceExit()
        {
            bridge?.RequestForceClose();
            Hide();
        }
    }
}