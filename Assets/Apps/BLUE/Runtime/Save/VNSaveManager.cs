using UnityEngine;
using System.IO;

namespace PPP.BLUE.VN
{
    public class VNSaveManager : MonoBehaviour
    {
        public VNRunner vnRunner;

        string savePath;

        void Awake()
        {
            savePath = Path.Combine(Application.persistentDataPath, "save.json");
        }

        public void SaveGame()
        {
            if (vnRunner == null)
            {
                Debug.LogError("VNRunner missing");
                return;
            }

            VNState state = vnRunner.ExportState();

            VNSaveBlock block = new VNSaveBlock();
            block.state = state;

            string json = JsonUtility.ToJson(block, true);

            File.WriteAllText(savePath, json);

            Debug.Log("[VN] Game Saved");
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
            {
                Debug.LogWarning("[VN] Save file not found");
                return;
            }

            string json = File.ReadAllText(savePath);

            VNSaveBlock block = JsonUtility.FromJson<VNSaveBlock>(json);

            vnRunner.ImportState(block.state);

            Debug.Log("[VN] Game Loaded");
        }
    }
}