# Setup: getting a playable scene running

This framework ships the *code*. Here's how to assemble a minimal playable scene
in the Unity Editor. Takes ~15 minutes the first time.

## 1. Open the project
- Add this folder in **Unity Hub** and open it with Unity 2022.3 LTS (or newer).
- Let it import, then **Window → TextMeshPro → Import TMP Essential Resources**.

## 2. Create the persistent GameManager
1. New scene `Boot` (or reuse your main scene).
2. Create an empty GameObject `GameManager`, add the **GameManager** component.
3. Create a couple of **Character** assets (Create → SuikodenLike → Character),
   give them sprites/stats, and drag them into GameManager's *Starting Party*.
4. Optionally add starting **Item** assets and gold.

## 3. The player
1. Create a GameObject `Player` with a **SpriteRenderer** (your pixel hero).
2. Add **Rigidbody2D** (Gravity Scale = 0, Body Type = Dynamic or Kinematic).
3. Add a **Collider2D** (e.g. CapsuleCollider2D).
4. Add these components:
   - **PlayerController**
   - **PlayerInteractor**
   - **RandomEncounterController**
5. (Optional) Add an Animator with `MoveX`, `MoveY`, `Speed` params for a walk cycle.

## 4. NPCs & objects
- **NPC:** GameObject + SpriteRenderer + Collider2D + **NPCInteractable**; assign
  a **Dialogue** asset. Place as many as you like — they're just prefabs.
- **Chest:** GameObject + SpriteRenderer + Collider2D + **ChestInteractable**;
  assign an Item and/or gold.

## 5. Random encounter zones
1. Create an empty GameObject `Grassland`.
2. Add a **BoxCollider2D** with *Is Trigger* checked, sized over the area.
3. Add **EncounterZone**, assign an **Encounter Table** asset (with enemy groups).
- Walk the player into it and keep moving — a battle triggers after a randomized
  distance.

## 6. Dialogue UI (Canvas)
1. Create a **Canvas** (Screen Space – Overlay).
2. Build a dialogue panel: a background Image, a `Speaker` TMP text, a `Body` TMP
   text, an optional `Portrait` Image, and a `ContinueIndicator` object.
3. For choices: an empty `ChoicesContainer` (with a Vertical Layout Group) and a
   `ChoiceButton` prefab (a Button with a child TMP text).
4. Add a **DialogueManager** component (on the Canvas or a manager object) and a
   **DialogueUI** component; drag all the fields into DialogueUI, then drag the
   DialogueUI into DialogueManager's `ui` field.

## 7. Battle UI (Canvas)
1. Build a battle panel: `Log` TMP text, `PartyStatus` TMP text, a `CommandMenu`
   object with five Buttons (Attack/Skill/Item/Defend/Run), a `TargetContainer`,
   and a `TargetButton` prefab.
2. Add a **BattleManager** component to a manager object.
3. Add a **BattleUI** component; assign the BattleManager and all the panel fields.
- The BattleManager listens for encounters automatically via GameManager.

## 8. Press Play
- Walk around, talk to NPCs (press **E**), open chests, and get into random
  battles in your encounter zone.

### Notes
- Default keys: **WASD/Arrows** to move, **E** (or Submit) to interact/advance text.
- Everything communicates through events, so you can restyle any UI without
  touching the game logic.
