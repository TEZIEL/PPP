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
    [SerializeField] private bool fillTopToBottom = true;

    public enum DesktopLayoutMode { Free, Grid }

    [SerializeField] private DesktopLayoutMode layoutMode = DesktopLayoutMode.Free;
    public DesktopLayoutMode LayoutMode => layoutMode;

    public void ToggleLayoutMode()
    {
        layoutMode = (layoutMode == DesktopLayoutMode.Free) ? DesktopLayoutMode.Grid : DesktopLayoutMode.Free;
        Debug.Log($"[Desktop] LayoutMode = {layoutMode}");
    }

    public void SnapAllIfGridMode()
    {
        if (LayoutMode != DesktopLayoutMode.Grid) return;
        ResolveAllGridOverlaps(); // ✅ “전체 스냅”도 겹침 방지 루트로 통일
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

    // ✅ DesktopIconDraggable이 요구하던 함수(컴파일 에러 방지)
    public Vector2 GetNearestSlotPosition(Vector2 currentAnchoredPos)
    {
        int idx = GetNearestSlotIndex(currentAnchoredPos);
        if (idx < 0) return currentAnchoredPos;

        var slots = GetSlots();
        return (idx >= 0 && idx < slots.Count) ? slots[idx] : currentAnchoredPos;
    }

    // =========================
    // ✅ 핵심: "겹침 방지 스냅"
    // =========================
    public void SnapIconToGridNoOverlap(DesktopIconDraggable icon)
    {
        if (LayoutMode != DesktopLayoutMode.Grid) return;
        if (iconsRoot == null || icon == null) return;

        var slots = GetSlots();
        if (slots.Count == 0) return;

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);

        // 1) 현재 점유중인 슬롯 수집(자기 자신 제외)
        var used = new bool[slots.Count];
        for (int i = 0; i < icons.Length; i++)
        {
            var other = icons[i];
            if (other == null || other == icon) continue;

            int otherIdx = GetNearestSlotIndex(other.GetRect().anchoredPosition);
            if (otherIdx >= 0 && otherIdx < used.Length)
                used[otherIdx] = true;
        }

        // 2) 내가 가고 싶은 슬롯
        int desired = GetNearestSlotIndex(icon.GetRect().anchoredPosition);
        int chosen = FindNearestFreeSlotIndex(desired, used);

        if (chosen < 0) return;

        Rect allowed = DesktopBounds.GetAllowedRect(iconsRoot);
        var r = icon.GetRect();
        r.anchoredPosition = DesktopBounds.ClampAnchoredPosition(slots[chosen], r, allowed);
    }

    // ✅ 저장 로드/해상도 변경 등으로 "이미 겹쳐있는 상태"를 정리
    public void ResolveAllGridOverlaps()
    {
        if (LayoutMode != DesktopLayoutMode.Grid) return;
        if (iconsRoot == null) return;

        var slots = GetSlots();
        if (slots.Count == 0) return;

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        if (icons == null || icons.Length == 0) return;

        // “현재 위치 기준”으로 정렬해서, 유저가 배치한 느낌을 최대한 유지
        System.Array.Sort(icons, (a, b) =>
        {
            var pa = a.GetRect().anchoredPosition;
            var pb = b.GetRect().anchoredPosition;

            // 위→아래(=y 큰 게 먼저), 왼→오(=x 작은 게 먼저)
            int y = pb.y.CompareTo(pa.y);
            return (y != 0) ? y : pa.x.CompareTo(pb.x);
        });

        var used = new bool[slots.Count];
        Rect allowed = DesktopBounds.GetAllowedRect(iconsRoot);

        foreach (var ic in icons)
        {
            if (ic == null) continue;

            int desired = GetNearestSlotIndex(ic.GetRect().anchoredPosition);
            int chosen = FindNearestFreeSlotIndex(desired, used);
            if (chosen < 0) break;

            used[chosen] = true;

            var r = ic.GetRect();
            r.anchoredPosition = DesktopBounds.ClampAnchoredPosition(slots[chosen], r, allowed);
        }

        windowManager?.SaveOS();
    }

    private int FindNearestFreeSlotIndex(int desired, bool[] used)
    {
        if (used == null || used.Length == 0) return -1;

        desired = Mathf.Clamp(desired, 0, used.Length - 1);

        if (!used[desired]) return desired;

        // 가까운 빈 슬롯을 양방향으로 탐색
        for (int offset = 1; offset < used.Length; offset++)
        {
            int right = desired + offset;
            if (right < used.Length && !used[right]) return right;

            int left = desired - offset;
            if (left >= 0 && !used[left]) return left;
        }

        return -1;
    }

    // =========================
    // 기존 “정렬”도 유지 가능
    // (정렬은 애초에 1칸씩 배치라 겹칠 일이 거의 없음)
    // =========================
    public void AlignIconsToGrid()
    {
        if (iconsRoot == null) return;

        var icons = iconsRoot.GetComponentsInChildren<DesktopIconDraggable>(true);
        if (icons == null || icons.Length == 0) return;

        List<Vector2> slots = GetSlots();
        if (slots.Count == 0) return;

        // 안정성: id 기준
        System.Array.Sort(icons, (a, b) => string.CompareOrdinal(a.GetId(), b.GetId()));

        int count = Mathf.Min(icons.Length, slots.Count);
        Rect allowed = DesktopBounds.GetAllowedRect(iconsRoot);

        for (int i = 0; i < count; i++)
        {
            RectTransform r = icons[i].GetRect();
            r.anchoredPosition = DesktopBounds.ClampAnchoredPosition(slots[i], r, allowed);
        }

        windowManager?.SaveOS();
    }

    private static List<Vector2> BuildSlots(RectTransform desktopRect, Vector2 cell, Vector2 gap, bool topToBottom)
    {
        var slots = new List<Vector2>(128);
        if (desktopRect == null) return slots;

        Rect allowed = DesktopBounds.GetAllowedRect(desktopRect);

        float stepX = cell.x + gap.x;
        float stepY = cell.y + gap.y;

        int cols = Mathf.FloorToInt((allowed.width + gap.x) / stepX);
        int rows = Mathf.FloorToInt((allowed.height + gap.y) / stepY);

        if (cols <= 0 || rows <= 0) return slots;

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