namespace PPP.BLUE.VN
{
    public enum VNInputState
    {
        Dialogue,
        Drink,
        Popup,
        Blocked
    }

    public static class VNInputGate
    {
        // 입력 라우팅 자체가 가능한지 (포커스 + 최소화 체크)
        public static bool CanRouteInput(VNPolicyController policy)
        {
            if (policy == null) return false;

            var st = policy.GetWindowState();

            if (st.IsMinimized) return false;
            if (!st.IsFocused) return false;

            return true;
        }

        // 현재 VN 입력 상태 해석
        public static VNInputState ResolveState(VNPolicyController policy)
        {
            if (policy == null) return VNInputState.Blocked;

            if (!CanRouteInput(policy))
                return VNInputState.Blocked;

            if (policy.IsDrinkPanelOpen)
                return VNInputState.Drink;

            if (policy.IsClosePopupOpen || policy.IsModalOpen)
                return VNInputState.Popup;

            return VNInputState.Dialogue;
        }

        // 일반 대사 진행 가능 여부 (Space / Click)
        public static bool CanAdvanceDialogue(VNPolicyController policy)
        {
            return ResolveState(policy) == VNInputState.Dialogue;
        }

        // Skip / Auto 사용 가능 여부
        public static bool CanUseSkipOrAuto(VNPolicyController policy)
        {
            return ResolveState(policy) == VNInputState.Dialogue;
        }

        // Auto 백그라운드 진행 허용 여부
        // (AutoTimer가 돌아갈 수 있는 상태)
        public static bool CanAutoAdvanceInBackground(VNPolicyController policy)
        {
            if (policy == null) return false;

            var st = policy.GetWindowState();

            // 최소화면 Auto 정지
            if (st.IsMinimized) return false;

            // VN 상태 차단
            if (policy.IsDrinkPanelOpen) return false;
            if (policy.IsClosePopupOpen || policy.IsModalOpen) return false;

            return true;
        }

        // Save 가능 여부
        public static bool CanSave(VNPolicyController policy)
        {
            if (policy == null) return false;

            // Save는 포커스와 분리
            // 타이핑 완료 콜백 때문에 포커스 기반 차단은 하지 않음

            if (policy.IsDrinkPanelOpen) return false;
            if (policy.IsClosePopupOpen || policy.IsModalOpen) return false;

            return true;
        }
    }
}