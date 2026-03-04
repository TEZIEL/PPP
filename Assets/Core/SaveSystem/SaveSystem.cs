using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FileName = "save.json";

    private static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

   


}