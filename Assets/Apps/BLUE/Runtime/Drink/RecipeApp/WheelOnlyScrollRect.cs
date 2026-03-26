using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ScrollRect의 드래그 스크롤만 막고,
/// 마우스 휠 스크롤은 그대로 허용하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class WheelOnlyScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect scrollRect;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작 무시
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중 무시
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 무시
    }
}