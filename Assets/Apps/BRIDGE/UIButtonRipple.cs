using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIButtonRipple : MonoBehaviour
{
    [SerializeField] private RectTransform ripplePrefab;
    [SerializeField] private float duration = 0.22f;
    [SerializeField] private float maxScale = 1.8f;

    public void Play(RectTransform parent)
    {
        if (ripplePrefab == null || parent == null) return;
        var rt = Instantiate(ripplePrefab, parent);
        rt.SetAsLastSibling();
        StartCoroutine(CoRipple(rt));
    }

    private IEnumerator CoRipple(RectTransform rt)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.6f;
        cg.alpha = 0.35f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float s = Mathf.SmoothStep(0, 1, t);
            rt.localScale = Vector3.one * Mathf.Lerp(0.6f, maxScale, s);
            cg.alpha = Mathf.Lerp(0.35f, 0f, s);
            yield return null;
        }

        Destroy(rt.gameObject);
    }
}