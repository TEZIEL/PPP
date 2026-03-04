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
            savePath = Application.persistentDataPath + "/vn_save.json";
        }

        public void SaveGame()
        {
            if (vnRunner == null)
            {
                Debug.LogError("VNRunner missing");
                return;
            }

            VNSaveBlock block = new VNSaveBlock();
            block.state = vnRunner.ExportState();

            string json = JsonUtility.ToJson(block, true);
            File.WriteAllText(savePath, json);

            Debug.Log("VN Save complete");
        }

        public void LoadGame()
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("Save not found");
                return;
            }

            string json = File.ReadAllText(savePath);

            VNSaveBlock block = JsonUtility.FromJson<VNSaveBlock>(json);

            vnRunner.ImportState(block.state);

            Debug.Log("VN Load complete");
        }
    }
}