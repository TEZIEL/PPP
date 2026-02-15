using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public static SaveData Data;

    private void Awake()
    {
        Data = SaveSystem.LoadOrCreate();
        Debug.Log($"Loaded day: {Data.game.day}, scene: {Data.game.sceneId}");
    }
}
