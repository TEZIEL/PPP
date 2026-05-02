using System;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNScriptDTO
    {
        public string scenarioId;
        public string scriptId;
        public string startNodeId;
        public VNCharacterDTO[] characters;
        public VNNodeDTO[] nodes;
    }

    [Serializable]
    public sealed class VNCharacterDTO
    {
        public string characterId;
        public string displayName;
        public string defaultExpression;
    }

    [Serializable]
    public sealed class VNNodeDTO
    {
        public string id;
        public string type;
        public string speakerId;
        public string speaker;
        public string expressionId;
        public string expression;
        public string text;
        public string label;
        public string target;
        public string arg;
        public string cond;
        public string next;
        public string arg1;
        public string arg2;
        public string background;
        public string bgm;
        public string sfx;
        public string command;
        public VNBranchRuleDTO[] branches;
        public VNChoiceOptionDTO[] choices;
    }

    [Serializable]
    public sealed class VNBranchRuleDTO
    {
        public string choiceText;
        public string expr;
        public string cond;
        public string jumpLabel;
        public string next;
    }

    [Serializable]
    public sealed class VNChoiceOptionDTO
    {
        public string choiceText;
        public string jumpLabel;
        public string next;
    }
}
