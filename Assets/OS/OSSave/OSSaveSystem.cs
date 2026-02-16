using System.IO;
using UnityEngine;

namespace PPP.OS.Save
{
    public static class OSSaveSystem
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "os_save.json");

        public static void Save(OSSaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[OS SAVE] ¡æ {SavePath}");
        }

        public static OSSaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[OS LOAD] No save file.");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<OSSaveData>(json);
            Debug.Log("[OS LOAD] Loaded.");
            return data;
        }
    }
}
