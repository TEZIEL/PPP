using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SaveData
{
    public int version = 1;

    public GameState game = new();
    public ProgressState progress = new();
    public UnlockState unlocks = new();
    public OSState os = new();
}

