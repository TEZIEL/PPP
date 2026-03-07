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
            => ResolveState(policy) == VNInputState.Dialogue;
    }
}

