using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNPolicyController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private VNOSBridge bridge;

        public VNWindowState GetWindowState()
        {
            return bridge != null ? bridge.GetWindowState() : new VNWindowState(false, true);
        }

        public bool IsInDrinkMode { get; private set; }

        // ✅ 팝업 떠 있으면 VN 입력 막기용
        public bool IsModalOpen { get; private set; }

        private void Awake()
        {
            // ✅ 가장 안전: inspector에 넣는 게 1순위
            // ✅ 그래도 빠지면 "자식 → 부모" 순으로 찾아서 반드시 잡는다
            if (bridge == null) bridge = GetComponentInChildren<VNOSBridge>(true);
            if (bridge == null) bridge = GetComponentInParent<VNOSBridge>(true);

            if (bridge == null)
                Debug.LogError("[VNPolicy] VNOSBridge not found. Assign it in Inspector.");
        }

        private void Start()
        {
            // ✅ VN은 기본적으로 항상 닫기 팝업을 거치게 한다
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

            // ✅ 여기서도 "다시 한번" 보정해줘 (안전장치)
            // 드링크가 끝난 뒤 팝업이 안 뜨는 현상은 대부분 여기서 잡힘
            bridge?.RequestBlockClose(true);

            Debug.Log("[VNPolicy] ExitDrinkMode (re-assert BlockClose=True)");
        }

        // VNClosePopupController가 Show/Hide에서 호출
        public void SetModalOpen(bool on)
        {
            IsModalOpen = on;
            Debug.Log($"[VNPolicy] IsModalOpen = {IsModalOpen}");
        }
    }
}