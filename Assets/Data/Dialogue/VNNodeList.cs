using UnityEngine;
using UnityEditor;
using System;                 // ← 이것 추가
using System.IO;
using System.Collections.Generic;
using PPP.BLUE.VN;

[Serializable]
public class VNNodeList
{
    public List<VNNode> nodes;
}

