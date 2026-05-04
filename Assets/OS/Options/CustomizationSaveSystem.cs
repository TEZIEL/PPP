using System;
using System.IO;
using UnityEngine;

public static class CustomizationSaveSystem
{
    private const string FileName = "CUSTOMIZATION_SAVE.json";

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    public static bool Exists()
    {
        return File.Exists(GetPath());
    }

    public static CustomizationSaveData CreateDefault()
    {
        return new CustomizationSaveData
        {
            version = 1,
            uiThemeOptionIndex = 0,
            appUIThemeOptionIndex = 0,
            backgroundSkyIndex = 0,
            backgroundBuildingIndex = 0,
            backgroundHighlightIndex = 0
        };
    }

    public static CustomizationSaveData Load()
    {
        var path = GetPath();
        if (!File.Exists(path))
            return CreateDefault();

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<CustomizationSaveData>(json);
            return data ?? CreateDefault();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CustomizationSave] Load failed. fallback default. path={path} reason={e.Message}");
            return CreateDefault();
        }
    }

    public static void Save(CustomizationSaveData data)
    {
        if (data == null)
            data = CreateDefault();

        var path = GetPath();
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CustomizationSave] Save failed. path={path} reason={e}");
        }
    }
}
