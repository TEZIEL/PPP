using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    public sealed class VNScript
    {
        public string ScriptId { get; }
        public List<VNNode> nodes;

        // label -> index
        private readonly Dictionary<string, int> labelToIndex = new();
        public int LabelCount => labelToIndex.Count;

        public VNScript(string scriptId, List<VNNode> nodes)
        {
            ScriptId = scriptId ?? string.Empty;
            this.nodes = nodes ?? new List<VNNode>();

            BuildLabelIndex(); // ✅ 필수
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

                var key = n.label.Trim();
                labelToIndex[key] = i;
            }
        }

        public string DumpLabels(int max = 50)
        {
            int n = 0;
            var sb = new System.Text.StringBuilder();
            foreach (var kv in labelToIndex)
            {
                sb.Append(kv.Key).Append("->").Append(kv.Value).Append(", ");
                if (++n >= max) break;
            }
            return sb.ToString();
        }

        public void RebuildLabelIndex()
        {
            BuildLabelIndex();
        }

        public bool TryGetLabelIndex(string label, out int index)
        {
            if (string.IsNullOrEmpty(label))
            {
                index = -1;
                return false;
            }
            return labelToIndex.TryGetValue(label.Trim(), out index);
        }
    }
}
