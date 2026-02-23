using System;

public readonly struct VNWindowState
{
    public readonly bool Focused;
    public readonly bool Minimized;

    public VNWindowState(bool focused, bool minimized)
    {
        Focused = focused;
        Minimized = minimized;
    }
}

public interface IVNCloseRequestHandler
{
    // 닫기 버튼이 눌렸을 때 OS가 VN에게 "닫아도 돼?"를 물어보는 훅
    // true 반환: 닫아도 됨 / false 반환: 닫으면 안 됨(팝업 띄우거나 무시)
    bool CanCloseNow();
}

public interface IVNHostOS
{
    // 1) 현재 창 상태(입력 허용 여부에 필요)
    VNWindowState GetWindowState(string appId);

    // 2) 특정 구간에서 "나가기 금지" 락
    void SetExitLocked(string appId, bool locked);

    // 3) 닫기 인터셉트(확인 팝업 강제 등)
    void SetCloseHandler(string appId, IVNCloseRequestHandler handler);

    // 4) 세이브 서브블록
    void SaveSubBlock(string key, object data);
    T LoadSubBlock<T>(string key) where T : class;
}