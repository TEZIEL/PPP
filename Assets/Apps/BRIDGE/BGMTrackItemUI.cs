using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BGMTrackItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private Button playButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button likeButton;

    private BGMTrackData boundTrack;

    public void Bind(BGMTrackData track)
    {
        boundTrack = track;

        if (titleText != null)
            titleText.text = track != null ? track.displayName : string.Empty;

        if (artworkImage != null)
            artworkImage.sprite = track != null ? track.artwork : null;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                if (boundTrack != null)
                    BGMManager.Instance?.PlayTrack(boundTrack);
            });
        }

        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                if (boundTrack != null)
                    BGMManager.Instance?.BlockTrack(boundTrack.trackId);
            });
        }

        if (likeButton != null)
        {
            likeButton.onClick.RemoveAllListeners();
            likeButton.onClick.AddListener(() =>
            {
                Debug.Log($"Liked: {boundTrack?.displayName}");
            });
        }
    }
}