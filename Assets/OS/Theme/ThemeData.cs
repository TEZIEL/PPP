using UnityEngine;

[CreateAssetMenu(menuName = "OS/Theme Data", fileName = "ThemeData")]
public class ThemeData : ScriptableObject
{
    [Header("Window")]
    public Color windowTint = Color.white;

    [Header("Taskbar")]
    public Color taskbarBg = Color.white;
    public Color taskbarButtonNormal = Color.white;
    public Color taskbarButtonPressed = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("Optional Status Area")]
    public Color statusBarTint = Color.white;
}
