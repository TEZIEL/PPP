using UnityEngine;

[CreateAssetMenu(menuName = "OS/UISkin")]
public class UISkin : ScriptableObject
{
    [Header("TitleBar Sprites")]
    public Sprite titleActive;
    public Sprite titleInactive;

    [Header("UnderBar Sprites")]
    public Sprite underActive;
    public Sprite underInactive;

    // (선택) 필요하면 색 틴트도 남겨둘 수 있음
    public Color tint = Color.white;
}