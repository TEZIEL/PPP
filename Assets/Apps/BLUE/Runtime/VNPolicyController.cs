using UnityEngine;
using System;

namespace PPP.BLUE.VN
{
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        public VNWindowState GetWindowState()
            => bridge != null ? bridge.GetWindowState() : new VNWindowState(false, true);

        public bool IsInDrinkMode { get; private set; }

        // ✅ 더이상 bool 직접 토글 금지. refCount로 관리.
        private int modalCount;
        public bool IsModalOpen => modalCount > 0;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);

            if (bridge == null)
                Debug.LogError("[VNPolicy] VNOSBridge not found. Assign it in Inspector.");
        }

        private void Start()
        {
            bridge?.RequestBlockClose(true);
            Debug.Log("[VNPolicy] BlockClose = True (default)");
        }

        public void EnterDrinkMode()
        {
            IsInDrinkMode = true;

            // ✅ 드링크는 "모달 1개"를 점유
            PushModal("DrinkMode");

            Debug.Log("[VNPolicy] EnterDrinkMode");
        }

        public void ExitDrinkMode()
        {
            IsInDrinkMode = false;

            PopModal("DrinkMode");

            bridge?.RequestBlockClose(true);
            Debug.Log("[VNPolicy] ExitDrinkMode (re-assert BlockClose=True)");
        }

        // ----------------------------
        // ✅ 새 API: Push / Pop
        // ----------------------------
        public void PushModal(string reason)
        {
            modalCount++;
            Debug.Log($"[VNPolicy] Modal++ ({reason}) count={modalCount}");
        }

        public void PopModal(string reason)
        {
            modalCount = Mathf.Max(0, modalCount - 1);
            Debug.Log($"[VNPolicy] Modal-- ({reason}) count={modalCount}");
        }

        // ----------------------------
        // ✅ 호환용 API (기존 호출 유지 가능)
        // ----------------------------
        public void SetModalOpen(bool on)
        {
            if (on) PushModal("SetModalOpen(true)");
            else PopModal("SetModalOpen(false)");
        }
    }
}