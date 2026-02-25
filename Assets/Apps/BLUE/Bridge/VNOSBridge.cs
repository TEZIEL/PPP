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
        public bool BlockClose { get; private set; }
        private bool closeRequestPending;

        private bool registered; // ✅ 중복 등록 방지

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
            TryRegisterCloseHandler();
        }

        private void Awake()
        {
            // 인스펙터로도 꽂을 수 있지만, 정석은 OS가 InjectHost로 주입하는 것.
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
            // BlockClose는 Host 쪽 Close 로직에서 CanCloseNow() 호출해서 반영
        }

        // OS가 닫기 누름 -> VN에게 물어봄
        public bool CanCloseNow() => !BlockClose;


        public event Action OnForceCloseRequested;

        public void RequestForceClose()
        {
            BlockClose = false; // 이제 닫아도 됨
            OnForceCloseRequested?.Invoke();
        }

        public event Action OnCloseRequested;




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