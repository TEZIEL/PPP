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

    // Hook only: caller can decide when to flush Save(Bootstrap.Data).
    public static void SetWindowPositionHook(string appId, Vector2 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        PlayerPrefs.SetInt(appId + "_x", x);
        PlayerPrefs.SetInt(appId + "_y", y);
    }


    // Hook only: runtime reads from loaded SaveData.
    public static bool TryGetWindowPosition(string appId, out Vector2 pos)
    {
        if (!PlayerPrefs.HasKey(appId + "_x"))
        {
            pos = Vector2.zero;
            return false;
        }

        int x = PlayerPrefs.GetInt(appId + "_x");
        int y = PlayerPrefs.GetInt(appId + "_y");

        pos = new Vector2(x, y);
        return true;
    }

}