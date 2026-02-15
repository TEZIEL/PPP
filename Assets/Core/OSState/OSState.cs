using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OSState
{
    // windowId -> position
    public Dictionary<string, Vector2> windowPositions = new();

    // current skin id (expand later)
    public string skinId = "default";

    // desktop iconId -> position
    public Dictionary<string, Vector2> desktopIconPositions = new();

public void SetWindowPosition(string appId, Vector2 position)
{
    if (string.IsNullOrWhiteSpace(appId))
    {
        return;
    }

    windowPositions[appId] = position;
}

public bool TryGetWindowPosition(string appId, out Vector2 position)
{
    if (string.IsNullOrWhiteSpace(appId))
    {
        position = default;
        return false;
    }

    return windowPositions.TryGetValue(appId, out position);
}
}
