using System;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNScriptDTO
    {
        public string scriptId;
        public VNNodeDTO[] nodes;
    }

    [Serializable]
    public sealed class VNNodeDTO
    {
        public string id;
        public string type;
        public string speakerId;
        public string text;
        public string label;
        public VNBranchRuleDTO[] branches;
        public VNChoiceOptionDTO[] choices;
    }

    [Serializable]
    public sealed class VNBranchRuleDTO
    {
        public string choiceText;
        public string expr;
        public string jumpLabel;
    }

    [Serializable]
    public sealed class VNChoiceOptionDTO
    {
        public string choiceText;
        public string jumpLabel;
    }
}
