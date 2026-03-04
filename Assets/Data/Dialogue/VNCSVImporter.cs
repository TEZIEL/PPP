using UnityEngine;
using UnityEditor;
using System;                 // ← 이것 추가
using System.IO;
using System.Collections.Generic;
using PPP.BLUE.VN;

public class VNCSVImporter : AssetPostprocessor
{
    static string csvFolder = "Assets/ScenarioCSV/";
    static string jsonFolder = "Assets/ScenarioJSON/";

    static void OnPostprocessAllAssets(
        string[] imported,
        string[] deleted,
        string[] moved,
        string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (!path.StartsWith(csvFolder)) continue;
            if (!path.EndsWith(".csv")) continue;

            ConvertCSV(path);
        }
    }
    
    [Serializable]
    public class VNNodeList
    {
        public List<VNNode> nodes;
    }

    static void ConvertCSV(string csvPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvPath);

        string[] lines = File.ReadAllLines(csvPath);

        List<VNNode> nodes = new List<VNNode>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] col = lines[i].Split(',');

            VNNode node = new VNNode();

            node.id = col.Length > 0 ? col[0] : "";

            if (col.Length > 1)
            {
                if (System.Enum.TryParse(col[1], true, out VNNodeType t))
                    node.type = t;
                else
                    node.type = VNNodeType.Say;
            }

            node.speakerId = col.Length > 2 ? col[2] : "";
            node.text = col.Length > 3 ? col[3] : "";
            node.label = col.Length > 4 ? col[4] : "";

            nodes.Add(node);
        }

        VNNodeList list = new VNNodeList();
        list.nodes = nodes;

        string json = JsonUtility.ToJson(list, true);

        if (!Directory.Exists(jsonFolder))
            Directory.CreateDirectory(jsonFolder);

        string jsonPath = jsonFolder + fileName + ".json";

        File.WriteAllText(jsonPath, json);

        Debug.Log($"[VN] CSV → JSON generated: {jsonPath}");
    }
}