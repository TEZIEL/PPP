using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNClosePopupController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;
        [SerializeField] private VNPolicyController policy; // ✅ 추가

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text messageText;

        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnExit;

        [Header("Text")]
        [TextArea]
        [SerializeField] private string defaultMessage = "Are you sure you want to quit?";

        private void Awake()
        {
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
            if (popupRoot == null) return;

            if (messageText != null) messageText.text = defaultMessage;
            popupRoot.SetActive(true);

            // ✅ 팝업 열림 = 모달 열림
            policy?.SetModalOpen(true);

            // ✅ Space/Enter가 버튼에 먹지 않게(중요)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);


        }

        public void Hide()
        {
            if (popupRoot == null) return;

            popupRoot.SetActive(false);

            // ✅ 모달 닫힘
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