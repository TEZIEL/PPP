using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNBacklogKey
    {
        public string scriptId;
        public string nodeId;
        public int lineIndex;

        public VNBacklogKey()
        {
        }

        public VNBacklogKey(string scriptId, string nodeId, int lineIndex)
        {
            this.scriptId = scriptId ?? string.Empty;
            this.nodeId = nodeId ?? string.Empty;
            this.lineIndex = lineIndex;
        }

        public string ToCompositeKey()
            => $"{scriptId ?? string.Empty}|{nodeId ?? string.Empty}|{lineIndex}";
    }

    [Serializable]
    public sealed class VNBacklogEntry
    {
        public string scriptId;
        public string nodeId;
        public int lineIndex;
        public string speaker;
        public string text;
        public bool isFinal;
        public long sequence;

        public string CompositeKey => $"{scriptId ?? string.Empty}|{nodeId ?? string.Empty}|{lineIndex}";

        public VNBacklogKey ToKey()
            => new VNBacklogKey(scriptId, nodeId, lineIndex);

        public VNBacklogEntry Clone()
        {
            return new VNBacklogEntry
            {
                scriptId = scriptId,
                nodeId = nodeId,
                lineIndex = lineIndex,
                speaker = speaker,
                text = text,
                isFinal = isFinal,
                sequence = sequence
            };
        }
    }

    [Serializable]
    public sealed class VNBacklogState
    {
        public List<VNBacklogEntry> entries = new();
        public long nextSequence;
    }
}
