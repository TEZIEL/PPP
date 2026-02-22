using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UIButtonYouTubeFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Highlight (hover/pressed circle)")]
    [SerializeField] private Image highlightImage;      // 버튼 안의 원형 이미지
    [SerializeField] private CanvasGroup highlightCg;   // 알파 컨트롤
    [SerializeField] private float hoverAlpha = 0.12f;
    [SerializeField] private float pressedAlpha = 0.22f;
    [SerializeField] private float fadeDur = 0.08f;

    [Header("Click Ripple (optional)")]
    [SerializeField] private UIButtonRipple ripple;     // 네가 만든 리플 스크립트
    [SerializeField] private RectTransform rippleParent; // 보통 버튼 RectTransform

    private Coroutine _fadeCo;
    private bool _hover;
    private bool _pressed;

    private void Awake()
    {
        if (highlightImage == null)
        {
            // 버튼 자식에서 자동 탐색(원형 이미지 하나 만들어두면 잘 잡힘)
            highlightImage = GetComponentInChildren<Image>(includeInactive: true);
        }

        if (highlightImage != null && highlightCg == null)
            highlightCg = highlightImage.GetComponent<CanvasGroup>();

        if (highlightCg == null && highlightImage != null)
            highlightCg = highlightImage.gameObject.AddComponent<CanvasGroup>();

        if (highlightCg != null)
        {
            highlightCg.alpha = 0f;
            highlightCg.blocksRaycasts = false;
            highlightCg.interactable = false;
        }

        if (rippleParent == null) rippleParent = transform as RectTransform;

        // Button 클릭에 리플 연결(원하면)
        var btn = GetComponent<Button>();
        if (btn != null && ripple != null)
        {
            btn.onClick.AddListener(() => ripple.Play(rippleParent));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hover = true;
        UpdateTarget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hover = false;
        _pressed = false;
        UpdateTarget();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _pressed = true;
        UpdateTarget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        if (highlightCg == null) return;

        float target = 0f;
        if (_pressed) target = pressedAlpha;
        else if (_hover) target = hoverAlpha;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(target));
    }

    private IEnumerator CoFade(float to)
    {
        float from = highlightCg.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDur);
            float s = Mathf.SmoothStep(0f, 1f, t);
            highlightCg.alpha = Mathf.Lerp(from, to, s);
            yield return null;
        }

        highlightCg.alpha = to;
        _fadeCo = null;
    }
}