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
        public List<VNWindowStateData> vnWindowStates = new();
        // Legacy field kept for backward compatibility with older save payloads.
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
        public string id;
        public float x;
        public float y;
        public bool pinned;

        // Legacy field set kept for backward compatibility with existing save payloads.
        public string windowId;
        public float anchoredX;
        public float anchoredY;
        public bool isPinned;
        public int siblingIndex;

        public void SetState(string stateId, float stateX, float stateY, bool statePinned)
        {
            id = stateId;
            x = stateX;
            y = stateY;
            pinned = statePinned;

            // Keep legacy fields in sync for compatibility.
            windowId = stateId;
            anchoredX = stateX;
            anchoredY = stateY;
            isPinned = statePinned;
        }

        public string GetId()
        {
            return !string.IsNullOrWhiteSpace(id) ? id : windowId;
        }

        public float GetX()
        {
            return !string.IsNullOrWhiteSpace(id) ? x : anchoredX;
        }

        public float GetY()
        {
            return !string.IsNullOrWhiteSpace(id) ? y : anchoredY;
        }

        public bool GetPinned()
        {
            return !string.IsNullOrWhiteSpace(id) ? pinned : isPinned;
        }
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
