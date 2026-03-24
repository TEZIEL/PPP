using System.IO;
using UnityEngine;

namespace PPP.OS.Save
{
    public static class OSSaveSystem
    {
        private const string DefaultFileName = "OS_SAVE.json";

        private static string GetPath(string fileName = null)
        {
            string resolved = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName;
            return Path.Combine(Application.persistentDataPath, resolved);
        }

        public static void Save(OSSaveData data, string fileName = null)
        {
            string savePath = GetPath(fileName);
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json);
                Debug.Log($"[OS SAVE] ¡æ {savePath} (len={json.Length}) existsAfter={File.Exists(savePath)}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[OS SAVE] FAILED path={savePath}\n{e}");
            }
        }

        public static OSSaveData Load(string fileName = null)
        {
            string savePath = GetPath(fileName);
            if (!File.Exists(savePath))
            {
                Debug.Log("[OS LOAD] No save file.");
                return null;
            }

            string json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<OSSaveData>(json);
            Debug.Log("[OS LOAD] Loaded.");
            return data;
        }
    }
}
