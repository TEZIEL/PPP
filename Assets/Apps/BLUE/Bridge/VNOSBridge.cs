using UnityEngine;

public sealed class VNOSBridge : MonoBehaviour, IVNCloseRequestHandler
{
    [Header("Identity")]
    [SerializeField] private string appId = "Visual Novel";

    [Header("OS Host (adapter)")]
    [SerializeField] private MonoBehaviour hostBehaviour; // IVNHostOS 구현체
    private IVNHostOS Host => hostBehaviour as IVNHostOS;

    // VN이 설정하는 정책
    public bool ExitLocked { get; private set; }
    public bool BlockClose { get; private set; }

    private void Awake()
    {
        if (Host == null)
        {
            Debug.LogError("[VNOSBridge] Host is null or does not implement IVNHostOS.");
            return;
        }

        // 닫기 인터셉트 등록
        Host.SetCloseHandler(appId, this);
    }

    public VNWindowState GetWindowState()
    {
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
        // BlockClose는 Host 쪽 Close 로직에서 CanCloseNow()를 호출해서 반영됨
    }

    // OS가 닫기 누름 -> VN에게 물어봄
    public bool CanCloseNow()
    {
        // BlockClose가 켜져 있으면 "바로 닫지 말고" Host가 확인팝업 띄우게 만드는 방식
        // 여기서 false를 반환하면 Host는 Close를 중단하고, 대신 팝업을 띄우면 됨.
        return !BlockClose;
    }

    // Save 연동은 다음 단계에서 VNProgressSave 붙일 때 사용
    public void SaveVN(string key, object data) => Host?.SaveSubBlock(key, data);
    public T LoadVN<T>(string key) where T : class => Host?.LoadSubBlock<T>(key);
}