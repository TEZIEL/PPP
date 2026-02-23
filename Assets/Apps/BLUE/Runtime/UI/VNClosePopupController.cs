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

        private void Awake()
        {
            // ✅ 자동 주입 (인스펙터 누락 방지)
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (policy == null) policy = GetComponentInChildren<VNPolicyController>(true);

            if (popupRoot != null) popupRoot.SetActive(false);

            if (btnCancel != null) btnCancel.onClick.AddListener(Hide);
            if (btnExit != null) btnExit.onClick.AddListener(ForceExit);

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
            // ✅ 드링크 모드면 팝업 자체 금지(2중 방어)
            if (policy != null && policy.IsInDrinkMode) return;

            if (popupRoot == null) return;

            if (messageText != null) messageText.text = defaultMessage;
            popupRoot.SetActive(true);

            policy?.SetModalOpen(true);

            // ✅ Space/Enter 버튼 재클릭 방지
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            if (popupRoot == null) return;

            popupRoot.SetActive(false);
            policy?.SetModalOpen(false);

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ForceExit()
        {
            bridge?.RequestForceClose();
            Hide();
        }
    }
}