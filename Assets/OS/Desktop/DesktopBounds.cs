using UnityEngine;

public static class DesktopBounds
{
    public const float MarginX = 48f;   // 좌우
    public const float MarginY = 54f;   // 상하
    public const float TaskbarHeight = 54f;

    public static Rect GetAllowedRect(RectTransform desktopRect)
    {
        Rect r = desktopRect.rect;

        float left = r.xMin + MarginX;
        float right = r.xMax - MarginX;

        float top = r.yMax - MarginY;
        float bottom = r.yMin + MarginY + TaskbarHeight;

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

