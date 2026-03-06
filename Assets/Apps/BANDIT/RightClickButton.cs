using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class RightClickButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Action")]
    [SerializeField] private UnityEvent onRightClick;

    [Header("Text Color State")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color highlightedTextColor = Color.white;
    [SerializeField] private Color pressedTextColor = Color.white;

    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TMP_Text>(includeInactive: true);

        ApplyTextColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyTextColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        ApplyTextColor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ApplyTextColor();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ApplyTextColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        onRightClick?.Invoke();
    }

    private void ApplyTextColor()
    {
        if (targetText == null) return;

        if (isPressed)
            targetText.color = pressedTextColor;
        else if (isHovered)
            targetText.color = highlightedTextColor;
        else
            targetText.color = normalTextColor;
    }
}
