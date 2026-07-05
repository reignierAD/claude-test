# Raggler's Challenge — Unity Replica

A Unity replica of the **"Raggler's Challenge"** mini-game from *Ragnarok Origin*
(the popular *triple-tile matching* genre, as seen in 3 Tiles / Zen Match /
Sheep a Sheep). Not a pixel-perfect copy — the mechanics, item system, star
rating and scoring are all there, with placeholder art generated entirely at
runtime.

## Requirements

- **Unity 6** (6000.0 or newer — including the latest Unity 6.x releases).
  Older editors from 2022.3 LTS onward should also work.
- No packages beyond the built-in **uGUI** (already in the manifest).
- No art, prefabs or scene wiring: every sprite and UI element is generated
  from code when you press Play.

## How to run

1. Open the project folder with Unity Hub (add it as an existing project).
   If your editor is newer than the pinned version, let Unity upgrade it.
2. Open `Assets/Scenes/Main.unity` (or literally any scene — even an empty
   new one works, the game bootstraps itself at runtime).
3. Press **Play**.

## How to play

- **Tap a card** to move it into the **clearing zone** at the bottom.
  Cards covered by a higher layer are darkened and can't be picked.
- **Three identical cards** in the clearing zone clear automatically.
- The clearing zone holds **7 cards max** — go over and you **lose**.
- Clear every card on the board to win the stage.

### Items (right panel)

| Item | Effect |
|---|---|
| **Remove** | Moves the first 3 cards of the clearing zone to a holding area (bottom-left). Tap a held card to put it back. |
| **Undo** | Returns the last card you played from the zone back to its board spot. |
| **Refresh** | Reshuffles the kinds of all cards still on the board. |

Each item is limited to **10 uses per stage** and consumes inventory
(you start with 37 / 5 / 23 and earn +1 of each per stage cleared).

### Rating & scoring

- **Stars (per stage):** based on completion time — finish under the stage's
  fast threshold for ★★★, under the slow threshold for ★★, otherwise ★.
  The three stars at the top of the HUD dim in real time as thresholds pass.
- **Score:** 30 points per cleared triple, multiplied by a **combo** (up to
  ×5) when matches land within 4 seconds of each other, plus a time bonus
  on stage clear (10 pts per second under the 2-star threshold).
- **Endless mode:** boards keep coming and get harder each round (more cards,
  kinds and layers); the clearing zone carries over between rounds. Each
  cleared round pays a rising bonus. Best score is saved.

### Progression

- 10 stages; a stage unlocks once the previous one is cleared.
- Stars, best endless score and item inventory persist via `PlayerPrefs`.
- A **Reset Progress** button sits in the bottom-left of the menu.

## Project layout

```
Assets/
  Scenes/Main.unity        Empty scene — everything is built at runtime
  Scripts/
    Bootstrap.cs           Creates camera, EventSystem, canvas, controller
    GameConfig.cs          All tuning: card kinds, stage table, scoring rules
    GameController.cs      Menu, HUD, popups, game flow, items, persistence
    Board.cs               Layered layout generation + coverage (blocking)
    Tray.cs                Clearing zone: grouping, triple detection, overflow
    Card.cs                Card visuals + click forwarding
    SpriteFactory.cs       Runtime-generated sprites (rounded rect, circle, star)
    Ui.cs                  Small uGUI builder helpers
    Tween.cs               Minimal coroutine tween helper
```

## Tuning

Everything lives in `GameConfig.cs`: stage sizes/layers/kinds, star time
thresholds, tray capacity, item caps, score values, endless difficulty curve.
Swap the runtime placeholder art by giving `Card.Create` real sprites.
