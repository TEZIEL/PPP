using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace PPP.BLUE.VN
{
    public sealed class VNTextTyper : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private float charsPerSecond = 40f;

        public bool IsTyping { get; private set; }
        
        private Coroutine co;
        private string fullTextCache = "";
        private Action onCompletedCache;

        public void SetTarget(TMP_Text t) => target = t;

        public void StartTyping(string fullText, Action onCompleted)
        {
            if (target == null)
            {
                Debug.LogError("[VNTextTyper] target TMP_Text is null.");
                return;
            }

            StopTyping(); // 기존 타이핑 정리
            fullTextCache = fullText ?? "";
            onCompletedCache = onCompleted;

            co = StartCoroutine(CoType(fullTextCache));
        }

        public void ForceComplete()
        {
            if (target == null) return;

            if (co != null)
            {
                StopCoroutine(co);
                co = null;
            }

            target.text = fullTextCache;
            IsTyping = false;

            // ✅ 콜백은 한 번만 호출되게 null 처리
            var cb = onCompletedCache;
            onCompletedCache = null;
            cb?.Invoke();
        }

        public void SkipToEnd()
        {
            if (!IsTyping || target == null) return;

            // 코루틴 종료하고 전체 텍스트 즉시 출력
            StopCoroutine(co);
            co = null;
            IsTyping = false;

            // 이미 코루틴에서 target.text를 누적했을 수 있으니,
            // 마지막 fullText를 저장하는 방식도 가능하지만, 여기선 간단히 처리:
            // => SkipToEnd를 부르는 쪽에서 "현재 풀텍스트"를 다시 Set하는 방식이 더 안전함
        }

        public void StopTyping()
        {
            if (co != null)
            {
                StopCoroutine(co);
                co = null;
            }
            IsTyping = false;
            onCompletedCache = null;
            fullTextCache = "";
        }

        private IEnumerator CoType(string fullText)
        {
            IsTyping = true;
            target.text = "";

            if (fullText.Length == 0)
            {
                IsTyping = false;
                var cb0 = onCompletedCache;
                onCompletedCache = null;
                cb0?.Invoke();
                yield break;
            }

            float interval = 1f / Mathf.Max(1f, charsPerSecond);

            for (int i = 0; i < fullText.Length; i++)
            {
                target.text += fullText[i];
                yield return new WaitForSeconds(interval);
            }

            co = null;
            IsTyping = false;

            var cb = onCompletedCache;
            onCompletedCache = null;
            cb?.Invoke();
        }
    }
}