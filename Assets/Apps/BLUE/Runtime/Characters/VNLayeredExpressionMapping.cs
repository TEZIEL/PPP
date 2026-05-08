using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.BLUE.VN
{
    [Serializable]
    public sealed class VNLayeredExpressionMapping
    {
        public string characterId;
        public string expressionId = "normal";
        public Sprite baseSprite;
        public Sprite eyebrowSprite;
        public Sprite eyeOpenSprite;
        public Sprite eyeClosedSprite;
        public Sprite mouthClosedSprite;
        public List<Sprite> mouthOpenSprites = new();
    }
}
