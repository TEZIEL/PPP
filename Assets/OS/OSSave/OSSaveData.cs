using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.OS.Save
{
    [Serializable]
    public class OSSaveData
    {
        public int version = 2;
        public List<OSWindowData> windows = new();
        public List<OSIconData> icons = new();
    }

    [Serializable]
    public class OSWindowData
    {
        public string appId;
        public Vector2Int position;
        public Vector2Int size;
        public bool isMinimized;
    }

    [Serializable]
    public class OSIconData
    {
        public string id;
        public Vector2 normalized;
        public int slotIndex = -1;
        public DesktopLayoutMode layoutMode;
    }

    public enum DesktopLayoutMode
    {
        Free,
        Grid
    }
}
