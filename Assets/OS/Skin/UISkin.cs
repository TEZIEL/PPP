using UnityEngine;

[CreateAssetMenu(menuName = "OS/UISkin")]
public class UISkin : ScriptableObject
{
    [Header("TitleBar Tint")]
    public Color titleActiveColor = Color.white;
    public Color titleInactiveColor = Color.gray;

    [Header("Frame Tint")]
    public Color frameActiveColor = Color.white;
    public Color frameInactiveColor = Color.gray;
}
