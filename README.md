# Suikoden-like RPG Framework (Unity 2D)

A **data-driven, customizable** starter framework for a classic JRPG in the vein of
*Suikoden II*, built for the Unity engine (2D / pixel-art). You bring the story,
sprites, NPCs and items — the framework handles the systems.

> **Status:** Core code foundation. All gameplay *systems* are implemented as C#
> scripts. The last-mile "wire it up in the Unity Editor" steps (scenes, prefabs,
> Canvas layout) are done in Unity — see [`docs/SETUP.md`](docs/SETUP.md). Unity's
> scene/prefab files use fragile auto-generated GUIDs, so they're intentionally
> *not* hand-authored here.

## What's implemented

| System | Scripts | Notes |
|---|---|---|
| **Overworld movement** | `Field/PlayerController.cs` | Free 8-direction 2D walking, animator-ready. |
| **Random field encounters** | `Field/EncounterZone.cs`, `RandomEncounterController.cs` | Step-based, Suikoden-style "walk and get ambushed", per-zone rates. |
| **NPC interaction** | `Field/Interactable.cs`, `NPCInteractable.cs`, `PlayerInteractor.cs` | Face + press to talk. |
| **Interactive objects** | `Field/ChestInteractable.cs` | Chests/signs; give items, persist via flags. |
| **Branching dialogue** | `Dialogue/DialogueManager.cs`, `DialogueUI.cs` | Typewriter text, portraits, choices, give-item/recruit/set-flag effects. |
| **Turn-based battle** | `Battle/BattleManager.cs`, `BattleUnit.cs`, `DamageCalculator.cs`, `BattleUI.cs` | Attack / Skill / Item / Defend / Run, speed-based turns, enemy AI, EXP/gold/loot. |
| **Inventory** | `Inventory/Inventory.cs`, `InventoryUI.cs` | Stacking, gold, equipment bonuses. |
| **Party & progression** | `Party/PartyMember.cs` | Levels, EXP curve, derived stats, equipment. |
| **Game hub / save-state** | `Core/GameManager.cs` | Persistent party, inventory, story flags; brokers battle transitions. |

## Everything you customize is a ScriptableObject

No coding needed to build your game's content. In Unity, right-click in the
Project window → **Create → SuikodenLike →** …

- **Character** — a playable hero: name, pixel sprite, portrait, stats, skills.
- **Enemy** — sprite, stats, skills, EXP/gold/loot.
- **Item** — consumables, weapons, armor (with stat bonuses).
- **Skill** — physical/magic damage, heals, buffs; single/all targets.
- **Dialogue** — full conversations with branching + side effects.
- **Encounter Table** — which enemy groups appear in a zone and how often.

See [`docs/CUSTOMIZING.md`](docs/CUSTOMIZING.md) for a step-by-step "make your
own content" guide, and [`docs/SETUP.md`](docs/SETUP.md) to get a playable scene
running.

## Requirements

- **Unity 2022.3 LTS** or newer (project targets 2022.3.40f1; Unity 6 works too).
- TextMeshPro (auto-installed via `Packages/manifest.json`).

## Quick start

1. Open the folder in Unity Hub (it will import and generate the `Library/`).
2. When prompted, **Import TMP Essentials**.
3. Follow [`docs/SETUP.md`](docs/SETUP.md) to build the boot scene and hook up
   the managers.

## Roadmap (natural next steps)

- Save/load to disk, shops & inns, equipment menu UI, status effects,
  Suikoden's signature **6-character parties**, unite attacks, army/duel battles,
  and a tilemap world with map transitions. Ask and I'll build these next.
