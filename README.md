# Raggler's Challenge — Unity Replica (WebGL + BSC Testnet)

A Unity replica of the **"Raggler's Challenge"** mini-game from *Ragnarok
Origin* (the popular *triple-tile matching* genre, as seen in 3 Tiles /
Zen Match / Sheep a Sheep). The mechanics, item system, star rating and
scoring are all there — plus a Web3 layer: **MetaMask login on BNB Smart
Chain Testnet**, with **stars convertible to ERC-20 tokens** (via
[Moralis](https://moralis.com/) for balance reads).

## Requirements

- **Unity 6** (6000.0 or newer — including the latest 6.x releases).
- No packages beyond the built-in **uGUI** (already in the manifest).
- Everything renders with runtime-generated placeholder art until you
  assign your own pictures in the settings asset (see below).

## How to run

1. Open the project folder with Unity Hub (add it as an existing project).
2. Open `Assets/Scenes/Main.unity` (any scene works — the game bootstraps
   itself at runtime) and press **Play**.

## Customizing the game in the editor 🎨

Run **Tools > Raggler > Create Game Settings Asset** once. This creates
`Assets/Resources/GameSettings.asset`; select it and everything is editable
in the Inspector — no code required:

- **Cards**: add/remove entries in *Card Kinds* to change how many
  varieties exist, and assign a **Sprite** to each to use your own card
  pictures (drag any imported image; set its Texture Type to *Sprite (2D
  and UI)*). Entries without a sprite render as colored placeholder circles.
- **Card Back & Power-up Icons**: give face-down side-pile cards a custom
  picture, and put icons on the Remove / Undo / Refresh buttons.
- **Menu Peekaboo**: drop any images (memes encouraged) into *Peekaboo
  Sprites* and they'll randomly peek in from a screen edge on the main
  menu, hold a moment, and duck away — interval, hold time and size are
  all tunable. Leave the list empty to disable.
- **Background**: separate images for the **main menu** and **gameplay**
  (with a shared fallback sprite and flat color), plus toggles for the
  slowly spinning **menu sunburst** and the menu **power-ups chip**.
- **Levels**: one list entry per level (add more entries = more levels).
  Each level sets its own **card varieties** (e.g. 3 kinds in level 1,
  5 in level 2, ...), tile count, layer depth, the two star-time
  thresholds, and **Side Stack Cards** — the number of face-down cards in
  each pile beside the clearing zone (0 = no piles).
- **Endless Mode**: a **fixed** variety count and a tile-count range
  (default 30–51 cards per board), the side-pile size (default 10 per
  side), plus the run timer and the times at which stars burn out.

If no asset exists, the game silently uses built-in defaults identical to
the shipped values.

## How to play

- **Tap a card** to move it into the **clearing zone** at the bottom.
  Cards covered by a higher layer are darkened and can't be picked.
- **Three identical cards** in the clearing zone clear automatically.
- The clearing zone holds **7 cards max** — go over and you **lose**.
- Some boards also have **face-down side piles** flanking the clearing
  zone — only the front (face-up) card of each pile can be played, and
  taking it flips the next one up.
- Clear every card on the board (and the side piles) to win the level.
- Quitting mid-run asks for confirmation, and clearing a level celebrates
  with animated stars and confetti. Remaining power-ups are shown on the
  main menu.

### Items (right panel)

| Item | Effect |
|---|---|
| **Remove** | Moves the first 3 cards of the clearing zone to a holding area (bottom-left). Tap a held card to put it back. |
| **Undo** | Returns the last card you played from the zone back to its board spot. |
| **Refresh** | Reshuffles the kinds of all cards still on the board. |

Each item is limited to **10 uses per level** and consumes inventory
(you start with 37 / 5 / 23 and earn +1 of each per level cleared).

### Rating & scoring

- **Levels:** stars come from completion time (3★ under the fast
  threshold, 2★ under the slow one, otherwise 1★). The HUD stars dim in
  real time. Score = 30 pts per triple × combo (up to ×5 within 4s) +
  a time bonus on clear.
- **Endless:** boards of a fixed variety pool keep coming while a
  **countdown timer** runs. Your three stars burn out at configurable
  times; **clearing a board banks the stars still lit**, then the timer
  and stars reset fresh for the next board. A failed or timed-out board
  banks nothing. The run's result shows up to 3 star icons plus a "+N"
  for anything banked beyond that (e.g. 2★ + 3★ = ★★★ +2), and the whole
  banked total is claimable as tokens.

## Stars → tokens (BSC Testnet) 🪙

- Click **Connect Wallet** on the menu: MetaMask pops up, switches to BSC
  Testnet and signs a login message.
- Clear a level → the victory screen offers **Claim N RST** (1 star =
  1 token, ERC-20). **Each level pays out exactly once per wallet** —
  enforced by the smart contract; replaying a finished level never mints
  again.
- End an Endless run → claim every star you banked across its boards
  (repeatable, with an on-chain cooldown).
- Balances are read through the **Moralis Web3 Data API** when a key is
  configured, with a direct-chain fallback.
- In the editor (non-WebGL) the whole flow runs as a **local simulation**
  so you can test without a wallet.

Full instructions — deploying `contracts/RagglerToken.sol`, faucet links,
Moralis keys, WebGL build settings — are in
**[BLOCKCHAIN_SETUP.md](BLOCKCHAIN_SETUP.md)**.

## Building for WebGL

Switch the platform to **Web/WebGL**, select the **Raggler** WebGL template
in Player Settings (it ships the MetaMask/Moralis JavaScript), set
compression to *Disabled* for simple hosting, and build. Details in
[BLOCKCHAIN_SETUP.md](BLOCKCHAIN_SETUP.md).

## Project layout

```
Assets/
  Scenes/Main.unity            Empty scene — everything is built at runtime
  Editor/GameSettingsCreator.cs  One-click settings asset creation
  Plugins/WebGL/Web3.jslib     Unity -> browser JS bridge
  WebGLTemplates/Raggler/      index.html + web3config.js + raggler-web3.js
  Scripts/
    Bootstrap.cs               Creates camera, EventSystem, canvas, bridge, controller
    GameSettings.cs            ScriptableObject: cards, background, levels, endless
    GameConfig.cs              Settings loader + engine constants
    GameController.cs          Menu, HUD, popups, game flow, items, claims
    Board.cs                   Layered layout generation + coverage (blocking)
    Tray.cs                    Clearing zone: grouping, triple detection, overflow
    Card.cs                    Card visuals (sprite or placeholder) + clicks
    Web3Bridge.cs              MetaMask/Moralis bridge + editor simulation
    SpriteFactory.cs           Runtime-generated sprites (rounded rect, circle, star)
    Ui.cs                      Small uGUI builder helpers
    Tween.cs                   Minimal coroutine tween helper
contracts/RagglerToken.sol     ERC-20 + one-shot level claims + endless claims
BLOCKCHAIN_SETUP.md            Deploy & configure guide (BSC Testnet, Moralis)
```
