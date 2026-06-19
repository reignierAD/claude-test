# Raggler's Challenge — Unity3D WebGL Setup Guide

## Project Requirements
- Unity 2022 LTS or newer (WebGL build support module installed)
- TextMeshPro (install via Package Manager)

## Folder Layout
```
Assets/
  Scripts/        ← All C# scripts (already created)
  Sprites/        ← Add your card face sprites here (PNG, 256x256 recommended)
  Scenes/
    MainScene.unity
```

## Scene Setup (MainScene)

### 1. Card Prefab
Create `Assets/Prefabs/Card.prefab`:
- Add `SpriteRenderer` (default sprite = card back)
- Add `BoxCollider2D` (size ~1x1)
- Add `Card` script component
- Set Sorting Layer to "Cards"

### 2. GameManager GameObject
- Create empty GameObject named **GameManager**
- Attach: `GameManager`, `BoardGenerator`, `PowerUpManager`, `EndlessScoreTracker`
- Assign `cardPrefab` field → the Card prefab above
- Assign `cardSprites[]` → your card face sprites (8–12 sprites recommended)
- Set `threeStarTimes` and `twoStarTimes` arrays (10 entries each):
  - Example 3★ times: `[20, 25, 30, 35, 40, 45, 50, 55, 60, 70]` (seconds)
  - Example 2★ times: `[40, 50, 60, 70, 80, 90, 100, 110, 120, 140]`

### 3. ClearingZone GameObject
- Create empty GameObject named **ClearingZone** at bottom of screen (Y ≈ -4)
- Attach `ClearingZone` script
- Create 7 child empty GameObjects named Slot0–Slot6, spaced 1.1 units apart on X
- Assign them to `slotTransforms[]`

### 4. UI Canvas (Screen Space - Overlay)
Create the following UI elements and wire them to `UIManager`:

| UI Element | Field in UIManager |
|---|---|
| TextMeshPro — timer | `timerText` |
| 3× Image (stars) | `starImages[]` |
| TextMeshPro — slot count | `slotCountText` |
| TextMeshPro — remove charges | `removeCountText` |
| TextMeshPro — undo charges | `undoCountText` |
| TextMeshPro — refresh charges | `refreshCountText` |
| Panel — Stage Select | `stageSelectPanel` |
| Panel — Result | `resultPanel` |
| Panel — Game Over | `gameOverPanel` |
| 10× Button (stages) | `stageButtons[]` |

**Power-Up Buttons** — attach `PowerUpManager.UseRemove()` / `UseUndo()` / `UseRefresh()`
to their respective Button `OnClick` events.

**Stage Buttons** — each calls `GameManager.Instance.StartStage(i)` with its index.

**Endless Button** — calls `GameManager.Instance.StartEndless()`

### 5. Camera
- Orthographic, Size = 5
- Position (0, 0, -10)

## WebGL Build Settings
1. File → Build Settings → Switch Platform → **WebGL**
2. Player Settings:
   - Resolution: 1280 × 720 (or 960 × 540 for mobile-friendly)
   - WebGL Template: Default
   - Compression: Gzip
3. Build → select output folder

## Card Sprite Suggestions
Use 8–12 distinct monster/item images (like the originals):
Poring, Lunatic, Fabre, Drops, Poporing, Smokie, Yoyo, Thief Bug Egg

Each sprite should have:
- Pivot: Center
- Pixels Per Unit: 100
- Filter Mode: Bilinear
- Compression: None (for crisp look at small sizes)
