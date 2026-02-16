using System.Collections.Generic;
using UnityEngine;

public class DesktopGridManager : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private WindowManager windowManager;
    [SerializeField] private RectTransform iconsRoot; // DesktopIconBG

    [Header("Grid Settings")]
    [SerializeField] private Vector2 cellSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 spacing = new Vector2(48f, 48f);

    // 좌상단부터 정렬할지(윈도우 감성), 아니면 좌하단부터 할지 선택
    [SerializeField] private bool fillTopToBottom = true;

    public enum DesktopLayoutMode { Free, Grid }

    [SerializeField] private DesktopLayoutMode layoutMode = DesktopLayoutMode.Free;
    public DesktopLayoutMode LayoutMode => layoutMode;

    public void ToggleLayoutMode()
    {
        layoutMode = (layoutMode == DesktopLayoutMode.Free) ? DesktopLayoutMode.Grid : DesktopLayoutMode.Free;
        Debug.Log($"[Desktop] LayoutMode = {layoutMode}");
    }

    public List<Vector2> GetSlots()
    {
        return BuildSlots(iconsRoot, cellSize, spacing, fillTopToBottom);
    }

    public int GetNearestSlotIndex(Vector2 pos)
    {
        var slots = GetSlots();
        if (slots.Count == 0) return -1;

        int best = 0;
        float bestDist = (slots[0] - pos).sqrMagnitude;
        for (int i = 1; i < slots.Count; i++)
        {
            float d = (slots[i] - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    public Vector2 GetSlotPos(int index, Vector2 fallback)
    {
        var slots = GetSlots();
        if (index < 0 || index >= slots.Count) return fallback;
        return slots[index];
    }


    public Vector2 GetNearestSlotPosition(Vector2 currentAnchoredPos)
    {
        if (iconsRoot == null) return currentAnchoredPos;

        var slots = BuildSlots(iconsRoot, cellSize, spacing, fillTopToBottom);
        if (slots.Count == 0) return currentAnchoredPos;

        Vector2 best = slots[0];
        float bestDist = (best - currentAnchoredPos).sqrMagnitude;

        for (int i = 1; i < slots.Count; i++)
        {
            float d = (slots[i] - currentAnchoredPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = slots[i];
            }
        }

        return best;
    }


    public void AlignIconsToGrid()
    {
        if (iconsRoot == null) return;

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        if (icons == null || icons.Length == 0) return;

        // 슬롯 생성
        List<Vector2> slots = BuildSlots(iconsRoot, cellSize, spacing, fillTopToBottom);
        if (slots.Count == 0) return;

        // 아이콘 정렬: 안정성을 위해 id 기준 정렬(원하면 위치 기준으로 바꿔도 됨)
        System.Array.Sort(icons, (a, b) => string.CompareOrdinal(a.GetId(), b.GetId()));

        int count = Mathf.Min(icons.Length, slots.Count);
        for (int i = 0; i < count; i++)
        {
            RectTransform r = icons[i].GetRect();
            r.anchoredPosition = DesktopBounds.ClampAnchoredPosition(slots[i], r, DesktopBounds.GetAllowedRect(iconsRoot));
        }

        // 정렬 결과 저장 덮어쓰기
        if (windowManager != null) windowManager.SaveOS();
    }

    private static List<Vector2> BuildSlots(RectTransform desktopRect, Vector2 cell, Vector2 gap, bool topToBottom)
    {
        var slots = new List<Vector2>(128);

        Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);

        float stepX = cell.x + gap.x;
        float stepY = cell.y + gap.y;

        // 슬롯 개수 계산
        int cols = Mathf.FloorToInt((allowed.width + gap.x) / stepX);
        int rows = Mathf.FloorToInt((allowed.height + gap.y) / stepY);

        if (cols <= 0 || rows <= 0) return slots;

        // 좌상단 기준 첫 슬롯 pivot 위치 계산(기본 pivot 0.5,0.5 기준)
        // 아이콘 pivot이 0.5,0.5라고 가정(대부분 그럴 거라 문제 없음)
        float startX = allowed.xMin + cell.x * 0.5f;
        float startY = topToBottom
            ? allowed.yMax - cell.y * 0.5f
            : allowed.yMin + cell.y * 0.5f;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                float x = startX + c * stepX;
                float y = topToBottom ? (startY - r * stepY) : (startY + r * stepY);
                slots.Add(new Vector2(x, y));
            }
        }

        return slots;
    }
}
