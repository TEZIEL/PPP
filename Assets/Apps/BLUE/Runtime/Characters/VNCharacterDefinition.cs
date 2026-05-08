using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNCharacterDefinition
    {
        public string characterId;
        public string displayName;
        public string defaultExpressionId = "normal";
        public List<string> speakerIds = new();
        public VNCharacterRenderMode renderMode = VNCharacterRenderMode.FullSprite;
        public string defaultPosition = "center";
        public bool supportsBlink;
        public bool supportsMouth;
    }
}
