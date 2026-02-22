using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class MusicPlayerUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Views")]
    [SerializeField] private GameObject playerView;   // MusicRoot
    [SerializeField] private GameObject listView;     // PlaylistView

    [Header("Video Area Hover UI")]
    [SerializeField] private CanvasGroup hoverGroup;  // OverlayDim + center/side btn + top-left info를 묶어서
    [SerializeField] private float hoverFadeDur = 0.12f;

    [Header("Info")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text authorText;

    [Header("Image")]
    [SerializeField] private Image mainImage;
    [SerializeField] private Sprite[] sprites; // 테스트용

    [Header("Track Info (separate from image)")]
    [SerializeField] private string trackTitle = "Track 1";
    [SerializeField] private string trackAuthor = "TEZIEL";

    [Header("Buttons")]
    [SerializeField] private Button leftBtn;
    [SerializeField] private Button rightBtn;
    [SerializeField] private Button playPauseCenterBtn;
    [SerializeField] private Button listBtn;
    [SerializeField] private Button backFromListBtn;

    private Coroutine _fadeCo;
    private int _index;

    private const string PREF_KEY_INDEX = "music.index";

    private void Awake()
    {
        _index = PlayerPrefs.GetInt(PREF_KEY_INDEX, 0);
        ApplyImageIndex();
        ApplyTrackInfo();

        SetHoverVisible(false, instant: true);

        if (leftBtn) leftBtn.onClick.AddListener(Prev);
        if (rightBtn) rightBtn.onClick.AddListener(Next);
        if (playPauseCenterBtn) playPauseCenterBtn.onClick.AddListener(TogglePlayPauseDummy);

        if (listBtn) listBtn.onClick.AddListener(OpenList);
        if (backFromListBtn) backFromListBtn.onClick.AddListener(CloseList);

        CloseList();
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHoverVisible(true);
    public void OnPointerExit(PointerEventData eventData) => SetHoverVisible(false);

    private void SetHoverVisible(bool on, bool instant = false)
    {
        if (hoverGroup == null) return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);

        if (instant)
        {
            hoverGroup.alpha = on ? 1f : 0f;
            hoverGroup.blocksRaycasts = on;
            hoverGroup.interactable = on;
            return;
        }

        _fadeCo = StartCoroutine(CoFade(hoverGroup, on ? 1f : 0f, hoverFadeDur, on));
    }

    private IEnumerator CoFade(CanvasGroup cg, float to, float dur, bool enableInputAtEnd)
    {
        float from = cg.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur);
            float s = Mathf.SmoothStep(0, 1, t);
            cg.alpha = Mathf.Lerp(from, to, s);
            yield return null;
        }
        cg.alpha = to;
        cg.blocksRaycasts = enableInputAtEnd;
        cg.interactable = enableInputAtEnd;
        _fadeCo = null;
    }

    private void Prev()
    {
    if (sprites == null || sprites.Length == 0) return;
    _index = (_index - 1 + sprites.Length) % sprites.Length;
    ApplyImageIndex();
    SaveIndex();
    }

    private void Next()
    {
    if (sprites == null || sprites.Length == 0) return;
    _index = (_index + 1) % sprites.Length;
    ApplyImageIndex();
    SaveIndex();
    }


    private void ApplyTrackInfo()
    {
        if (titleText) titleText.text = trackTitle;
        if (authorText) authorText.text = trackAuthor;
    }

    private void ApplyImageIndex()
    {
        if (mainImage && sprites != null && sprites.Length > 0)
            mainImage.sprite = sprites[_index];
    }

    private void SaveIndex() => PlayerPrefs.SetInt(PREF_KEY_INDEX, _index);

    private void TogglePlayPauseDummy()
    {
        // 지금은 “음악 실제 재생” 안 한다고 했으니
        // 버튼 아이콘 토글만 하거나 로그만 찍어도 됨
    }

    private void OpenList()
    {
        if (playerView) playerView.SetActive(false);
        if (listView) listView.SetActive(true);
    }

    private void CloseList()
    {
        if (playerView) playerView.SetActive(true);
        if (listView) listView.SetActive(false);
    }
}