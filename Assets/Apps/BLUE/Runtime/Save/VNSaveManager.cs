using UnityEngine;

namespace PPP.BLUE.VN
{
    public class VNSaveManager : MonoBehaviour
    {
        public VNRunner vnRunner;

        public void SaveGame()
        {
            if (vnRunner == null)
            {
                Debug.LogError("VNRunner missing");
                return;
            }

            if (vnRunner.TrySaveNow("VNSaveManager"))
                Debug.Log("[VN] Game Saved (VN_SAVE.json)");
        }

        public void LoadGame()
        {
            if (vnRunner.TryLoadNow("VNSaveManager"))
                Debug.Log("[VN] Game Loaded (VN_SAVE.json)");
            else
                Debug.LogWarning("[VN] Save file not found");
        }
    }
}
