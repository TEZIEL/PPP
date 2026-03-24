#if false
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

            List<string> col = ParseCSVLine(lines[i]);

            VNNode node = new VNNode();

            node.id = col.Count > 0 ? col[0].Trim() : "";

            if (col.Count > 1)
            {
                if (System.Enum.TryParse(col[1].Trim(), true, out VNNodeType t))
                    node.type = t;
                else
                    node.type = VNNodeType.Say;
            }

            node.speakerId = col.Count > 2 ? col[2].Trim() : "";
            node.text = col.Count > 3 ? col[3].Trim() : "";
            node.label = col.Count > 4 ? col[4].Trim() : "";

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


    static List<string> ParseCSVLine(string line)
    {
        List<string> result = new List<string>();

        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
                continue;
            }

            current += c;
        }

        result.Add(current);

        return result;
    }

}
#endif