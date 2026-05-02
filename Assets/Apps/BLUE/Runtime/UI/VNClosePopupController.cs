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
        [SerializeField] private string returnToTitleMessage = "타이틀 화면으로 돌아가시겠습니까?";

        private bool isShowing;
        private System.Action confirmAction;
        private System.Action cancelAction;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);

            if (policy == null) policy = GetComponentInParent<VNPolicyController>(true);
            if (policy == null) policy = GetComponentInChildren<VNPolicyController>(true);

            EnsureCanvasGroup();
            SetPopupVisible(false);

            btnCancel?.onClick.AddListener(OnCloseCancel);
            btnExit?.onClick.AddListener(ForceExit);

        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
            if (!isShowing)
                SetPopupVisible(false);
        }

        private void OnDestroy()
        {
        }

        public void Show()
        {
            ShowExitConfirm();
        }

        public void ShowExitConfirm()
        {
            ShowExitConfirm(() => bridge?.RequestForceClose(), null);
        }

        public void ShowExitConfirm(System.Action onConfirm, System.Action onCancel)
        {
            cancelAction = onCancel;
            ShowPopup(defaultMessage, onConfirm);
        }

        public void ShowReturnToTitleConfirm(System.Action onConfirm)
        {
            ShowPopup(returnToTitleMessage, onConfirm);
        }

        private void ShowPopup(string message, System.Action onConfirm)
        {
            Debug.Log($"[VNClosePopup] Show requested isShowing={isShowing}");
            if (popupRoot == null) return;
            if (isShowing)
            {
                Debug.Log("[VNClosePopup] Show ignored (already showing)");
                return;
            }

            isShowing = true;
            confirmAction = onConfirm;
            cancelAction ??= null;

            if (messageText != null)
                messageText.text = message;

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
            cancelAction = null;
            confirmAction = null;
            shortcutController?.LockForSeconds(0.15f);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void OnCloseCancel()
        {
            cancelAction?.Invoke();
            Hide();
        }

        public void OnCloseConfirm()
        {
            ForceExit();
        }

        public void RequestCloseFromPopup()
        {
            Debug.Log($"[TITLE] bridge.RequestCloseFromUI called bridge={(bridge != null)}");
            bridge?.RequestCloseFromUI();
        }

        private void ForceExit()
        {
            var action = confirmAction;
            Hide();
            action?.Invoke();
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
