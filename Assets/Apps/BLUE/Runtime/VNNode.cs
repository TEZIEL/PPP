using System;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNNode
    {
        public string id;          // 라인 고유키 (seenSet 등에 사용)
        public VNNodeType type;

        // Say
        public string speakerId;
        public string text;

        // Label / Jump
        public string label;

        // Branch
        public BranchRule[] branches;

        [Serializable]
        public sealed class BranchRule
        {
            public string choiceText;
            public string expr;      // 조건식 (지금은 아주 단순하게)
            public string jumpLabel; // 조건이 true면 점프할 라벨
        }

        public override string ToString()
        {
            return $"[{type}] id={id} speaker={speakerId} label={label} text={text}";
        }
    }
}