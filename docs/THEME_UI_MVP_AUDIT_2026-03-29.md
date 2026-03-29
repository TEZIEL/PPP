# UI Theme MVP Audit (2026-03-29)

## Scope
- Scene: `Assets/OS_Prototype.unity`
- OS UI scripts/prefabs for desktop background, desktop icons, window frame, taskbar/status area.

## Key findings
1. **Window frame skin entrypoint already exists**
   - `WindowController.SetActiveVisual()` applies `UISkin` sprites + tint for title/under bars.
   - `P_WindowFrame_Base.prefab` already wires `skin`, `titleBarImage`, `underImage`.

2. **Desktop/taskbar has many hardcoded `Image.color` values in scene/prefab**
   - `BackgroundBG` in scene has custom gray tint with a sprite.
   - `P_DesktopBackground` prefab is solid color image (no sprite).
   - `P_TaskbarBG` prefab has fixed green tint.
   - `TB_AppButton_Pressed` root image has fixed reddish tint.

3. **Desktop icon tint is script-driven but per-instance serialized values exist**
   - `DesktopIconLauncher` has serialized `normalIconColor` / `selectedIconColor` and applies `iconImage.color`.
   - Multiple icon objects in scene each serialize same color values, increasing manual edit risk if values diverge later.

4. **Taskbar buttons and windows are dynamically instantiated**
   - `TaskbarManager.Add()` instantiates taskbar buttons at runtime.
   - `WindowManager.Open()` instantiates window prefab and content at runtime.
   - Therefore one-shot scene-only recolor logic can miss runtime-created UI.

5. **App icons are per-app sprite references**
   - `AppDefinition` assets point to dedicated `windowIconSprite`/`taskbarIconSprite` per app.
   - This is fine for palette-swap workflows, but if source icons are not monochrome some assets may need redraw/preprocessing.

## MVP recommendation
- Add one `ThemeData` ScriptableObject with:
  - `desktopBg`, `desktopHighlight`, `iconNormal`, `iconSelected`, `windowTint`, `taskbarBg`, `statusBar`.
- Add one small `ThemeManager` + lightweight applier components:
  - `ThemeImageRole` (for static background/taskbar/status/image tint targets).
  - `DesktopIconLauncher` reads icon colors from `ThemeManager` (fallback to existing serialized values).
  - `WindowController` uses theme tint in `SetActiveVisual()` (can keep `UISkin` sprites for active/inactive).
  - `TaskbarButtonController` optionally applies icon/button tint on `Initialize()`.
- Trigger apply on:
  - scene load / theme change
  - after `TaskbarManager.Add()` and `WindowManager.Open()` to cover runtime instances.

## Final assessment
- **Classification**: "몇 군데만 수정하면 가능".
- MVP target (1~2 days) is realistic if limited to OS shell UI (desktop/taskbar/window frame/icon tint).
