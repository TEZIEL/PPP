using UnityEngine;

public static class DesktopBounds
{
    public const float Margin = 48f;
    public const float TaskbarHeight = 54f;

    // desktopRect(=DesktopIconBG) 기준 로컬 rect
    public static Rect GetAllowedRect(RectTransform desktopRect)
    {
        Rect r = desktopRect.rect;

        float left = r.xMin + Margin;
        float right = r.xMax - Margin;
        float top = r.yMax - Margin;

        // 하단: margin + taskbar
        float bottom = r.yMin + Margin + TaskbarHeight;

        return Rect.MinMaxRect(left, bottom, right, top);
    }

    // pivot/size 고려 clamp
    public static Vector2 ClampAnchoredPosition(Vector2 anchoredPos, RectTransform iconRect, Rect allowed)
    {
        Vector2 size = iconRect.rect.size;
        Vector2 pivot = iconRect.pivot;

        float minX = allowed.xMin + size.x * pivot.x;
        float maxX = allowed.xMax - size.x * (1f - pivot.x);

        float minY = allowed.yMin + size.y * pivot.y;
        float maxY = allowed.yMax - size.y * (1f - pivot.y);

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        return anchoredPos;
    }
}

