using System;

namespace PPP.BLUE.VN
{
    public readonly struct VNWindowState
    {
        public readonly bool IsFocused;
        public readonly bool IsMinimized;

        public VNWindowState(bool isFocused, bool isMinimized)
        {
            IsFocused = isFocused;
            IsMinimized = isMinimized;
        }
    }

    public interface IVNCloseRequestHandler
    {
        bool CanCloseNow();
        void NotifyCloseRequested();
    }

    public interface IVNHostOS
    {
        VNWindowState GetWindowState(string appId);
        void SetExitLocked(string appId, bool locked);
        void SetCloseHandler(string appId, IVNCloseRequestHandler handler);

        void SaveSubBlock(string key, object data);
        T LoadSubBlock<T>(string key) where T : class;
    }
}