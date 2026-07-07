# CLAUDE.md — Raggler's Challenge (Unity replica)

Persistent context for Claude Code sessions in this repo. Read this first, then
skim recent `git log` — commit messages here are intentionally detailed and are
the authoritative record of *what* changed and *why*.

## What this project is

A **Unity 6** replica of "Raggler's Challenge" from *Ragnarok Origin* — the
triple-tile matching genre (3 Tiles / Zen Match / Sheep-a-Sheep). Board of
layered cards; tap uncovered cards into a 7-slot clearing zone; three identical
cards auto-clear; tray overflow = lose. Plus Remove/Undo/Refresh items, a
time-based 3-star rating, an Endless mode, and a **Web3 layer** (MetaMask login
on **BSC Testnet**, stars → an ERC-20 token "RST", Moralis for balance reads).

Built as a **school project**; blockchain honesty caveats (client-reported star
counts, client-side API key) are documented in `BLOCKCHAIN_SETUP.md` and are
acceptable for a testnet demo.

## Hard architectural facts (do not break these)

- **100% code-built UI. No prefabs, no scene wiring, no imported art required.**
  `Assets/Scenes/Main.unity` is essentially empty. `Bootstrap.cs` runs via
  `[RuntimeInitializeOnLoadMethod]` and creates the camera, EventSystem, Canvas
  (`CanvasScaler` reference resolution **1600×900**), the `Web3Bridge`, and the
  `GameController`. Pressing Play in *any* scene runs the whole game.
- **All sprites are generated at runtime** in `SpriteFactory` (rounded rects,
  circle, star, arrow chevron, sunburst rays). No texture assets exist.
- **Designer data lives in a ScriptableObject**: `Assets/Resources/GameSettings.asset`.
  Access it everywhere via `GameConfig.S` (loads `Resources/GameSettings`, or an
  in-memory instance from `GameSettings.PopulateDefaults()` if the asset is
  absent). Menu: **Tools > Raggler > Create Game Settings Asset**
  (`Assets/Editor/GameSettingsCreator.cs`).
- The Web3 `GameObject` **must** stay named `"Web3Bridge"` — the browser JS
  (`raggler-web3.js`) calls it by name via `SendMessage`.

## Files (Assets/Scripts)

| File | Responsibility |
|---|---|
| `Bootstrap.cs` | Runtime scene build (camera, EventSystem, canvas, Web3Bridge, GameController). |
| `GameConfig.cs` | `GameConfig.S` settings loader + engine constants (see below). |
| `GameSettings.cs` | The ScriptableObject: cards, per-screen backgrounds, levels, endless, icons, peekaboo, menu-UI toggles. |
| `GameController.cs` | The big one: menu, HUD, popups, game flow, items, scoring, endless, transitions, claim UI, persistence. |
| `Board.cs` | Layered layout generation, coverage/blocking, undo return, refresh reshuffle. Play-area bounds live here. |
| `Tray.cs` | Clearing zone: kind-grouping, triple detection + pop, overflow. |
| `SideStack.cs` | Face-down side piles beside the tray; only the front card is playable. |
| `Card.cs` | One card: frame + masked face (picture or placeholder), back, shade. |
| `StarIcon.cs` | Layered star (dark border + gold fill + sparkle); used by HUD/menu/popups. |
| `Confetti.cs` | Explosive radial confetti (velocity + gravity + spin + fade). |
| `Peekaboo.cs` | Menu-only meme images peeking from a random edge; tap to dismiss. |
| `SpinBackground.cs` | Slow constant Z-rotation for the sunburst. |
| `SpriteFactory.cs` | All runtime sprites. |
| `Ui.cs` | uGUI builder helpers (Rect/Stretch/Panel/PanelInner/BorderPanel/Label/MakeButton). |
| `Tween.cs` | Coroutine tweens (MoveTo, PopAndDestroy, ScaleIn ease-out-back, Delay, Run). |
| `Web3Bridge.cs` | MetaMask/Moralis bridge; **local PlayerPrefs simulation when not WebGL** so the flow is testable in-editor. |
| `Editor/GameSettingsCreator.cs` | One-click settings-asset creation. |

Web3 client stack (ship together when building): `Assets/Plugins/WebGL/Web3.jslib`,
`Assets/WebGLTemplates/Raggler/{index.html, web3config.js, raggler-web3.js}`,
`contracts/RagglerToken.sol`.

## Key constants (GameConfig.cs; engine-level, not designer-facing)

`CardSize 110`, `HalfStep 55`, `TraySize 7`, `HoldSize 3`,
`ItemUseCapPerStage 10`, `MatchScore 30`, `MaxCombo 5`, `ComboWindow 4s`,
`TimeBonusPerSecond 10`, `EndlessRoundBonus 100`, plus the starting item
inventory (`StartRemove/StartUndo/StartRefresh` — the user tunes these, so read
the current values from the file rather than assuming).
Board play-area bounds in `Board.cs`: `MaxCol 9`, `MaxRow 3` (half-steps;
**rows are hard-capped so cards never climb over the HUD stars**).

## Gameplay rules that are easy to get wrong

- Card types are dealt from **one shared bag** across board + both side stacks so
  every kind's total is a multiple of 3 (`GameController.DealBoard`). Board tile
  count is clamped to `Board.CapacityFor(layers)` before the bag is built, and
  `Board.SplitAcrossLayers` redistributes any per-layer overflow — otherwise big
  boards silently drop cards and become unwinnable.
- **Remove** item works whenever the hold area (`HoldSize`) has *free space*, not
  only when empty. It consumes inventory + a per-stage use.
- **Undo** returns a card to its *origin* (board / correct side stack), using the
  card's remembered `jitterX/jitterY` and `stackSide`.
- **Endless**: clearing a board **banks** the stars still lit, then the countdown
  and stars reset for the next board; a failed/timed-out board banks nothing.
  Result shows up to 3 star icons **plus "+N"** for the overflow. Endless-to-
  endless board changes are intentionally **instant** (no transition) so the
  countdown isn't eaten.
- Timer pauses while a popup is open OR a transition is running (`_transitioning`).

## Design language (keep the cartoonish theme coherent)

- **Every button** uses `Ui.MakeButton`, which auto-builds the raised "bump"
  look: drop shadow behind a bordered body, tones derived by `Darken()`. The
  menu level/endless buttons use `GameController.BumpButton` (same idea, explicit
  colors). Don't add flat buttons.
- **Concentric corners**: any fill nested inside a border must use
  `Ui.PanelInner` / `SpriteFactory.RoundedRectInner` (smaller radius), not the
  outer `RoundedRect`. Reusing the outer sprite makes corners look misaligned —
  this was a real bug we fixed.
- **Text shadows must be sharp**: one white `Outline` + one single-offset
  `Shadow`. **Never stack two `Outline` components** — it renders 8 smeared
  copies (blurry). This was also a real bug.
- Card pictures fill the face **edge-to-edge via a `Mask`** (rounded corners, no
  white letterbox bars). Placeholder circles are used when a kind has no sprite.
- Screen transition = **black swipe**: panel slides in from the right, swap at
  full cover, slides off the left (`GameController.TransitionRoutine` + `Slide`,
  easeInOutQuad). Used for level start / next / replay / quit-to-menu. NOT a fade.
- Backgrounds are **per-screen** (`ApplyBackground(bool menu)`): separate menu vs
  game sprites with a shared fallback + flat color. The spinning **sunburst shows
  on the menu only** (never during gameplay), even over a custom picture.
- Stars everywhere are `StarIcon` (bordered + shiny), never a bare star image.

## Verifying code changes WITHOUT the Unity editor

There is no Unity editor in the Claude Code environment, so C# is checked by
compiling against hand-written stubs of the UnityEngine/UnityEditor APIs with
`mcs` (mono; installed via `apt-get install -y mono-mcs` if missing).

**The stub file lives in the session scratchpad and is NOT committed**, so a new
session must recreate it. Approach: write a `UnityStubs.cs` (and `EditorStubs.cs`
for the editor script) declaring only the members the game uses, then:

```
mcs -target:library -out:check.dll UnityStubs.cs Assets/Scripts/*.cs
```

Expect zero errors. This catches ~all syntax/type mistakes; it does **not** run
the game, so still ask the user to Play-test and (for the token flow) WebGL-build.
JS files are syntax-checked with `node --check`.

Practical caution learned the hard way: when doing large `str.replace` edits via a
Python heredoc, **a replacement that finds no match fails silently**. After such
edits, `grep` for the new code to confirm it actually landed before committing.

## Git workflow

- Develop on branch **`claude/ragnarok-origin-unity-replica-qmoddl`**. Default
  branch is **`main`**. Only push to the designated branch.
- The user frequently pushes Unity-generated `.meta`, `ProjectSettings/`, and
  `Packages/manifest.json` changes from their editor between turns, so pushes
  often reject with "fetch first". Standard recovery:
  `git pull --rebase origin <branch>` then push again. This is routine, not a
  problem.
- Push with `git push -u origin <branch>`.
- Do **not** open a PR unless explicitly asked.
- End commit messages with the `Co-Authored-By:` and `Claude-Session:` trailers
  the harness provides. Do **not** put the raw model identifier (e.g.
  `claude-opus-4-8`) into commits, code, or any pushed artifact.

## GameSettings knobs (all Inspector-editable, no code)

Cards (name + sprite + placeholder color; count = varieties available) · per-screen
backgrounds (menu / game / fallback sprite + flat color) · `menuSunburst` toggle ·
`showMenuPowerUps` toggle · card-back sprite · Remove/Undo/Refresh icons · peekaboo
sprites + interval/hold/size · per-level `{tiles, cardVarieties, layers,
threeStarTime, twoStarTime, sideStackCards}` · endless `{varieties, min/maxTiles,
layers, duration, star3/star2 times, sideStackCards}`.

## Open threads / things the user may ask next

- A chunky **display font** (needs a real imported font asset wired into
  `Ui.DefaultFont` / GameSettings) — the biggest remaining step toward the
  cartoon reference look.
- A **redesigned menu power-ups widget** (I suggested vertical icon badges with
  count bubbles instead of the text chip); `showMenuPowerUps` already gates it.
- Non-square card/meme images currently **stretch** to fill (mask, no letterbox);
  a crop-instead-of-stretch option is a possible follow-up.
- For real token security, claims should be **backend-signed** (currently
  client-reported) — noted in `BLOCKCHAIN_SETUP.md`.
