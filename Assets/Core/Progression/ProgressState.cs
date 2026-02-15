using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class ProgressState
{
    // key: characterId (¿¹: "melion", "sena")
    public Dictionary<string, int> success = new();
    public Dictionary<string, int> fail = new();
    public Dictionary<string, int> greatSuccess = new();
}
