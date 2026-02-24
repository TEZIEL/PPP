using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNState
    {
        public string scriptId;
        public int pointer;

        public List<VNIntVar> vars = new();

        public int greatCount;
        public int successCount;
        public int failCount;
        public string lastResult;
    }

    [Serializable]
    public sealed class VNIntVar
    {
        public string key;
        public int value;
    }
}