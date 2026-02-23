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

    [Header("Gallery View")]
    [SerializeField] private GameObject galleryView;        // GalleryView
    [SerializeField] private Button galleryButton;          // RightMenu/Gallery 버튼(선택)
    [SerializeField] private Button backButton;             // GalleryView/BackButton

    [Header("Sprites")]
    [SerializeField] private Sprite[] pool;                 // 랜덤으로 쓸 이미지 풀

    [Header("Swipe")]
    [Range(0.1f, 0.8f)]
    [SerializeField] private float commitThreshold01 = 0.30f; // 30%
    [SerializeField] private float swipeAnimDur = 0.12f;
    [SerializeField] private float wheelStep01 = 0.35f;        // 휠로도 넘기기(감각값)
    [SerializeField] private float wheelCooldown = 0.18f;

    [Header("Swipe Tuning")]
    [SerializeField] private float swipeDuration = 0.18f;     // 쑥 이동 애니 시간
    [SerializeField] private float dragSensitivity = 1.0f;    // 드래그가 얼마나 잘 따라오나(증폭)

    [SerializeField] private Button likeButton;
    [SerializeField] private TMP_Text likeCountText;
    [SerializeField] private TMP_Text dislikeCountText;


    private float _lastWheelTime;
    private int _likeCount;
    private int _dislikeCount;

    private void RefreshCounts()
    {
        if (likeCountText) likeCountText.text = _likeCount.ToString();
        if (dislikeCountText) dislikeCountText.text = _dislikeCount.ToString();
    }


    private Vector2 _startPos;
    private float viewH;
    private bool isSwiping;
    private Vector2 dragStartLocal;
    private float dragDeltaY; // +면 위로 드래그(다음), -면 아래로 드래그(이전)

    // 히스토리: 인덱스 기반
    private readonly List<int> history = new List<int>();
    private int cursor = 0; // history[cursor]가 현재

    // 외부에서 카운트 업데이트용
    public int LikeCount { get; private set; }
    public int DislikeCount { get; private set; }

    private void StartSwipe(Vector2 pos)
    {
        isSwiping = true;
        _startPos = pos;
    }

    private void Awake()
    {
        if (swipeViewport == null) swipeViewport = GetComponentInChildren<RectTransform>();
        viewH = swipeViewport.rect.height;

        // 최소 1장 세팅
        int first = PickRandomIndex();
        history.Clear();
        history.Add(first);
        cursor = 0;

        ApplyImagesInstant();

        SetOverlayVisible(true, instant: true);
        if (likeButton) likeButton.onClick.AddListener(Like);
        RefreshCounts();
        if (galleryButton) galleryButton.onClick.AddListener(OpenGallery);
        if (backButton) backButton.onClick.AddListener(CloseGallery);

        CloseGallery();
    }

    private void Like()
    {
        _likeCount++;
        RefreshCounts();
    }

    private void Dislike()
    {
        _dislikeCount++;
        RefreshCounts();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (swipeViewport != null)
            viewH = swipeViewport.rect.height;
    }

    // ====== 메뉴에서 호출 ======
    public void AddLike()
    {
        LikeCount++;
        // TODO: UI 텍스트 연결해뒀으면 여기서 갱신
    }

    public void AddDislike()
    {
        DislikeCount++;
        // TODO: UI 텍스트 연결해뒀으면 여기서 갱신
    }

    public void ResetCounts()
    {
        LikeCount = 0;
        DislikeCount = 0;
        // TODO: UI 텍스트 갱신
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

        if (y < 0f)
            GoNext();   // 다음 이미지
        else
            GoPrev();   // 이전 이미지

        _lastWheelTime = Time.unscaledTime;
    }

    // ====== 입력(드래그/휠) ======
    private void Update()
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;
        if (pool == null || pool.Length == 0) return;

        // 휠로도 넘기기
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            // 아래로 휠(-)이면 다음(내리기), 위로 휠(+)이면 이전(올리기) 취향대로 조절 가능
            if (wheel < 0f) CommitNext();
            else CommitPrev();
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
        SetOverlayVisible(false, instant: false);
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
        if (pagesRoot)
            pagesRoot.anchoredPosition = new Vector2(0f, dragDeltaY);
    }

    public void EndDrag()
    {
        if (isSwiping) return;
        if (galleryView != null && galleryView.activeSelf) return;

        float moved01 = Mathf.Abs(dragDeltaY) / Mathf.Max(1f, viewH);
        if (moved01 >= commitThreshold01)
        {
            // 아래로 드래그(음수)면 다음 / 위로 드래그(양수)면 이전 (원하는 감각대로)
            if (dragDeltaY < 0f) CommitNext();
            else CommitPrev();
        }
        else
        {
            // 원위치 스냅
            StartCoroutine(CoSnapBack());
        }
    }

    private IEnumerator CoSnapBack()
    {
        isSwiping = true;

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

        SetOverlayVisible(true, instant: false);
    }

    private void CommitNext()
    {
        if (isSwiping) return;
        StartCoroutine(CoCommit(next: true));
    }


    private void GoNext()
    {
        // TODO: 여기 안에 “드래그 스와이프 성공 시 다음으로 넘길 때” 쓰는
        // 인덱스 변경 + 이미지 적용 + 저장(있으면) 코드를 그대로 넣어줘
        CommitIndex(+1);
    }

    private void GoPrev()
    {
        CommitIndex(-1);
    }

    private void CommitIndex(int dir)
    {
        // ✅ 여기만 너 기존 코드로 채우면 됨.
        // 예시(너 프로젝트에 맞게 바꿔):
        // _index = (_index + dir + sprites.Length) % sprites.Length;
        // ApplyIndex();
        // SaveIndex();

        // ---- 너 기존 “이미지 바꾸는 코드”를 여기로 이동 ----
    }

    private void CommitPrev()
    {
        if (isSwiping) return;
        StartCoroutine(CoCommit(next: false));
    }

    private IEnumerator CoCommit(bool next)
    {
        isSwiping = true;

        // 목표 위치: 다음이면 화면 높이만큼 위로(또는 아래로) 쑥
        float dir = next ? -1f : 1f; // next면 아래로 넘긴다(음수)
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

        // 실제 데이터 이동
        if (next) MoveCursorNext();
        else MoveCursorPrev();

        // 페이지 이미지 재배치
        ApplyImagesInstant();

        // 원위치
        if (pagesRoot) pagesRoot.anchoredPosition = Vector2.zero;

        isSwiping = false;
        SetOverlayVisible(true, instant: false);
    }

    private void MoveCursorNext()
    {
        // 이미 히스토리 다음이 있으면 그대로 이동
        if (cursor < history.Count - 1)
        {
            cursor++;
            return;
        }

        // 새 랜덤 추가
        history.Add(PickRandomIndex());
        cursor = history.Count - 1;
    }

    private void MoveCursorPrev()
    {
        // 맨 앞이면 더 못 감(원하면 여기서 새로 “앞에도 랜덤 생성” 가능)
        if (cursor <= 0) return;
        cursor--;
    }

    private void ApplyImagesInstant()
    {
        if (pool == null || pool.Length == 0) return;

        int curr = history[cursor];
        int prev = (cursor > 0) ? history[cursor - 1] : curr;
        int next = (cursor < history.Count - 1) ? history[cursor + 1] : PickRandomIndex();

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
}