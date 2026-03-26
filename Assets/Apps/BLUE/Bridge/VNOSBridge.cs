using UnityEngine;
using System;

namespace PPP.BLUE.VN
{


    public sealed class VNOSBridge : MonoBehaviour, IVNCloseRequestHandler
    {
        [Header("Identity")]
        [SerializeField] private string appId = "app.vn"; // ✅ AppDefinition.AppId와 반드시 동일하게!

        [Header("OS Host (adapter)")]
        [SerializeField] private MonoBehaviour hostBehaviour; // IVNHostOS 구현체
        [SerializeField] private VNRunner runner;
        [SerializeField] private VNPolicyController policy;

        private IVNHostOS Host => hostBehaviour as IVNHostOS;

        // VN이 설정하는 정책
        public bool ExitLocked { get; private set; }
        public bool BlockClose { get; private set; } = true;
        private bool closeRequestPending;
        private bool closeRequestDeferred;

        private bool registered; // ✅ 중복 등록 방지
        private bool allowCloseOnce;

        /// <summary>
        /// OS가 ContentPrefab instantiate 직후 호출해서 Host를 주입한다.
        /// </summary>
        public void InjectHost(IVNHostOS host, string injectedAppId = null)
        {
            hostBehaviour = host as MonoBehaviour;

            if (!string.IsNullOrEmpty(injectedAppId))
                appId = injectedAppId;

            Debug.Log($"[VNBridge] InjectHost host={(hostBehaviour ? hostBehaviour.name : "NULL")} appId={appId}");
            Debug.Log($"[VNBridge] InjectHost bridgeId={GetInstanceID()} runnerId={(runner != null ? runner.GetInstanceID().ToString() : "NULL")}");

            TryRegisterCloseHandler();
        }

        private void OnEnable()
        {
            Debug.Log($"[VNBridge] OnEnable id={GetInstanceID()} obj={name} (before BlockClose={BlockClose})");

            RequestBlockClose(true); // VN은 기본적으로 닫기 차단
            TryRegisterCloseHandler();
        }

        private void Awake()
        {
            // 인스펙터로도 꽂을 수 있지만, 정석은 OS가 InjectHost로 주입하는 것.
            RequestBlockClose(true);
            TryRegisterCloseHandler();
            if (runner == null) runner = GetComponentInChildren<VNRunner>(true);
            if (policy == null) policy = GetComponentInChildren<VNPolicyController>(true);
            Debug.Log($"[VNBridge] Awake bridgeId={GetInstanceID()} runnerId={(runner != null ? runner.GetInstanceID().ToString() : "NULL")} appId={appId}");
        }

        private void OnDisable()
        {
            if (Host != null)
                Host.ClearCloseHandler(appId, this);

            registered = false; // 다시 켜질 때 재등록 가능
        }

        private void OnDestroy()
        {
            if (Host != null)
                Host.ClearCloseHandler(appId, this);

            registered = false;
        }

        private void Unregister()
        {
            if (!registered) return;
            if (Host == null) return;

            Host.ClearCloseHandler(appId, this);
            registered = false;
        }


        private void TryRegisterCloseHandler()
        {
            if (registered) return;
            if (Host == null)
            {
                Debug.LogWarning($"[VNBridge] Host is NULL (hostBehaviour={(hostBehaviour ? hostBehaviour.GetType().Name : "NULL")})");
                return;
            }

            Host.SetCloseHandler(appId, this);
            registered = true;

            Host.SetExitLocked(appId, ExitLocked);
            Debug.Log($"[VNBridge] Registered close handler appId={appId}");
        }

        public VNWindowState GetWindowState()
        {
            // focused=false, minimized=true (입력 막는 보수적 기본값)
            return Host != null ? Host.GetWindowState(appId) : new VNWindowState(false, true);
        }

        public void RequestLockExit(bool on)
        {
            ExitLocked = on;
            Host?.SetExitLocked(appId, on);
        }

        public void RequestBlockClose(bool on)
        {
            BlockClose = on;

        }

        public bool CanCloseNow()
        {
            if (ExitLocked) return false;
            if (!BlockClose) return true;
            return allowCloseOnce;
        }

        public bool ConsumeClosePermission()
        {
            if (!allowCloseOnce)
                return false;

            allowCloseOnce = false;
            return true;
        }


        public event Action OnForceCloseRequested;

        public void RequestForceClose()
        {

            allowCloseOnce = true;
            OnForceCloseRequested?.Invoke();
        }

        public event Action OnCloseRequested;

        public void ConsumeCloseOnce()
        {
            allowCloseOnce = false;
        }

        public void ResetCloseGuard()
        {
            allowCloseOnce = false;
            BlockClose = true;
        }



        // ✅ OS가 호출하는 함수
        public void NotifyCloseRequested()
        {
            Debug.Log($"[VNBridge] NotifyCloseRequested pending={closeRequestPending}");
            if (closeRequestPending) return;
            if (policy != null && policy.IsSaveLoadModalOpen)
            {
                Debug.Log("[VNBridge] Close request ignored (SaveLoad modal open).");
                return;
            }
            if (policy != null && !policy.CanRequestClose())
            {
                Debug.Log("[VNBridge] Close request ignored (policy blocked).");
                return;
            }

            closeRequestPending = true;

            // ✅ Close 요청 시 Auto 상태 자체를 OFF로 전환 (취소 후 1회 토글 복귀 보장)
            runner?.ForceAutoOff("CloseRequested");

            if (policy != null && policy.IsModalOpen)
            {
                closeRequestDeferred = true;
                Debug.Log("[VNBridge] Close request deferred until modal closes.");
                return;
            }

            // ClosePopup 모달 카운트는 VNClosePopupController.Show/Hide에서만 관리
            OnCloseRequested?.Invoke();
        }

        private void Update()
        {
            if (!closeRequestPending || !closeRequestDeferred)
                return;

            if (policy != null && policy.IsModalOpen)
                return;

            closeRequestDeferred = false;
            Debug.Log("[VNBridge] Deferred close request resumed.");
            OnCloseRequested?.Invoke();
        }

        public void ClearCloseRequestPending()
        {
            closeRequestPending = false;
            closeRequestDeferred = false;
        }

        public void SaveVN(string key, object data) => Host?.SaveSubBlock(key, data);
        public T LoadVN<T>(string key) where T : class => Host?.LoadSubBlock<T>(key);
    }
}
