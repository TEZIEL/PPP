using UnityEngine;

namespace PPP.BLUE.VN.DrinkSystem
{
    public sealed class DrinkFailTracker : MonoBehaviour
    {
        [SerializeField] private int warningThreshold = 5;
        public int FailCount { get; private set; }

        public bool RegisterResult(string result)
        {
            if (string.Equals(result, "fail", System.StringComparison.OrdinalIgnoreCase))
            {
                FailCount++;
                return FailCount >= warningThreshold;
            }

            FailCount = 0;
            return false;
        }

        public void ResetFailCount()
        {
            FailCount = 0;
        }
    }
}
