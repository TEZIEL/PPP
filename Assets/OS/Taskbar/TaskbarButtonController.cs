using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TaskbarButtonController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private WindowController targetWindow;
    [SerializeField] private Button button;

    [Header("Visual")]
    [SerializeField] private Image background;
    [SerializeField] private Sprite recessedSprite;
    [SerializeField] private Sprite raisedSprite;

    [Header("Label (Optional)")]
    [SerializeField] private TMPro.TMP_Text titleText; // 있으면 표시이름 표시

    [Header("Debug/Identity")]
    [SerializeField] private string appId;

    [Header("Remove Animation")]
    [SerializeField] private float removeDuration = 0.12f;

    private bool _listenerHooked;

    private CanvasGroup _cg;
    private LayoutElement _le;
    private Coroutine _removeCo;
    private float _initialWidth = -1f;

    public RectTransform Rect => (RectTransform)transform;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();

        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        _le = GetComponent<LayoutElement>();
        if (_le == null) _le = gameObject.AddComponent<LayoutElement>();

        HookListener();
    }

    private void OnEnable() => HookListener();

    private void OnDisable()
    {
        UnhookListener();
        if (_removeCo != null) { StopCoroutine(_removeCo); _removeCo = null; }
    }

    private void OnDestroy() => UnhookListener();

    private void HookListener()
    {
        if (button == null) return;

        // 중복 방지
        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);
        _listenerHooked = true;
    }

    private void UnhookListener()
    {
        if (!_listenerHooked) return;
        if (button == null) return;

        button.onClick.RemoveListener(OnClick);
        _listenerHooked = false;
    }

    // ✅ TaskbarManager.Add에서 호출 (표시이름까지 주입)
    public void Initialize(string id, string displayName, WindowManager manager, WindowController window)
    {
        appId = id;
        windowManager = manager;
        targetWindow = window;

        if (titleText != null) titleText.text = displayName;
    }

    public void SetMinimizedVisual(bool minimized)
    {
        if (background == null) return;
        if (recessedSprite == null || raisedSprite == null) return;

        background.sprite = minimized ? raisedSprite : recessedSprite;
    }

    private void OnClick()
    {
        if (windowManager == null) return;
        if (targetWindow == null) return;

        string id = !string.IsNullOrEmpty(targetWindow.AppId) ? targetWindow.AppId : appId;
        if (string.IsNullOrEmpty(id)) return;

        if (targetWindow.IsMinimized)
        {
            windowManager.Restore(id);
            return;
        }

        if (windowManager.ActiveAppId != id)
        {
            windowManager.Focus(id);
            return;
        }

        windowManager.Minimize(id);
    }

    // ✅ 핵심: “폭을 0으로 줄여서” 레이아웃이 부드럽게 재배치되게 함
    public void PlayCloseReflow(Action onDone)
    {
        if (_removeCo != null) return; // 중복 호출 방지

        // 클릭/레이캐스트 막아두기
        if (_cg != null)
        {
            _cg.interactable = false;
            _cg.blocksRaycasts = false;
        }

        // 초기 폭 캐시(한 번만)
        if (_initialWidth < 0f)
        {
            var rt = (RectTransform)transform;
            _initialWidth = Mathf.Max(1f, rt.rect.width);
        }

        _removeCo = StartCoroutine(CoCloseReflow(onDone));
    }

    private IEnumerator CoCloseReflow(Action onDone)
    {
        float dur = Mathf.Max(0.01f, removeDuration);

        float t = 0f;
        float w0 = _initialWidth;
        float a0 = 1f;

        if (_le != null)
        {
            // 레이아웃이 확실히 폭을 보게끔
            _le.preferredWidth = w0;
            _le.flexibleWidth = 0f;
        }

        if (_cg != null) _cg.alpha = 1f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float s = Mathf.SmoothStep(0f, 1f, t);

            if (_le != null) _le.preferredWidth = Mathf.Lerp(w0, 0f, s);
            if (_cg != null) _cg.alpha = Mathf.Lerp(a0, 0f, s);

            yield return null;
        }

        if (_le != null) _le.preferredWidth = 0f;
        if (_cg != null) _cg.alpha = 0f;

        _removeCo = null;
        onDone?.Invoke();
    }
}