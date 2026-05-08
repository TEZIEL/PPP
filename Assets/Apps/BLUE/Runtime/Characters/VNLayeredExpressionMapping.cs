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
        public Sprite eyebrowOpenSprite;
        public Sprite eyebrowBlinkHalfSprite;
        public Sprite eyebrowBlinkClosedSprite;
        public Sprite eyeOpenSprite;
        public Sprite eyeBlinkHalfSprite;
        public Sprite eyeBlinkClosedSprite;
        public Sprite eyeClosedSprite;
        public Sprite mouthClosedSprite;
        public List<Sprite> mouthOpenSprites = new();
    }
}
