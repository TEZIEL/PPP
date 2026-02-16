using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPP.OS.Save
{
    [Serializable]
    public class OSSaveData
    {
        public List<OSWindowData> windows = new List<OSWindowData>();
    }

    [Serializable]
    public class OSWindowData
    {
        public string appId;
        public Vector2Int position;
        public Vector2Int size;
        public bool isMinimized;
    }
}
