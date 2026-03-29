using UnityEngine;

[CreateAssetMenu(menuName = "BLUE/BGM Track Data")]
public class BGMTrackData : ScriptableObject
{
    public string trackId;
    public string displayName;
    public AudioClip clip;
    public Sprite artwork;
    public bool favoriteByDefault;
}