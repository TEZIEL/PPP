using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // ✅ 추가

public class FidgetShortsController : MonoBehaviour, IScrollHandler
{
    [Header("Viewport / Pages")]
    [SerializeField] private RectTransform swipeViewport;   // SwipeViewport
    [SerializeField] private RectTransform pagesRoot;       // Pages
    [SerializeField] private Image pagePrevImage;           // PagePrev 안의 Image
    [SerializeField] private Image pageCurrImage;           // PageCurr 안의 Image
    [SerializeField] private Image pageNextImage;           // PageNext 안의 Image

    [Header("UI Groups to hide while swiping")]
    [SerializeField] private CanvasGroup rightMenuGroup;    // RightMenu (CanvasGroup 권장)
    [SerializeField] private CanvasGroup infoGroup;         // Information (CanvasGroup 권장)
    [Header("Overlay Hover While Swiping")]
    [SerializeField, Range(0f, 1f)] private float overlayHoverAlpha = 0f;
    [SerializeField] private float overlayFadeDur = 0.08f;


    [Header("Gallery View")]
    [SerializeField] private GameObject galleryView;        // GalleryView
    [SerializeField] private Button galleryButton;          // RightMenu/Gallery 버튼(선택)
    [SerializeField] private Button backButton;             // GalleryView/BackButton

    [Header("Sprites")]
    [SerializeField] private Sprite[] pool;                 // 랜덤으로 쓸 이미지 풀

    [Header("Swipe")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float commitThreshold01 = 0.15f;
    [SerializeField] private float swipeAnimDur = 0.12f;

    [SerializeField] private float wheelCooldown = 0.12f;

    [Header("Swipe Tuning")]
    
    [SerializeField] private float dragSensitivity = 1.6f;    // 드래그가 얼마나 잘 따라오나(증폭)

    [SerializeField] private TMP_Text likeCountText;
    [SerializeField] private TMP_Text dislikeCountText;

    // “아래로 내릴 때” 다음 이미지 프리뷰/확정을 일치시키기 위한 pending
    
    private int _pendingNextIndex = -1; // next가 아직 history에 없을 때, “미리보기용”으로 1번만 뽑아두는 캐시
    private float _lastWheelTime;
    private int _likeCount;
    private int _dislikeCount;


    private void SetupPageRects()
    {
        if (!swipeViewport) return;

        float w = swipeViewport.rect.width;
        float h = swipeViewport.rect.height;

        void Setup(RectTransform rt, float posY)
        {
            if (!rt) return;

            // Middle Center 고정
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, posY);
            rt.localScale = Vector3.one;
        }

        Setup(pagePrevImage ? (RectTransform)pagePrevImage.transform : null, +h);
        Setup(pageCurrImage ? (RectTransform)pageCurrImage.transform : null, 0f);
        Setup(pageNextImage ? (RectTransform)pageNextImage.transform : null, -h);
    }

    

    

    private void ResetShorts()
    {
        if (pool == null || pool.Length == 0) return;

        history.Clear();
        history.Add(PickRandomIndex());
        cursor = 0;

           _pendingNextIndex = -1;

        ApplyImagesInstant();

        if (pagesRoot) pagesRoot.anchoredPosition = Vector2.zero;
    }

    private void RefreshCounts()
    {
        if (likeCountText) likeCountText.text = _likeCount.ToString();
        if (dislikeCountText) dislikeCountText.text = _dislikeCount.ToString();
    }


    
    private float viewH;
    private bool isSwiping;
    private Vector2 dragStartLocal;
    private float dragDeltaY; // +면 위로 드래그(다음), -면 아래로 드래그(이전)

    // 히스토리: 인덱스 기반
    private readonly List<int> history = new List<int>();
    private int cursor = 0; // history[cursor]가 현재

    

 


    private void Awake()
    {
        if (swipeViewport == null) swipeViewport = GetComponentInChildren<RectTransform>();
        viewH = swipeViewport.rect.height;

      

        SetOverlayVisible(true, instant: true);
        
        if (galleryButton) galleryButton.onClick.AddListener(OpenGallery);
        if (backButton) backButton.onClick.AddListener(CloseGallery);

        CloseGallery();
        RefreshCounts();
                
        
    }


    private void OnEnable()
    {
        if (swipeViewport == null) return;

        viewH = swipeViewport.rect.height;

        ResetShorts();
        SetupPageRects();

        // ResetShorts 안에서 ApplyImagesInstant를 이미 호출한다면 여기선 생략 가능
        // ApplyImagesInstant();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (swipeViewport != null)
        {
            viewH = swipeViewport.rect.height;
            
            SetupPageRects();
        }
    }

    public void AddLike()
    {
        _likeCount++;
        RefreshCounts();
    }

    public void AddDislike()
    {
        _dislikeCount++;
        RefreshCounts();
    }

    public void ResetCounts()
    {
        _likeCount = 0;
        _dislikeCount = 0;
        RefreshCounts();
    }

   

    public void OpenGallery()
    {
        // 스와이프 중이면 무시 (레이스 방지)
        if (isSwiping) return;

        if (galleryView) galleryView.SetActive(true);

        // 갤러리 켜면 메인 입력/메뉴 꺼도 되는데, "루트"는 끄지 마
        SetOverlayVisible(false, instant: true);
        if (pagesRoot) pagesRoot.gameObject.SetActive(false);
    }

    public void CloseGallery()
    {
        if (galleryView) galleryView.SetActive(false);

        if (pagesRoot) pagesRoot.gameObject.SetActive(true);
        SetOverlayVisible(true, instant: true);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Time.unscaledTime - _lastWheelTime < wheelCooldown) return;

        float y = eventData.scrollDelta.y;
        if (Mathf.Abs(y) < 0.01f) return;

        // 보통 휠 아래(-) = 다음, 위(+) = 이전
        if (y < 0f) CommitNext();
        else
        {
            if (cursor > 0) CommitPrev();
            // cursor==0이면 이전 없음
        }

        _lastWheelTime = Time.unscaledTime;
    }

    // ====== 입력(드래그/휠) ======
    private void Update()
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;
        if (pool == null || pool.Length == 0) return;

        // ✅ 휠 fallback (OnScroll이 안 와도 동작)
        // ✅ 휠 fallback (OnScroll이 안 와도 동작)
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            if (Time.unscaledTime - _lastWheelTime < wheelCooldown) return;

            if (wheel < 0f)         // 아래로 굴림 = 다음
            {
                CommitNext();
            }
            else                    // 위로 굴림 = 이전 (첫 장이면 막기)
            {
                if (cursor > 0) CommitPrev();
            }

            _lastWheelTime = Time.unscaledTime;
        }
    }

    // 아래 3개 함수는 SwipeViewport에 붙일 입력 스크립트가 호출하게 할 수도 있는데,
    // 너가 "스크립트 하나로 끝내고 싶다"면 SwipeViewport에 EventTrigger로 연결해도 됨.
    // (하지만 난 깔끔하게 Input 기반 + 레이캐스트 이슈 최소화를 위해 별도 입력 스크립트도 추천함)
    public void BeginDrag(Vector2 screenPos, Camera uiCam)
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;

        if (!ScreenToLocal(screenPos, uiCam, out dragStartLocal))
            return;

        dragDeltaY = 0f;
        SetOverlaySwiping(true, instant: true); // ✅ 숨김은 즉시
    }

    public void Drag(Vector2 screenPos, Camera uiCam)
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;

        if (!ScreenToLocal(screenPos, uiCam, out var local))
            return;

        dragDeltaY = (local.y - dragStartLocal.y);

        // PagesRoot를 실제로 끌어내리는 연출(선택)
        // y가 -면 아래로 드래그(다음), +면 위로 드래그(이전)로 쓸거면 여기서 부호 바꿔도 됨
        // Drag() 내부에서
        if (pagesRoot)
        {
            float targetY = dragDeltaY * dragSensitivity;

            // 너무 멀리 끌려나가는 거 방지 (화면 1.1배 정도까지만)
            float clamp = viewH * 1.1f;
            targetY = Mathf.Clamp(targetY, -clamp, clamp);

            // ✅ 첫 장에서는 “이전(Prev) 방향” 드래그 자체를 못 하게 막기
            if (cursor == 0)
                targetY = Mathf.Max(0f, targetY);

            // ✅ 즉시 대입 대신 부드럽게 따라가게
            float follow = 25f; // 숫자↑ = 더 빨리 따라감 (손맛). 18~35 추천
            float newY = Mathf.Lerp(pagesRoot.anchoredPosition.y, targetY, 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime));
            pagesRoot.anchoredPosition = new Vector2(0f, newY);
        }
    }

    public void EndDrag()
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;

        float y = pagesRoot ? pagesRoot.anchoredPosition.y : 0f;
        float moved01 = Mathf.Abs(y) / Mathf.Max(1f, viewH);

        if (moved01 < commitThreshold01)
        {
            StartCoroutine(CoSnapBack());
            return;
        }

        if (y > 0f)
        {
            CommitNext(); // 다음
        }
        else
        {
            if (cursor > 0) CommitPrev(); // 이전
            else StartCoroutine(CoSnapBack());
        }
    }

    private IEnumerator CoSnapBack()
    {
        isSwiping = true;
        SetOverlaySwiping(true, instant: true); // ✅ 스냅백 중에도 확실히 숨김

        Vector2 from = pagesRoot ? pagesRoot.anchoredPosition : Vector2.zero;
        Vector2 to = Vector2.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, swipeAnimDur);
            float s = Mathf.SmoothStep(0f, 1f, t);
            if (pagesRoot) pagesRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, s);
            yield return null;
        }

        if (pagesRoot) pagesRoot.anchoredPosition = Vector2.zero;
        isSwiping = false;
        SetOverlaySwiping(false, instant: false);
    }

    private void CommitNext()
    {
        if (isSwiping) return;
        SetOverlayVisible(false, instant: true); // ✅ 휠/드래그 공통: 스와이프 시작 숨김
        StartCoroutine(CoCommit(next: true));
    }

    private void CommitPrev()
    {
        if (isSwiping) return;
        SetOverlayVisible(false, instant: true);  // ✅ 휠/드래그 공통
        StartCoroutine(CoCommit(next: false));
    }
    private IEnumerator CoCommit(bool next)
    {
        isSwiping = true;

        // ✅ next면 content 위로(+), prev면 아래로(-)
        float dir = next ? 1f : -1f;
        Vector2 from = pagesRoot ? pagesRoot.anchoredPosition : Vector2.zero;
        Vector2 to = new Vector2(0f, dir * viewH);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, swipeAnimDur);
            float s = Mathf.SmoothStep(0f, 1f, t);
            if (pagesRoot) pagesRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, s);
            yield return null;
        }

        if (next) MoveCursorNext();
        else MoveCursorPrev();

        if (next) RecycleAfterNext();
        else RecycleAfterPrev();

        if (pagesRoot) pagesRoot.anchoredPosition = Vector2.zero;

        isSwiping = false;
        SetOverlaySwiping(false, instant: false);
    }

    private void MoveCursorNext()
    {
        // 이미 히스토리 다음이 있으면 그대로 이동
        if (cursor < history.Count - 1)
        {
            cursor++;
            return;
        }

        // 새 랜덤 추가: ApplyImagesInstant에서 미리 뽑아둔 pending을 그대로 확정
        int idx = (_pendingNextIndex >= 0) ? _pendingNextIndex : PickRandomIndex();
        history.Add(idx);
        cursor = history.Count - 1;

        _pendingNextIndex = -1; // 확정했으니 비움
    }

    private void MoveCursorPrev()
    {
        if (cursor <= 0) return;
        cursor--;

        _pendingNextIndex = -1; // 뒤로 가면 ‘다음’은 history에 있을 가능성이 높으니 pending 폐기
    }

    private void ApplyImagesInstant()
    {
        if (pool == null || pool.Length == 0) return;

        int curr = history[cursor];
        int prev = (cursor > 0) ? history[cursor - 1] : curr;

        int next;
        if (cursor < history.Count - 1)
        {
            // 이미 히스토리에 다음이 존재
            next = history[cursor + 1];
            _pendingNextIndex = -1; // 이미 정해져 있으니 pending 필요 없음
        }
        else
        {
            // 아직 “다음”이 없으면 미리 1번만 뽑아서 pending에 고정
            if (_pendingNextIndex < 0) _pendingNextIndex = PickRandomIndex();
            next = _pendingNextIndex;
        }

        if (pagePrevImage) pagePrevImage.sprite = pool[prev];
        if (pageCurrImage) pageCurrImage.sprite = pool[curr];
        if (pageNextImage) pageNextImage.sprite = pool[next];
    }

    private int PickRandomIndex()
    {
        if (pool == null || pool.Length == 0) return 0;
        return Random.Range(0, pool.Length);
    }

    private void SetOverlayVisible(bool on, bool instant)
    {
        SetCanvasGroup(rightMenuGroup, on, instant);
        SetCanvasGroup(infoGroup, on, instant);
    }

    private void SetCanvasGroup(CanvasGroup cg, bool on, bool instant)
    {
        if (cg == null) return;

        float to = on ? 1f : 0f;

        if (instant)
        {
            cg.alpha = to;
            cg.interactable = on;
            cg.blocksRaycasts = on;
            return;
        }

        // 아주 짧게만
        StartCoroutine(CoFadeCG(cg, to, 0.08f, on));
    }

    private IEnumerator CoFadeCG(CanvasGroup cg, float to, float dur, bool enableAtEnd)
    {
        float from = cg.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur);
            float s = Mathf.SmoothStep(0f, 1f, t);
            cg.alpha = Mathf.Lerp(from, to, s);
            yield return null;
        }
        cg.alpha = to;
        cg.interactable = enableAtEnd;
        cg.blocksRaycasts = enableAtEnd;
    }

    private bool ScreenToLocal(Vector2 screenPos, Camera uiCam, out Vector2 local)
    {
        local = default;
        if (swipeViewport == null) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(swipeViewport, screenPos, uiCam, out local);
    }

    private void SetOverlaySwiping(bool swiping, bool instant)
    {
        if (swiping)
        {
            // ✅ 살짝 보이되 클릭은 막기
            SetCanvasGroupAlpha(rightMenuGroup, overlayHoverAlpha, instant, interactable: false);
            SetCanvasGroupAlpha(infoGroup, overlayHoverAlpha, instant, interactable: false);
        }
        else
        {
            // ✅ 정상 복귀
            SetCanvasGroupAlpha(rightMenuGroup, 1f, instant, interactable: true);
            SetCanvasGroupAlpha(infoGroup, 1f, instant, interactable: true);
        }
    }

    private void SetCanvasGroupAlpha(CanvasGroup cg, float alpha, bool instant, bool interactable)
    {
        if (cg == null) return;

        if (instant)
        {
            cg.alpha = alpha;
            cg.interactable = interactable;
            cg.blocksRaycasts = interactable; // ✅ 스와이프 중엔 레이캐스트 차단
            return;
        }

        StartCoroutine(CoFadeCG(cg, alpha, overlayFadeDur, interactable));
    }

    private void RecycleAfterNext()
    {
        if (pool == null || pool.Length == 0) return;
        // Prev <- Curr <- Next <- Prev(재사용)
        var oldPrev = pagePrevImage;
        pagePrevImage = pageCurrImage;
        pageCurrImage = pageNextImage;
        pageNextImage = oldPrev;

        // "새 Next" 스프라이트만 갱신
        int nextIdx;
        if (cursor < history.Count - 1)
        {
            nextIdx = history[cursor + 1];
            _pendingNextIndex = -1;
        }
        else
        {
            if (_pendingNextIndex < 0) _pendingNextIndex = PickRandomIndex();
            nextIdx = _pendingNextIndex;
        }

        if (pageNextImage) pageNextImage.sprite = pool[nextIdx];

        // 위치 재배치(+H/0/-H)
        SetupPageRects();
    }

    private void RecycleAfterPrev()
    {
        if (pool == null || pool.Length == 0) return;
        // Next <- Curr <- Prev <- Next(재사용)
        var oldNext = pageNextImage;
        pageNextImage = pageCurrImage;
        pageCurrImage = pagePrevImage;
        pagePrevImage = oldNext;

        // "새 Prev" 스프라이트만 갱신
        int prevIdx = (cursor > 0) ? history[cursor - 1] : history[cursor];
        if (pagePrevImage) pagePrevImage.sprite = pool[prevIdx];

        // 위치 재배치(+H/0/-H)
        SetupPageRects();
    }

    
}