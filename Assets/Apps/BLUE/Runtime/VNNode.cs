using System;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNNode
    {
        public string id;          // Stable line key (seenSet, save)
        public VNNodeType type;

        // Say
        public string speakerId;
        public string text;

        // Label / Jump
        public string label;

        // Call
        public string callTarget;
        public string callArg;

        // Branch
        public BranchRule[] branches;

        // Choice
        public ChoiceOption[] choices;

        [Serializable]
        public sealed class BranchRule
        {
            public string choiceText;
            public string expr;      // Conditional expression
            public string jumpLabel; // Jump target when condition passes
        }

        [Serializable]
        public sealed class ChoiceOption
        {
            public string choiceText;
            public string jumpLabel;
        }

        public override string ToString()
        {
            return $"[{type}] id={id} speaker={speakerId} label={label} callTarget={callTarget} callArg={callArg} text={text}";
        }
    }
}
