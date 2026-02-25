using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNDebugHotkeys : MonoBehaviour
    {
        [SerializeField] private VNRunner runner;

        private void Awake()
        {
            if (runner == null)
                runner = GetComponent<VNRunner>() ?? FindFirstObjectByType<VNRunner>();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (runner == null || !runner.HasScript)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha9))
            runner.DebugForceSave("9");

            if (Input.GetKeyDown(KeyCode.Alpha0))
            runner.DebugForceLoad("0");
#endif
        }
    }
}
