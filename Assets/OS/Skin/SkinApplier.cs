using UnityEngine;
using UnityEngine.UI;

public class SkinApplier : MonoBehaviour
{
    [SerializeField] private UISkin skin;
    [SerializeField] private Graphic targetGraphic;

    public void Apply(bool isActive)
    {
        if (targetGraphic == null || skin == null)
        {
            return;
        }

        targetGraphic.color = isActive ? skin.activeColor : skin.inactiveColor;
    }
}
