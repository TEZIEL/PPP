using UnityEngine;

[CreateAssetMenu(fileName = "UISkin", menuName = "OS/UI Skin")]
public class UISkin : ScriptableObject
{
    [Header("Window Tint")]
    public Color activeColor = new Color(0.92f, 0.92f, 0.98f, 1f);
    public Color inactiveColor = new Color(0.6f, 0.6f, 0.67f, 1f);
}
