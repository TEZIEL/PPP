using System;
using System.Collections.Generic;

namespace PPP.BLUE.VN.DrinkSystem
{
    [Serializable]
    public sealed class DrinkData
    {
        public string id;
        public string name;
        public string imageKey;
        public Dictionary<string, int> ingredients = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> category = new List<string>();
        public List<string> tags = new List<string>();
        public bool artheon_addable;
        public int total;
    }
}
