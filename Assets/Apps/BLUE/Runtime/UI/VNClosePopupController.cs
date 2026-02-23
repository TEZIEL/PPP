using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNClosePopupController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        [SerializeField] private GameObject popupRoot;  // UI_ClosePopup (전체)
        [SerializeField] private TMP_Text messageText;  // Panel/Text(TMP)

        [SerializeField] private Button btnCancel;      // "아니오"
        [SerializeField] private Button btnExit;        // "예(종료)"

        [Header("Text")]
        [TextArea]
        [SerializeField] private string defaultMessage = "정말 종료하시겠습니까?";

        private void Awake()
        {
            // 시작 시 숨김
            if (popupRoot != null) popupRoot.SetActive(false);

            if (btnCancel != null) btnCancel.onClick.AddListener(Hide);
            if (btnExit != null) btnExit.onClick.AddListener(ForceExit);

            if (bridge != null)
            {
                // OS에서 닫기 눌렀는데 BlockClose라 막혔을 때 VN이 팝업 띄우는 이벤트
                bridge.OnCloseRequested += Show;
            }
        }

        private void OnDestroy()
        {
            if (bridge != null)
            {
                bridge.OnCloseRequested -= Show;
            }
        }

        public void Show()
        {
            if (popupRoot == null) return;

            if (messageText != null) messageText.text = defaultMessage;
            popupRoot.SetActive(true);
        }

        public void Hide()
        {
            if (popupRoot == null) return;
            popupRoot.SetActive(false);
        }

        private void ForceExit()
        {
            // VN이 "진짜 닫아도 된다" 승인하고, OS에게 강제닫기 요청
            if (bridge != null) bridge.RequestForceClose();
            Hide();
        }
    }
}