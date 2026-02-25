using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        public VNWindowState GetWindowState()
            => bridge != null ? bridge.GetWindowState() : new VNWindowState(false, true);

        public bool IsInDrinkMode { get; private set; }

        private int modalCount;
        public bool IsModalOpen => modalCount > 0;

        // ✅ 정책: “닫기 차단”은 여기서 계산한다
        // 드링크 중에만 막고 싶으면 IsInDrinkMode만
        public bool ShouldBlockClose => IsInDrinkMode;
        // 더 안전하게 가려면 아래로 교체:
        // public bool ShouldBlockClose => IsInDrinkMode || IsModalOpen;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);

            if (bridge == null)
                Debug.LogError("[VNPolicy] VNOSBridge not found. Assign it in Inspector.");
        }

        private void Start()
        {
            // ✅ 기본은 닫기 허용(=BlockClose false)
            SyncBlockClose("Start");
        }

        private void SyncBlockClose(string reason)
        {
            bridge?.RequestBlockClose(ShouldBlockClose);
            Debug.Log($"[VNPolicy] SyncBlockClose({reason}) => {ShouldBlockClose}");
        }

        public void EnterDrinkMode()
        {
            IsInDrinkMode = true;
            PushModal("DrinkMode"); // 네 설계대로: 드링크는 모달 점유
            Debug.Log("[VNPolicy] EnterDrinkMode");

            SyncBlockClose("EnterDrinkMode");
        }

        public void ExitDrinkMode()
        {
            IsInDrinkMode = false;
            PopModal("DrinkMode");
            Debug.Log("[VNPolicy] ExitDrinkMode");

            SyncBlockClose("ExitDrinkMode");
        }

        public void PushModal(string reason)
        {
            modalCount++;
            Debug.Log($"[VNPolicy] Modal++ ({reason}) count={modalCount}");
            // (선택) 모달도 닫기 차단에 포함시키는 정책이면 여기서 Sync
            // SyncBlockClose("PushModal");
        }

        public void PopModal(string reason)
        {
            modalCount = Mathf.Max(0, modalCount - 1);
            Debug.Log($"[VNPolicy] Modal-- ({reason}) count={modalCount}");
            // (선택) 모달도 닫기 차단에 포함시키는 정책이면 여기서 Sync
            // SyncBlockClose("PopModal");
        }

        public void SetModalOpen(bool on)
        {
            if (on) PushModal("SetModalOpen(true)");
            else PopModal("SetModalOpen(false)");
        }
    }
}