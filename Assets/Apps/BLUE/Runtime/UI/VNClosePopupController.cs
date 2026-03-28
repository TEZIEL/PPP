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
        [SerializeField] private CanvasGroup popupCanvasGroup;
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

            EnsureCanvasGroup();
            SetPopupVisible(false);

            btnCancel?.onClick.AddListener(Hide);
            btnExit?.onClick.AddListener(ForceExit);

            if (bridge != null)
            {
                bridge.OnCloseRequested -= Show; // 중복 방지
                bridge.OnCloseRequested += Show;
            }
        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
            if (!isShowing)
                SetPopupVisible(false);
        }

        private void OnDestroy()
        {
            if (bridge != null)
                bridge.OnCloseRequested -= Show;
        }

        public void Show()
        {
            Debug.Log($"[VNClosePopup] Show requested isShowing={isShowing}");
            if (popupRoot == null) return;
            if (isShowing)
            {
                Debug.Log("[VNClosePopup] Show ignored (already showing)");
                return;
            }

            isShowing = true;

            if (messageText != null)
                messageText.text = defaultMessage;

            SetPopupVisible(true);
            Debug.Log("[UI] ClosePopup show");

            Debug.Log("[VNClosePopup] PushModal reason=ClosePopup");
            policy?.PushModal("ClosePopup");

            shortcutController?.LockForSeconds(0.25f);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Hide()
        {
            Debug.Log($"[VNClosePopup] Hide requested isShowing={isShowing}");
            if (popupRoot == null) return;
            if (!isShowing)
            {
                Debug.Log("[VNClosePopup] Hide ignored (already hidden)");
                return;
            }

            isShowing = false;
            SetPopupVisible(false);
            Debug.Log("[UI] ClosePopup hide");

            Debug.Log("[VNClosePopup] PopModal reason=ClosePopup");
            policy?.PopModal("ClosePopup");

            bridge?.ClearCloseRequestPending();
            shortcutController?.LockForSeconds(0.15f);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void OnCloseCancel()
        {
            Hide();
        }

        public void OnCloseConfirm()
        {
            // 🔥 추가

            bridge?.RequestForceClose();
            Hide();
        }

        private void ForceExit()
        {
             // 🔥 추가

            bridge?.RequestForceClose();
            Hide();
        }

        private void EnsureCanvasGroup()
        {
            if (popupRoot == null)
                return;

            if (popupCanvasGroup == null)
                popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();

            if (popupCanvasGroup == null)
                popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

            if (!popupRoot.activeSelf)
                popupRoot.SetActive(true);
        }

        private void SetPopupVisible(bool visible)
        {
            if (popupCanvasGroup == null)
                return;

            popupCanvasGroup.alpha = visible ? 1f : 0f;
            popupCanvasGroup.interactable = visible;
            popupCanvasGroup.blocksRaycasts = visible;
        }
    }
}
