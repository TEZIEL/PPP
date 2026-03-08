using UnityEngine;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        private readonly Dictionary<string, int> modalReasonCounts = new();

        public VNWindowState GetWindowState()
            => bridge != null ? bridge.GetWindowState() : new VNWindowState(false, true);

        public bool IsInDrinkMode { get; private set; }

        private int modalCount;
        public bool IsModalOpen => modalCount > 0;

        public bool IsClosePopupOpen => IsModalReasonOpen("ClosePopup");
        public bool IsDrinkPanelOpen => IsModalReasonOpen("DrinkPanel") || IsInDrinkMode;

        // ✅ VN 앱은 평상시에도 OS 직접 종료를 막고, 항상 ClosePopup 경유로 닫는다.
        public bool ShouldBlockClose => true;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);

            if (bridge == null)
                Debug.LogError("[VNPolicy] VNOSBridge not found. Assign it in Inspector.");
        }

        private void Start()
        {
            RefreshClosePolicy("Start");
        }

        private void RefreshClosePolicy(string reason)
        {
            var shouldBlock = ShouldBlockClose;
            bridge?.RequestBlockClose(shouldBlock);
            Debug.Log($"[VNPolicy] RefreshClosePolicy({reason}) shouldBlock={shouldBlock} drink={IsInDrinkMode} blockingModal={IsBlockingModalState()}");
        }

        public void EnterDrinkMode()
        {
            IsInDrinkMode = true;
            PushModal("DrinkMode"); // 네 설계대로: 드링크는 모달 점유
            Debug.Log("[VNPolicy] EnterDrinkMode");

            RefreshClosePolicy("EnterDrinkMode");
        }

        public void ExitDrinkMode()
        {
            IsInDrinkMode = false;
            PopModal("DrinkMode");
            Debug.Log("[VNPolicy] ExitDrinkMode");

            RefreshClosePolicy("ExitDrinkMode");
        }

        public void PushModal(string reason)
        {
            modalCount++;
            ChangeReasonCount(reason, +1);
            Debug.Log($"[VNPolicy] Modal++ ({reason}) count={modalCount}");
            RefreshClosePolicy("PushModal");
        }

        public void PopModal(string reason)
        {
            modalCount = Mathf.Max(0, modalCount - 1);
            ChangeReasonCount(reason, -1);
            Debug.Log($"[VNPolicy] Modal-- ({reason}) count={modalCount}");
            RefreshClosePolicy("PopModal");
        }

        public void SetModalOpen(bool on)
        {
            Debug.Log($"[VNPolicy] SetModalOpen({on})");
            if (on) PushModal("SetModalOpen(true)");
            else PopModal("SetModalOpen(false)");
        }

        public bool IsModalReasonOpen(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            return modalReasonCounts.TryGetValue(reason, out var c) && c > 0;
        }

        public bool IsBlockingModalState()
        {
            return IsClosePopupOpen || IsDrinkPanelOpen || IsModalOpen;
        }

        public bool CanAcceptVNInput()
        {
            return VNInputGate.CanRouteInput(this);
        }

        public bool CanAutoAdvance(bool saveAllowed)
        {
            if (!saveAllowed) return false;
            return VNInputGate.CanAutoAdvanceInBackground(this);
        }

        public bool CanToggleAuto()
        {
            return VNInputGate.CanUseSkipOrAuto(this);
        }

        public bool CanSaveDialogueState()
        {
            return VNInputGate.CanSave(this);
        }

        public bool CanRequestClose()
        {
            // ClosePopup이 열린 상태에서만 재요청 차단
            return !IsClosePopupOpen;
        }

        private void ChangeReasonCount(string reason, int delta)
        {
            if (string.IsNullOrEmpty(reason) || delta == 0) return;

            modalReasonCounts.TryGetValue(reason, out var current);
            var next = Mathf.Max(0, current + delta);

            if (next == 0) modalReasonCounts.Remove(reason);
            else modalReasonCounts[reason] = next;
        }
    }
}
