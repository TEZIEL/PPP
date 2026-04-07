using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNState
    {
        public string scriptId;
        public int pointer;
        public string currentLabel;
        public string nodeId;
        public int nodeIndex;
        public string saveTime; // ISO-8601 local time string
        public bool isWaitingExternalCall;

        public List<VNIntVar> vars = new();
        public List<string> seen = new();
        public VNSettings settings = VNSettings.Default();

        public int greatCount;
        public int successCount;
        public int failCount;
        public string lastResult;

        public List<VNRunner.VNCallFrame> callStack = new();
        public VNDrinkState drink = new();
        public VNBacklogState backlog = new();
        public VNBacklogKey currentLineKey = new();
        public bool isCurrentLineTyping;
        public List<VNWindowStateData> windowStates = new();
    }

    [Serializable]
    public sealed class VNDrinkState
    {
        public string currentRequestId;
        public bool isActive;
    }

    [Serializable]
    public sealed class VNIntVar
    {
        public string key;
        public int value;
    }

    [Serializable]
    public sealed class VNWindowStateData
    {
        public string windowId;
        public float anchoredX;
        public float anchoredY;
        public bool isPinned;
    }

    [Serializable]
    public sealed class VNSettings
    {
        public bool auto;
        public float speed;

        public static VNSettings Default()
        {
            return new VNSettings
            {
                auto = false,
                speed = 1f,
            };
        }
    }
}
