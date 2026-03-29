using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DropdownItemVisual : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    [Header("Colors")]
   
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    [SerializeField] private Color normalText = Color.black;
    [SerializeField] private Color hoverText = Color.black;
    [SerializeField] private Color pressedText = Color.white;

    private void Awake()
    {
        ApplyNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        background.sprite = hoverSprite;
        label.color = hoverText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        background.sprite = pressedSprite;
        label.color = pressedText;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        background.sprite = hoverSprite;
        label.color = hoverText;
    }

    private void ApplyNormal()
    {
        background.sprite = normalSprite;
        label.color = normalText;
    }
}