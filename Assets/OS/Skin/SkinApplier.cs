using UnityEngine;
using UnityEngine.UI;

public class SkinApplier : MonoBehaviour
{
    [SerializeField] private UISkin skin;

    [Header("Targets")]
    [SerializeField] private Graphic titleBarGraphic; // TitleBar Image µî
    [SerializeField] private Graphic frameGraphic;    // WindowFrame Image µî
    [SerializeField] private Graphic underGraphic;

    public void Apply(bool isActive)
    {
        if (skin == null) return;

        if (titleBarGraphic != null)
            titleBarGraphic.color = isActive ? skin.titleActiveColor : skin.titleInactiveColor;

        if (frameGraphic != null)
            frameGraphic.color = isActive ? skin.frameActiveColor : skin.frameInactiveColor;

        if (underGraphic != null)
            underGraphic.color = isActive ? skin.underActiveColor : skin.underInactiveColor;
    }
}
