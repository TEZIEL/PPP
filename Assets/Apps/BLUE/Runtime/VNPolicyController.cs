using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        public bool IsInDrinkMode { get; private set; }

        private void Awake()
        {
            if (bridge == null)
                bridge = GetComponentInChildren<VNOSBridge>(true);
        }

        public bool IsModalOpen { get; private set; }

        public void SetModalOpen(bool open)
        {
            IsModalOpen = open;
            Debug.Log($"[VNPolicy] ModalOpen = {open}");
        }

        private void OnEnable()
        {
            // ✅ 언제 켜져도 기본은 팝업 경유
            bridge?.RequestBlockClose(true);
        }

        private void Start()
        {
            bridge?.RequestBlockClose(true);
            Debug.Log("[VNPolicy] BlockClose = True (default)");
        }

        public void EnterDrinkMode()
        {
            IsInDrinkMode = true;
            Debug.Log("[VNPolicy] EnterDrinkMode");
        }

        public void ExitDrinkMode()
        {
            IsInDrinkMode = false;
            Debug.Log("[VNPolicy] ExitDrinkMode");
        }

        public void AllowDirectClose(bool allow)
        {
            bridge?.RequestBlockClose(!allow);
            Debug.Log($"[VNPolicy] BlockClose = {!allow}");
        }
    }
}