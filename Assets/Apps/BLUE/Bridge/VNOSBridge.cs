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
            // ExitLocked면 무조건 금지
            if (ExitLocked) return false;

            // ✅ 허용 토큰이 있을 때만 1회 통과
            if (allowCloseOnce)
            {
                allowCloseOnce = false;
                return true;
            }

            // ✅ 그 외는 무조건 막힘 (BlockClose 값이 뭐든 상관없게)
            return false;
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
            if (closeRequestPending) return;
            closeRequestPending = true;

            // ✅ Auto 즉시 중단
            runner?.StopAutoExternal("CloseRequested");

            // ✅ 모달 ON (진행/스페이스/오토 차단)
            policy?.SetModalOpen(true);

            OnCloseRequested?.Invoke();
        }

        public void ClearCloseRequestPending()
        {
            closeRequestPending = false;
        }

        public void SaveVN(string key, object data) => Host?.SaveSubBlock(key, data);
        public T LoadVN<T>(string key) where T : class => Host?.LoadSubBlock<T>(key);
    }
}