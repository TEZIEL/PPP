using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BridgeMusicListView : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private ScrollRect trackListScrollRect;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BGMTrackItemUI itemPrefab;
    public BGMTrackItemUI SelectedItem => selectedItem;
    public BGMTrackData SelectedTrack => selectedItem != null ? selectedItem.BoundTrack : null;

    [Header("Scroll Buttons")]
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button disableButton;
    [SerializeField] private Button restoreButton;
    [SerializeField, Range(0.01f, 1f)] private float buttonScrollStep = 0.2f;

    [Header("Auto Scroll")]
    [SerializeField] private bool keepPlayingItemVisible = true;
    [SerializeField] private bool centerPlayingItemWhenChanged = true;
    [SerializeField] private float autoScrollPadding = 8f;

    private readonly List<BGMTrackItemUI> spawnedItems = new();
    private BGMTrackItemUI selectedItem;
    private Coroutine scrollIntoViewCo;

    private void Awake()
    {
        BindScrollButtons();
    }

    private void Update()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        var track = SelectedTrack;

        if (playButton != null)
            playButton.interactable = track != null && !manager.IsTrackDisabled(track);

        if (disableButton != null)
            disableButton.interactable = track != null && !manager.IsTrackDisabled(track);

        if (restoreButton != null)
            restoreButton.interactable = manager.HasDisabledTracks;
    }

    private void OnEnable()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        manager.OnLibraryChanged += Refresh;
        manager.OnTrackChanged += HandleTrackChanged;

        Refresh();
    }

    private void OnDisable()
    {
        var manager = BGMManager.Instance;
        if (manager != null)
        {
            manager.OnLibraryChanged -= Refresh;
            manager.OnTrackChanged -= HandleTrackChanged;
        }

        UnbindScrollButtons();

        if (scrollIntoViewCo != null)
        {
            StopCoroutine(scrollIntoViewCo);
            scrollIntoViewCo = null;
        }
    }

    private void BindScrollButtons()
    {
        if (scrollUpButton != null)
            scrollUpButton.onClick.AddListener(ScrollListUp);

        if (scrollDownButton != null)
            scrollDownButton.onClick.AddListener(ScrollListDown);
    }

    private void UnbindScrollButtons()
    {
        if (scrollUpButton != null)
            scrollUpButton.onClick.RemoveListener(ScrollListUp);

        if (scrollDownButton != null)
            scrollDownButton.onClick.RemoveListener(ScrollListDown);
    }

    public void ScrollListUp()
    {
        ScrollListByStep(+1f);
    }

    public void ScrollListDown()
    {
        ScrollListByStep(-1f);
    }

    private void ScrollListByStep(float direction)
    {
        if (trackListScrollRect == null)
            return;

        if (trackListScrollRect.content == null || trackListScrollRect.viewport == null)
            return;

        float contentHeight = trackListScrollRect.content.rect.height;
        float viewportHeight = trackListScrollRect.viewport.rect.height;
        if (contentHeight <= viewportHeight + 0.01f)
            return;

        float next = trackListScrollRect.verticalNormalizedPosition + (direction * buttonScrollStep);
        trackListScrollRect.verticalNormalizedPosition = Mathf.Clamp01(next);
    }

    public void PlaySelectedTrack()
    {
        var manager = BGMManager.Instance;
        var track = SelectedTrack;

        if (manager == null || track == null)
            return;

        manager.PlayTrackFromButton(track);
    }

    public void DisableSelectedTrack()
    {
        var manager = BGMManager.Instance;
        var track = SelectedTrack;

        if (manager == null || track == null)
            return;

        manager.DisableTrack(track);
        Refresh();
    }

    public void RestoreAllDisabledTracks()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        manager.RestoreAllTracks();
        Refresh();
    }

    public void Refresh()
    {
        if (contentRoot == null || itemPrefab == null)
            return;

        var prevSelectedId = SelectedTrack != null ? SelectedTrack.trackId : null;

        ClearItems();

        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        foreach (var track in manager.Tracks)
        {
            if (track == null)
                continue;

            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(track);
            item.OnClicked += OnItemClicked;

            bool isDisabled = manager.IsTrackDisabled(track);
            item.SetInteractable(!isDisabled);

            spawnedItems.Add(item);

            if (prevSelectedId != null && track.trackId == prevSelectedId)
            {
                selectedItem = item;
                selectedItem.SetSelected(true);
            }
        }

        SyncPlayingState();

        if (keepPlayingItemVisible && manager.CurrentTrack != null)
        {
            var currentItem = FindItemByTrack(manager.CurrentTrack);
            if (currentItem != null)
                ScrollItemIntoViewDeferred(currentItem.GetComponent<RectTransform>(), centerPlayingItemWhenChanged);
        }
    }

    private void ClearItems()
    {
        selectedItem = null;

        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                spawnedItems[i].OnClicked -= OnItemClicked;
                Destroy(spawnedItems[i].gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void OnItemClicked(BGMTrackItemUI clickedItem)
    {
        if (clickedItem == null)
            return;

        if (selectedItem != null && selectedItem != clickedItem)
            selectedItem.SetSelected(false);

        selectedItem = clickedItem;
        selectedItem.SetSelected(true);
    }

    private void HandleTrackChanged(BGMTrackData currentTrack)
    {
        SyncPlayingState();

        if (currentTrack == null)
            return;

        BGMTrackItemUI currentItem = null;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            var item = spawnedItems[i];
            if (item == null)
                continue;

            bool isCurrent = item.BoundTrack == currentTrack;
            item.SetPlaying(isCurrent);

            if (isCurrent)
            {
                currentItem = item;

                if (selectedItem != null && selectedItem != item)
                    selectedItem.SetSelected(false);

                selectedItem = item;
                selectedItem.SetSelected(true);
            }
        }

        if (keepPlayingItemVisible && currentItem != null)
        {
            ScrollItemIntoViewDeferred(currentItem.GetComponent<RectTransform>(), centerPlayingItemWhenChanged);
        }
    }

    private BGMTrackItemUI FindItemByTrack(BGMTrackData track)
    {
        if (track == null)
            return null;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            var item = spawnedItems[i];
            if (item != null && item.BoundTrack == track)
                return item;
        }

        return null;
    }

    private void ScrollItemIntoViewDeferred(RectTransform item, bool center)
    {
        if (!isActiveAndEnabled || item == null)
            return;

        if (scrollIntoViewCo != null)
            StopCoroutine(scrollIntoViewCo);

        scrollIntoViewCo = StartCoroutine(CoScrollItemIntoViewDeferred(item, center));
    }

    private IEnumerator CoScrollItemIntoViewDeferred(RectTransform item, bool center)
    {
        yield return null;

        if (trackListScrollRect == null || trackListScrollRect.content == null || trackListScrollRect.viewport == null || item == null)
            yield break;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(trackListScrollRect.content);

        ScrollItemIntoView(item, center);
        scrollIntoViewCo = null;
    }

    private void ScrollItemIntoView(RectTransform item, bool center)
    {
        if (trackListScrollRect == null || item == null)
            return;

        var content = trackListScrollRect.content;
        var viewport = trackListScrollRect.viewport;

        if (content == null || viewport == null)
            return;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        if (contentHeight <= viewportHeight + 0.01f)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
            return;
        }

        // item의 중심/상하단 위치를 content 로컬좌표 기준으로 계산
        float itemCenterY = -item.anchoredPosition.y;
        float itemHeight = item.rect.height;
        float itemTopY = itemCenterY - (itemHeight * 0.5f);
        float itemBottomY = itemCenterY + (itemHeight * 0.5f);

        float currentScrollY = content.anchoredPosition.y;
        float viewportTopY = currentScrollY;
        float viewportBottomY = currentScrollY + viewportHeight;

        float targetY = currentScrollY;

        if (center)
        {
            targetY = itemCenterY - (viewportHeight * 0.5f);
        }
        else
        {
            if (itemTopY < viewportTopY + autoScrollPadding)
            {
                targetY = itemTopY - autoScrollPadding;
            }
            else if (itemBottomY > viewportBottomY - autoScrollPadding)
            {
                targetY = itemBottomY - viewportHeight + autoScrollPadding;
            }
        }

        float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
        targetY = Mathf.Clamp(targetY, 0f, maxY);

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
    }

    private void SyncPlayingState()
    {
        var manager = BGMManager.Instance;
        if (manager == null)
            return;

        var currentTrack = manager.CurrentTrack;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            var item = spawnedItems[i];
            if (item == null)
                continue;

            bool isPlaying = currentTrack != null && item.BoundTrack == currentTrack && manager.IsPlaying;
            item.SetPlaying(isPlaying);
        }
    }
}