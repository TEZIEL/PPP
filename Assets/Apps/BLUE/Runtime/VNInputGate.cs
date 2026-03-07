namespace PPP.BLUE.VN
{
    public enum VNInputState
    {
        Dialogue,
        Choice,
        Drink,
        Popup,
        Blocked
    }

    public static class VNInputGate
    {
        public static bool CanRouteInput(VNPolicyController policy)
        {
            if (policy == null) return false;
            var st = policy.GetWindowState();
            if (st.IsMinimized) return false;
            if (!st.IsFocused) return false;
            return true;
        }

        public static VNInputState ResolveState(VNPolicyController policy)
        {
            if (!CanRouteInput(policy)) return VNInputState.Blocked;
            if (policy.IsChoiceWaiting) return VNInputState.Choice;
            if (policy.IsDrinkPanelOpen) return VNInputState.Drink;
            if (policy.IsClosePopupOpen || policy.IsModalOpen) return VNInputState.Popup;
            return VNInputState.Dialogue;
        }

        public static bool CanAdvanceDialogue(VNPolicyController policy)
            => ResolveState(policy) == VNInputState.Dialogue;

        public static bool CanUseSkipOrAuto(VNPolicyController policy)
            => ResolveState(policy) == VNInputState.Dialogue;

        public static bool CanSave(VNPolicyController policy)
        {
            if (policy == null) return false;

            // Save 가능 여부는 "입력 포커스"와 분리해서 판단한다.
            // 포커스가 잠시 벗어난 동안 타이핑 완료 콜백이 들어와도 SaveAllowed가
            // 영구적으로 FALSE에 고정되지 않도록, modal/drink/choice만 차단한다.
            if (policy.IsChoiceWaiting) return false;
            if (policy.IsDrinkPanelOpen) return false;
            if (policy.IsClosePopupOpen || policy.IsModalOpen) return false;

            return true;
        }
    }
}

