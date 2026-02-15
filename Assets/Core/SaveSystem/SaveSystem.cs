using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    public static void Save(SaveData data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(), json);
        Debug.Log($"Saved to: {GetPath()}");
    }

    public static SaveData LoadOrCreate()
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            var fresh = new SaveData();
            Save(fresh);
            return fresh;
        }

        var json = File.ReadAllText(path);
        var loaded = JsonUtility.FromJson<SaveData>(json);
        return loaded ?? new SaveData();
    }
}
