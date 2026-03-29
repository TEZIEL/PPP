using UnityEngine;

public class BridgeMusicListView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BGMTrackItemUI itemPrefab;

    private void OnEnable()
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.OnLibraryChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (BGMManager.Instance != null)
            BGMManager.Instance.OnLibraryChanged -= Refresh;
    }

    public void Refresh()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        var manager = BGMManager.Instance;
        if (manager == null) return;

        foreach (var track in manager.Tracks)
        {
            if (track == null) continue;
            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(track);
        }
    }
}