using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class FidgetShortformController : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("Wiring (Hierarchy)")]
    [SerializeField] private RectTransform swipeViewport; // SwipeViewport
    [SerializeField] private RectTransform pagesRoot;     // Pages
    [SerializeField] private RectTransform pagePrev;      // PagePrev
    [SerializeField] private RectTransform pageCurr;      // PageCurr
    [SerializeField] private RectTransform pageNext;      // PageNext

    [Header("Page Content")]
    [SerializeField] private Image prevImage;   // PagePrev 안의 Image
    [SerializeField] private Image currImage;   // PageCurr 안의 Image
    [SerializeField] private Image nextImage;   // PageNext 안의 Image
    [SerializeField] private Sprite[] sprites;  // 랜덤으로 뿌릴 이미지들

    [Header("Right Menu + Info (fade)")]
    [SerializeField] private CanvasGroup rightMenuCg;     // RightMenu (CanvasGroup)
    [SerializeField] private CanvasGroup infoCg;          // Information (CanvasGroup)
    [SerializeField] private float menuFadeDur = 0.08f;

    [Header("Gallery View")]
    [SerializeField] private GameObject galleryView;      // GalleryView
    [SerializeField] private Button backFromGalleryBtn;   // GalleryView/BackButton

    [Header("Counts UI")]
    [SerializeField] private TMP_Text likeCountText;
    [SerializeField] private TMP_Text dislikeCountText;

    [Header("Swipe Tuning")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float snapThreshold = 0.30f;  // 30% 넘으면 “쑤욱”
    [SerializeField] private float snapDuration = 0.18f;
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float wheelCooldown = 0.12f;

    [Header("Memory (History)")]
    [SerializeField] private int historyLimit = 200;

    private float _viewportH;
    private Vector2 _dragStartLocal;
    private Vector2 _rootStartPos;

    private bool _dragging;
    private bool _animating;
    private float _nextWheelTime;

    // 현재 표시되는 “컨텐츠 인덱스”
    private int _currIndex;

    // 이전/현재/다음 인덱스(3슬롯)
    private int _prevIndex;
    private int _nextIndex;

    // 갤러리용 “본 이미지” 기록(인덱스)
    private readonly List<int> _seen = new();

    // 좋아요/싫어요
    private int _likeCount;
    private int _dislikeCount;

    private const string PREF_LIKE = "fidget.like";
    private const string PREF_DISLIKE = "fidget.dislike";
    private const string PREF_CURR = "fidget.curr";

    private void Awake()
    {
        if (swipeViewport == null) swipeViewport = transform as RectTransform;

        // 뷰포트 높이
        _viewportH = swipeViewport != null ? swipeViewport.rect.height : 0f;
        if (_viewportH <= 1f) _viewportH = 600f; // 안전값

        // 카운트 로드
        _likeCount = PlayerPrefs.GetInt(PREF_LIKE, 0);
        _dislikeCount = PlayerPrefs.GetInt(PREF_DISLIKE, 0);
        ApplyCountsUI();

        // 현재 인덱스 로드 (없으면 랜덤)
        if (sprites != null && sprites.Length > 0)
        {
            _currIndex = Mathf.Clamp(PlayerPrefs.GetInt(PREF_CURR, Random.Range(0, sprites.Length)), 0, sprites.Length - 1);
        }
        else _currIndex = 0;

        // 초기 3슬롯 인덱스 구성
        _prevIndex = PickRandomIndex(except: _currIndex);
        _nextIndex = PickRandomIndex(except: _currIndex);

        // 초기 배치
        LayoutPages();
        ApplySprites();

        // 갤러리 처음엔 꺼둠
        if (galleryView) galleryView.SetActive(false);
        if (backFromGalleryBtn) backFromGalleryBtn.onClick.AddListener(CloseGallery);

        // 메뉴는 기본 표시
        SetMenuVisible(true, instant: true);

        // seen 기록
        PushSeen(_currIndex);
    }

    // -------------------------
    // Public UI hooks
    // -------------------------
    public void Like()
    {
        _likeCount++;
        PlayerPrefs.SetInt(PREF_LIKE, _likeCount);
        ApplyCountsUI();
        // (선택) 하트 애니는 나중에 여기서
    }

    public void Dislike()
    {
        _dislikeCount++;
        PlayerPrefs.SetInt(PREF_DISLIKE, _dislikeCount);
        ApplyCountsUI();
        // (선택) 깨진하트 애니는 나중에 여기서
    }

    public void ResetCounts()
    {
        _likeCount = 0;
        _dislikeCount = 0;
        PlayerPrefs.SetInt(PREF_LIKE, 0);
        PlayerPrefs.SetInt(PREF_DISLIKE, 0);
        ApplyCountsUI();
    }

    public void OpenGallery()
    {
        if (galleryView) galleryView.SetActive(true);
        // 갤러리 켜면 스와이프 잠깐 막는 느낌
        SetMenuVisible(false, instant: true);
    }

    public void CloseGallery()
    {
        if (galleryView) galleryView.SetActive(false);
        SetMenuVisible(true, instant: true);
    }

    // -------------------------
    // Drag / Wheel
    // -------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_animating) return;
        if (galleryView != null && galleryView.activeSelf) return;

        if (!TryGetLocal(eventData, out _dragStartLocal)) return;

        _dragging = true;
        _rootStartPos = pagesRoot.anchoredPosition;

        // 스와이프 시작하면 메뉴/인포 숨김
        SetMenuVisible(false, instant: false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _animating) return;

        if (!TryGetLocal(eventData, out var nowLocal)) return;

        float dy = nowLocal.y - _dragStartLocal.y;

        // root를 위아래로 끌어내림
        pagesRoot.anchoredPosition = new Vector2(_rootStartPos.x, _rootStartPos.y + dy);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging || _animating) return;
        _dragging = false;

        float y = pagesRoot.anchoredPosition.y;
        float th = snapThreshold * _viewportH;

        if (y <= -th)
        {
            // 아래로 넘김 (다음)
            StartCoroutine(CoSnapTo(-_viewportH, onDone: ShiftToNext));
        }
        else if (y >= th)
        {
            // 위로 넘김 (이전)
            StartCoroutine(CoSnapTo(+_viewportH, onDone: ShiftToPrev));
        }
        else
        {
            // 제자리 복귀
            StartCoroutine(CoSnapTo(0f, onDone: null));
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_animating) return;
        if (galleryView != null && galleryView.activeSelf) return;
        if (Time.unscaledTime < _nextWheelTime) return;

        _nextWheelTime = Time.unscaledTime + wheelCooldown;

        // 휠: 아래로 굴리면 다음(일반적으로 scrollDelta.y는 아래로 -값)
        if (eventData.scrollDelta.y < 0f)
        {
            SetMenuVisible(false, instant: false);
            StartCoroutine(CoSnapTo(-_viewportH, onDone: ShiftToNext));
        }
        else if (eventData.scrollDelta.y > 0f)
        {
            SetMenuVisible(false, instant: false);
            StartCoroutine(CoSnapTo(+_viewportH, onDone: ShiftToPrev));
        }
    }

    // -------------------------
    // Core shifting
    // -------------------------
    private void ShiftToNext()
    {
        // prev <- curr, curr <- next, next <- new random
        _prevIndex = _currIndex;
        _currIndex = _nextIndex;
        _nextIndex = PickRandomIndex(except: _currIndex);

        PlayerPrefs.SetInt(PREF_CURR, _currIndex);
        PushSeen(_currIndex);

        ApplySprites();

        // root 원위치로 리셋 (페이지 위치는 그대로 3슬롯)
        pagesRoot.anchoredPosition = Vector2.zero;

        // 메뉴 다시 표시
        SetMenuVisible(true, instant: false);
    }

    private void ShiftToPrev()
    {
        // next <- curr, curr <- prev, prev <- new random
        _nextIndex = _currIndex;
        _currIndex = _prevIndex;
        _prevIndex = PickRandomIndex(except: _currIndex);

        PlayerPrefs.SetInt(PREF_CURR, _currIndex);
        PushSeen(_currIndex);

        ApplySprites();

        pagesRoot.anchoredPosition = Vector2.zero;
        SetMenuVisible(true, instant: false);
    }

    // -------------------------
    // Helpers
    // -------------------------
    private void LayoutPages()
    {
        // PagePrev 위, PageCurr 중앙, PageNext 아래
        if (pagePrev) pagePrev.anchoredPosition = new Vector2(0, +_viewportH);
        if (pageCurr) pageCurr.anchoredPosition = new Vector2(0, 0);
        if (pageNext) pageNext.anchoredPosition = new Vector2(0, -_viewportH);

        if (pagesRoot) pagesRoot.anchoredPosition = Vector2.zero;
    }

    private void ApplySprites()
    {
        if (sprites == null || sprites.Length == 0) return;

        if (prevImage) prevImage.sprite = sprites[_prevIndex];
        if (currImage) currImage.sprite = sprites[_currIndex];
        if (nextImage) nextImage.sprite = sprites[_nextIndex];
    }

    private int PickRandomIndex(int except)
    {
        if (sprites == null || sprites.Length == 0) return 0;
        if (sprites.Length == 1) return 0;

        int r;
        int guard = 0;
        do
        {
            r = Random.Range(0, sprites.Length);
            guard++;
        } while (r == except && guard < 50);

        return r;
    }

    private void PushSeen(int idx)
    {
        _seen.Add(idx);
        if (_seen.Count > historyLimit) _seen.RemoveAt(0);
        // 갤러리 UI는 나중에 여기 _seen으로 채우면 됨
    }

    private void ApplyCountsUI()
    {
        if (likeCountText) likeCountText.text = _likeCount.ToString();
        if (dislikeCountText) dislikeCountText.text = _dislikeCount.ToString();
    }

    private bool TryGetLocal(PointerEventData e, out Vector2 local)
    {
        local = default;
        if (swipeViewport == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            swipeViewport, e.position, e.pressEventCamera, out local);
    }

    private IEnumerator CoSnapTo(float targetY, System.Action onDone)
    {
        _animating = true;

        Vector2 from = pagesRoot.anchoredPosition;
        Vector2 to = new Vector2(0, targetY);

        float t = 0f;
        float dur = Mathf.Max(0.01f, snapDuration);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float s = snapCurve != null ? snapCurve.Evaluate(Mathf.Clamp01(t)) : Mathf.SmoothStep(0, 1, t);
            pagesRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, s);
            yield return null;
        }

        pagesRoot.anchoredPosition = to;

        onDone?.Invoke();

        // (onDone에서 pagesRoot=0으로 리셋하니까) 여기서 강제 0은 안함
        _animating = false;
    }

    private void SetMenuVisible(bool on, bool instant)
    {
        // RightMenu + Information을 “같이” 숨겼다가 스와이프 끝나면 같이 복귀시키는 용도
        if (rightMenuCg == null && infoCg == null) return;

        if (instant)
        {
            if (rightMenuCg)
            {
                rightMenuCg.alpha = on ? 1f : 0f;
                rightMenuCg.blocksRaycasts = on;
                rightMenuCg.interactable = on;
            }
            if (infoCg)
            {
                infoCg.alpha = on ? 1f : 0f;
                infoCg.blocksRaycasts = on;
                infoCg.interactable = on;
            }
            return;
        }

        StopAllCoroutines(); // 스냅 코루틴과 섞일 수 있어서, 메뉴는 아래처럼 “간단” 처리로 통일
        StartCoroutine(CoFadeMenu(on ? 1f : 0f, on));
    }

    private IEnumerator CoFadeMenu(float toAlpha, bool enableInputAtEnd)
    {
        float dur = Mathf.Max(0.01f, menuFadeDur);

        float fromR = rightMenuCg ? rightMenuCg.alpha : 0f;
        float fromI = infoCg ? infoCg.alpha : 0f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float s = Mathf.SmoothStep(0, 1, t);

            if (rightMenuCg) rightMenuCg.alpha = Mathf.Lerp(fromR, toAlpha, s);
            if (infoCg) infoCg.alpha = Mathf.Lerp(fromI, toAlpha, s);

            yield return null;
        }

        if (rightMenuCg)
        {
            rightMenuCg.alpha = toAlpha;
            rightMenuCg.blocksRaycasts = enableInputAtEnd;
            rightMenuCg.interactable = enableInputAtEnd;
        }

        if (infoCg)
        {
            infoCg.alpha = toAlpha;
            infoCg.blocksRaycasts = enableInputAtEnd;
            infoCg.interactable = enableInputAtEnd;
        }
    }
}