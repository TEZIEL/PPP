using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollSFX : MonoBehaviour, IScrollHandler
{
    [SerializeField] private float cooldown = 0.3f; // 🔥 0.06 ~ 0.12 추천

    private float lastPlayTime;

    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.scrollDelta.y) < 0.01f)
            return;

        if (Time.unscaledTime - lastPlayTime < cooldown)
            return;

        lastPlayTime = Time.unscaledTime;

        SoundManager.Instance.PlayOS(OSSoundEvent.Scroll);
    }
}