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
        public List<string> seen = new();
        public VNSettings settings = VNSettings.Default();

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

    [Serializable]
    public sealed class VNSettings
    {
        public bool auto;
        public bool skip;
        public float speed;

        public static VNSettings Default()
        {
            return new VNSettings
            {
                auto = false,
                skip = false,
                speed = 1f,
            };
        }
    }
}
