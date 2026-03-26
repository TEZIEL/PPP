using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PPP.BLUE.VN
{
    public sealed class VNFadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fadeImage;
        [SerializeField] private bool raycastBlockWhenVisible = true;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>(true);

            if (fadeImage == null)
                fadeImage = GetComponentInChildren<Image>(true);

            SetAlpha(0f);
        }

        public IEnumerator FadeOut(float duration)
        {
            yield return FadeTo(1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            yield return FadeTo(0f, duration);
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
                return;

            var clamped = Mathf.Clamp01(alpha);
            canvasGroup.alpha = clamped;

            bool block = raycastBlockWhenVisible && clamped > 0.001f;
            canvasGroup.blocksRaycasts = block;
            canvasGroup.interactable = block;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (canvasGroup == null)
                yield break;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (duration <= 0f)
            {
                SetAlpha(target);
                yield break;
            }

            float from = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(from, target, t));
                yield return null;
            }

            SetAlpha(target);
        }
    }
}
