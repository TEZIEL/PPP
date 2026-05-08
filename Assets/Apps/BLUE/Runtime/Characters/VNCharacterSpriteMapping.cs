using System;
using UnityEngine;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNCharacterSpriteMapping
    {
        public string characterId;
        public string expressionId = "normal";
        public Sprite sprite;
    }
}
