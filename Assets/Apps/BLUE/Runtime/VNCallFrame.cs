using System;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNCallFrame
    {
        public int returnPointer;
        public string target;
        public string arg;
    }
}
