using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class AppUIThemeApplierBase : MonoBehaviour
{
    protected static void ApplyImageSlot(Image target, Color color, Sprite sprite = null)
    {
        if (target == null) return;
        target.color = color;
        if (sprite != null)
            target.sprite = sprite;
    }

    protected static void ApplyTextSlot(TMP_Text target, Color color)
    {
        if (target == null) return;
        target.color = color;
    }

    protected static void ApplyButtonColors(Button button, Color normal, Color textColor)
    {
        if (button == null) return;

        var c = button.colors;
        c.normalColor = normal;
        c.highlightedColor = normal;
        c.selectedColor = normal;
        c.pressedColor = normal * 0.9f;
        c.disabledColor = normal * 0.6f;
        button.colors = c;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = textColor;
    }

    protected static T FindInChildrenByName<T>(Transform root, string name) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name)
            {
                var component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }

            var nested = FindInChildrenByName<T>(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    public abstract void ApplyFromManager(AppUIThemeData data, string appId);
}
