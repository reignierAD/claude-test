# Customizing your game (no code required)

Everything that makes *your* game — the story, characters, sprites, enemies,
items — is authored as **ScriptableObject assets** in the Unity Project window.
Right-click → **Create → SuikodenLike → …**

## Your pixel-art characters
1. Import your sprite sheets (PNG). Set **Texture Type: Sprite (2D and UI)**,
   **Filter Mode: Point (no filter)** and **Compression: None** for crisp pixels.
2. Create → SuikodenLike → **Character**. Fill in name, drag your `sprite` and a
   dialogue `portrait`, set base stats and per-level growth, and add **Skill** assets.

## Your enemies
- Create → SuikodenLike → **Enemy**. Sprite, stats, optional skills, and the
  rewards (EXP, gold, and a loot list with drop chances).

## Your skills / spells
- Create → SuikodenLike → **Skill**. Choose a `kind` (physical/magic damage,
  heal, buff), a `target` (single/all, enemy/ally/self), MP cost and power.
- Assign skills to characters and enemies. The battle math lives in
  `DamageCalculator.cs` if you want to retune feel.

## Your items
- Create → SuikodenLike → **Item**. Consumables carry an effect (Heal HP/MP,
  Revive); weapons/armor carry `attackBonus`/`defenseBonus`.

## Your story & dialogue
- Create → SuikodenLike → **Dialogue**. Each *line* has a speaker, text, and an
  optional portrait. Add **choices** to branch (each choice jumps to a line index;
  `-1` ends the conversation).
- Per-line **side effects**: `giveItem`, `recruitCharacter` (adds them to your
  party!), and `setFlag` (mark story progress).
- Gate alternate dialogue on an NPC with `requiresFlag` +
  `dialogueWhenFlagSet` — e.g. a villager who says something new after you finish
  a quest.

## Your world & encounters
- Create → SuikodenLike → **Encounter Table**. Add **groups** (each a set of
  enemies that appear together) with weights, and tune
  `averageStepsBetweenEncounters` + `variance`.
- Assign the table to an **EncounterZone** trigger in your map. Different zones =
  different tables = different monster regions.

## Story flags (the glue for quests)
- `GameManager.SetFlag("met_the_king")`, `HasFlag(...)`, `ClearFlag(...)`.
- Dialogue lines can set flags; NPCs can react to them. This is how you build
  quest progression, one-time events, and branching outcomes without code.

## Placing NPCs & objects
- Drag an NPC or chest prefab into the scene, position it, assign its Dialogue/
  Item. That's it — they're fully data-driven and reusable.

## Tuning without touching systems
| Want to change… | Edit… |
|---|---|
| Damage/hit/crit feel | `Battle/DamageCalculator.cs` |
| EXP curve / level growth | `Party/PartyMember.cs` |
| Encounter frequency | the **Encounter Table** asset |
| Walk speed / interact key | `PlayerController` / `PlayerInteractor` inspector |
| Text speed | `DialogueManager.charsPerSecond` |
