using UnityEngine;

namespace PPP.BLUE.VN
{
    // "VN이 OS에 요구하는 제약"을 한 군데에서 관리하는 컨트롤러
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        [Header("Debug")]
        [SerializeField] private bool lockCloseOnStart = true; // 테스트용


        public bool IsInDrinkMode { get; private set; }
        public bool IsModalOpen { get; private set; }
        public void SetModalOpen(bool on) => IsModalOpen = on;

        private void Awake()
        {
            if (bridge == null)
                bridge = GetComponentInChildren<VNOSBridge>(true);
        }

        private void Start()
        {
            // ✅ 테스트: VN이 시작되면 닫기 막아보기
            if (lockCloseOnStart)
                SetBlockClose(true);
        }

        public void SetBlockClose(bool on)
        {
            if (bridge == null) return;
            bridge.RequestBlockClose(on);
            Debug.Log($"[VNPolicy] BlockClose = {on}");
        }

        // DayStart/DayEnd 등 "절대 나가기 금지" 구간
        public void EnterHardLockSection()
        {
            SetBlockClose(true);
            // 나중에 ExitLocked도 붙일 수 있음
        }

        public void ExitHardLockSection()
        {
            SetBlockClose(false);
        }

        public void EnterDrinkMode()
        {
            IsInDrinkMode = true;
            SetBlockClose(true); // 네 정책대로
        }

        public void ExitDrinkMode()
        {
            IsInDrinkMode = false;
            SetBlockClose(false);
        }
    }
}