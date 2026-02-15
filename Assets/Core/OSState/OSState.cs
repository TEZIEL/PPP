using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OSState
{
    // windowId -> position
    public Dictionary<string, Vector2> windowPositions = new();

    // 간단 스킨 값 (나중에 확장)
    public string skinId = "default";

    // desktop iconId -> position
    public Dictionary<string, Vector2> desktopIconPositions = new();
}

