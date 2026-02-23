using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    public sealed class VNScript
    {
        public readonly string scriptId;
        public readonly List<VNNode> nodes;

        // label -> index
        private readonly Dictionary<string, int> labelToIndex = new();

        public VNScript(string scriptId, List<VNNode> nodes)
        {
            this.scriptId = scriptId;
            this.nodes = nodes ?? new List<VNNode>();
            BuildLabelIndex();
        }

        private void BuildLabelIndex()
        {
            labelToIndex.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n == null) continue;
                if (n.type != VNNodeType.Label) continue;
                if (string.IsNullOrEmpty(n.label)) continue;

                // 같은 라벨이 중복이면 마지막이 덮어쓰게 됨 (초기엔 이걸로 충분)
                labelToIndex[n.label] = i;
            }
        }

        public bool TryGetLabelIndex(string label, out int index)
            => labelToIndex.TryGetValue(label, out index);
    }
}