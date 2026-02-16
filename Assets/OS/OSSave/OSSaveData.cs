using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.OS.Save
{
    [Serializable]
    public class OSSaveData
    {
        public List<OSWindowData> windows = new List<OSWindowData>();
        public List<OSIconData> icons = new List<OSIconData>();
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
        public Vector2Int position;
    }
}
