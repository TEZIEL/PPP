using System.IO;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public static class VNFileSaveSystem
    {
        private static string GetPathForKey(string key)
        {
            if (string.Equals(key, "vn.state.dbg", System.StringComparison.OrdinalIgnoreCase))
                return Path.Combine(Application.persistentDataPath, "VN_SAVE_DBG.json");

            return Path.Combine(Application.persistentDataPath, "VN_SAVE.json");
        }

        public static void Save(string key, VNState state)
        {
            if (state == null) return;

            var path = GetPathForKey(key);
            var json = JsonUtility.ToJson(state, true);
            File.WriteAllText(path, json);
            Debug.Log($"[VN SAVE] path={path} len={json.Length}");
        }

        public static VNState Load(string key)
        {
            var path = GetPathForKey(key);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            var state = JsonUtility.FromJson<VNState>(json);
            Debug.Log($"[VN LOAD] path={path} len={json.Length}");
            return state;
        }
    }
}

