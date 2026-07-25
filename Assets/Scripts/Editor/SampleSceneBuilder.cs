using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SuikodenLike.Battle;
using SuikodenLike.Core;
using SuikodenLike.Dialogue;
using SuikodenLike.Field;
using SuikodenLike.Inventory;

namespace SuikodenLike.EditorTools
{
    /// <summary>
    /// Builds the whole playable sample scene from code — map, colliders,
    /// player, NPCs, chest, encounter zone, battle arena and every UI panel,
    /// with all the serialized references wired up.
    ///
    /// Doing this in code rather than shipping a .unity file avoids Unity's
    /// auto-generated asset GUIDs, which don't survive being hand-authored.
    /// </summary>
    public static class SampleSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/SampleField.unity";
        public const string PrefabFolder = "Assets/Content/Prefabs";

        const int PPU = PlaceholderArt.PPU;
        const int Cols = PlaceholderArt.MapCols;
        const int Rows = PlaceholderArt.MapRows;

        // ---------- serialized-field helpers ----------
        // The framework's fields are private [SerializeField], so the editor
        // has to go through SerializedObject to set them.
        static SerializedObject SO(Object target) => new SerializedObject(target);

        static void Set(Object target, string field, System.Action<SerializedProperty> apply)
        {
            var so = SO(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[SampleSceneBuilder] {target.GetType().Name} has no field '{field}'.");
                return;
            }
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRef(Object t, string f, Object v) => Set(t, f, p => p.objectReferenceValue = v);
        static void SetFloat(Object t, string f, float v) => Set(t, f, p => p.floatValue = v);
        static void SetInt(Object t, string f, int v) => Set(t, f, p => p.intValue = v);
        static void SetString(Object t, string f, string v) => Set(t, f, p => p.stringValue = v);
        static void SetVec2(Object t, string f, Vector2 v) => Set(t, f, p => p.vector2Value = v);

        static void SetRefList(Object t, string f, IList<Object> values)
        {
            Set(t, f, p =>
            {
                p.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });
        }

        // ---------- geometry ----------
        static Vector3 TileToWorld(int row, int col)
        {
            return new Vector3(-Cols * 0.5f + col + 0.5f, Rows * 0.5f - row - 0.5f, 0f);
        }

        // ---------- entry point ----------
        public static void Build(PlaceholderArt.Baked art, SampleContentBuilder.Content content)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grid = PlaceholderArt.BuildMapGrid();

            // ---- persistent manager ----
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();
            SetRefList(gm, "startingParty", new Object[] { content.nadia, content.corin, content.yves, content.sable });
            SetInt(gm, "startingGold", 250);
            SetRefList(gm, "startingItems", new Object[] { content.medicine, content.medicine, content.ether });

            // ---- world ----
            var world = new GameObject("World");

            var mapGO = new GameObject("Map");
            mapGO.transform.SetParent(world.transform);
            var mapSR = mapGO.AddComponent<SpriteRenderer>();
            mapSR.sprite = art.map;
            mapSR.sortingOrder = -100;

            BuildCollision(grid, world.transform);
            BuildEncounterZone(grid, world.transform, content);

            // ---- player ----
            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(world.transform);
            spawn.transform.position = TileToWorld(10, 18);

            var player = new GameObject("Player");
            player.transform.SetParent(world.transform);
            player.transform.position = spawn.transform.position;

            var playerSR = player.AddComponent<SpriteRenderer>();
            playerSR.sprite = art.heroBlue;
            playerSR.sortingOrder = 10;

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var playerCol = player.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.7f, 0.5f);
            playerCol.offset = new Vector2(0f, -0.25f);

            var pc = player.AddComponent<PlayerController>();
            SetFloat(pc, "moveSpeed", 4.5f);
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<RandomEncounterController>();

            // ---- camera ----
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();

            var follow = camGO.AddComponent<CameraFollow>();
            SetRef(follow, "target", player.transform);
            SetVec2(follow, "mapMin", new Vector2(-Cols * 0.5f, -Rows * 0.5f));
            SetVec2(follow, "mapMax", new Vector2(Cols * 0.5f, Rows * 0.5f));

            // ---- NPCs and objects ----
            MakeNPC("NPC_ElderMarn", art.elder, TileToWorld(8, 12), world.transform,
                content.elderTalk, null, null);
            MakeNPC("NPC_Villager", art.villager, TileToWorld(11, 10), world.transform,
                content.villagerTalk, "quest_started", content.villagerAfterQuest);
            MakeChest("Chest_Roadside", art.chestClosed, art.chestOpen, TileToWorld(20, 6),
                world.transform, content.medicine, 2, 120, "roadside_01");
            MakeChest("Chest_Field", art.chestClosed, art.chestOpen, TileToWorld(6, 30),
                world.transform, content.kiteShield, 1, 0, "field_01");

            // ---- battle arena, parked far away in world space ----
            var arena = new GameObject("BattleArena");
            arena.transform.position = new Vector3(1000f, 0f, 0f);
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(arena.transform);
            backdrop.transform.localPosition = Vector3.zero;
            var bdSR = backdrop.AddComponent<SpriteRenderer>();
            bdSR.sprite = art.backdrop;
            bdSR.sortingOrder = -50;

            // ---- battle logic ----
            var battleGO = new GameObject("BattleManager");
            var battle = battleGO.AddComponent<BattleManager>();
            SetFloat(battle, "actionDelay", 0.55f);

            var view = battleGO.AddComponent<BattleSceneView>();
            SetRef(view, "battle", battle);
            SetRef(view, "arenaRoot", arena.transform);
            SetRef(view, "cam", cam);
            SetRef(view, "cameraFollow", follow);
            SetRef(view, "player", pc);
            SetRef(view, "respawnPoint", spawn.transform);

            // ---- UI ----
            BuildUI(battle);

            // ---- save ----
            PlaceholderArt.EnsureFolder("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        // ---------- world pieces ----------
        static void BuildCollision(char[,] grid, Transform parent)
        {
            var go = new GameObject("Collision");
            go.transform.SetParent(parent);

            // Merge each row's runs of solid tiles into one box, so the scene
            // ends up with a few dozen colliders instead of several hundred.
            for (int r = 0; r < Rows; r++)
            {
                int c = 0;
                while (c < Cols)
                {
                    if (grid[r, c] != '#') { c++; continue; }
                    int start = c;
                    while (c < Cols && grid[r, c] == '#') c++;
                    int width = c - start;

                    var box = go.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(width, 1f);
                    box.offset = new Vector2(
                        -Cols * 0.5f + start + width * 0.5f,
                        Rows * 0.5f - r - 0.5f);
                }
            }
        }

        static EncounterZone BuildEncounterZone(char[,] grid, Transform parent,
            SampleContentBuilder.Content content)
        {
            int minR = int.MaxValue, maxR = int.MinValue, minC = int.MaxValue, maxC = int.MinValue;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (grid[r, c] == 'T')
                    {
                        if (r < minR) minR = r;
                        if (r > maxR) maxR = r;
                        if (c < minC) minC = c;
                        if (c > maxC) maxC = c;
                    }

            var go = new GameObject("EncounterZone_Grassland");
            go.transform.SetParent(parent);
            if (minR > maxR) return null; // no tall grass in the map

            float w = maxC - minC + 1;
            float h = maxR - minR + 1;
            go.transform.position = new Vector3(
                -Cols * 0.5f + minC + w * 0.5f,
                Rows * 0.5f - minR - h * 0.5f, 0f);

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(w, h);

            var zone = go.AddComponent<EncounterZone>();
            SetRef(zone, "table", content.grassland);
            return zone;
        }

        static void MakeNPC(string name, Sprite sprite, Vector3 pos, Transform parent,
            Object dialogue, string flag, Object flagDialogue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);

            var npc = go.AddComponent<NPCInteractable>();
            SetRef(npc, "dialogue", dialogue);
            SetRef(npc, "spriteRenderer", sr);
            SetString(npc, "prompt", "Talk");
            if (!string.IsNullOrEmpty(flag))
            {
                SetString(npc, "requiresFlag", flag);
                SetRef(npc, "dialogueWhenFlagSet", flagDialogue);
            }
        }

        static void MakeChest(string name, Sprite closed, Sprite open, Vector3 pos, Transform parent,
            Object item, int qty, int gold, string chestId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingOrder = 6;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.7f);

            var chest = go.AddComponent<ChestInteractable>();
            SetRef(chest, "item", item);
            SetInt(chest, "quantity", qty);
            SetInt(chest, "gold", gold);
            SetRef(chest, "spriteRenderer", sr);
            SetRef(chest, "openedSprite", open);
            SetString(chest, "chestId", chestId);
            SetString(chest, "prompt", "Open");
        }

        // ---------- UI ----------
        static GameObject UIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static RectTransform Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        static Image Panel(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TMP_Text Label(string name, Transform parent, string text, int size,
            TextAlignmentOptions align, Color color)
        {
            var go = UIObject(name, parent);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        static readonly Color WindowNavy = new Color(0.06f, 0.10f, 0.22f, 0.94f);
        static readonly Color Parchment = new Color(0.91f, 0.89f, 0.82f, 1f);
        static readonly Color GoldHi = new Color(0.94f, 0.80f, 0.42f, 1f);

        static void BuildUI(BattleManager battle)
        {
            var canvasGO = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 480f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var choicePrefab = BuildChoiceButtonPrefab();
            var rowPrefab = BuildItemRowPrefab();

            BuildDialoguePanel(canvasGO, choicePrefab);
            BuildBattlePanel(canvasGO, battle, choicePrefab);
            BuildInventoryPanel(canvasGO, rowPrefab);

            // A small controls hint so the demo explains itself.
            var hint = Label("Hint", canvasGO.transform,
                "WASD / arrows  move      E  talk & open      I  bag",
                14, TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.5f));
            Stretch(hint.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -32f), new Vector2(400f, -10f));
        }

        static Button BuildChoiceButtonPrefab()
        {
            PlaceholderArt.EnsureFolder(PrefabFolder);
            var go = new GameObject("ChoiceButton", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.24f, 0.44f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 30f);

            var label = Label("Label", go.transform, "Choice", 16,
                TextAlignmentOptions.Left, Parchment);
            Stretch(label.gameObject, Vector2.zero, Vector2.one,
                new Vector2(10f, 0f), new Vector2(-6f, 0f));

            string path = PrefabFolder + "/ChoiceButton.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset.GetComponent<Button>();
        }

        static GameObject BuildItemRowPrefab()
        {
            PlaceholderArt.EnsureFolder(PrefabFolder);
            // No Image on the root: InventoryUI grabs the first Image in the
            // row and uses it as the item icon.
            var go = new GameObject("ItemRow", typeof(RectTransform));
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 28f);

            var icon = UIObject("Icon", go.transform);
            icon.AddComponent<Image>();
            var iconLE = icon.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 24f;
            iconLE.preferredHeight = 24f;

            var label = Label("Label", go.transform, "Item", 16,
                TextAlignmentOptions.Left, Parchment);
            var labelLE = label.gameObject.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 260f;
            labelLE.preferredHeight = 24f;

            string path = PrefabFolder + "/ItemRow.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        static void BuildDialoguePanel(GameObject canvasGO, Button choicePrefab)
        {
            var panel = UIObject("DialoguePanel", canvasGO.transform);
            Panel(panel, WindowNavy);
            Stretch(panel, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(20f, 20f), new Vector2(-20f, 170f));

            var portrait = UIObject("Portrait", panel.transform);
            var portraitImg = portrait.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            Stretch(portrait, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(12f, -108f), new Vector2(108f, -12f));

            var speaker = Label("Speaker", panel.transform, "", 18,
                TextAlignmentOptions.Left, GoldHi);
            Stretch(speaker.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(120f, -34f), new Vector2(-12f, -8f));

            var body = Label("Body", panel.transform, "", 17,
                TextAlignmentOptions.TopLeft, Parchment);
            Stretch(body.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(120f, 10f), new Vector2(-12f, -38f));

            var indicator = Label("ContinueIndicator", panel.transform, "▼", 16,
                TextAlignmentOptions.BottomRight, GoldHi);
            Stretch(indicator.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 6f), new Vector2(-10f, 30f));

            var choices = UIObject("ChoicesContainer", panel.transform);
            var vlg = choices.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.LowerRight;
            Stretch(choices, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-270f, 8f), new Vector2(-12f, 78f));

            // DialogueUI + DialogueManager live on the Canvas so that hiding
            // the panel never disables the component driving it.
            var ui = canvasGO.AddComponent<DialogueUI>();
            SetRef(ui, "root", panel);
            SetRef(ui, "speakerText", speaker);
            SetRef(ui, "bodyText", body);
            SetRef(ui, "portraitImage", portraitImg);
            SetRef(ui, "continueIndicator", indicator.gameObject);
            SetRef(ui, "choicesContainer", choices.transform);
            SetRef(ui, "choiceButtonPrefab", choicePrefab);

            var mgr = canvasGO.AddComponent<DialogueManager>();
            SetRef(mgr, "ui", ui);
            SetFloat(mgr, "charsPerSecond", 45f);
        }

        static void BuildBattlePanel(GameObject canvasGO, BattleManager battle, Button choicePrefab)
        {
            var panel = UIObject("BattlePanel", canvasGO.transform);
            Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var log = Label("Log", panel.transform, "", 18,
                TextAlignmentOptions.Top, Parchment);
            Stretch(log.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -46f), new Vector2(-20f, -14f));

            // Command window
            var menu = UIObject("CommandMenu", panel.transform);
            Panel(menu, WindowNavy);
            Stretch(menu, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 20f), new Vector2(180f, 190f));

            var menuList = UIObject("List", menu.transform);
            var mvlg = menuList.AddComponent<VerticalLayoutGroup>();
            mvlg.spacing = 4f;
            mvlg.padding = new RectOffset(10, 10, 10, 10);
            mvlg.childForceExpandWidth = true;
            mvlg.childForceExpandHeight = false;
            Stretch(menuList, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var attack = CommandButton("Attack", menuList.transform);
            var skill = CommandButton("Skill", menuList.transform);
            var item = CommandButton("Item", menuList.transform);
            var defend = CommandButton("Defend", menuList.transform);
            var run = CommandButton("Run", menuList.transform);

            // Party status window
            var status = UIObject("PartyStatus", panel.transform);
            Panel(status, WindowNavy);
            Stretch(status, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(190f, 20f), new Vector2(-20f, 190f));

            var statusText = Label("StatusText", status.transform, "", 16,
                TextAlignmentOptions.TopLeft, Parchment);
            Stretch(statusText.gameObject, Vector2.zero, Vector2.one,
                new Vector2(12f, 10f), new Vector2(-12f, -10f));

            // Target picker
            var targets = UIObject("TargetContainer", panel.transform);
            var tvlg = targets.AddComponent<VerticalLayoutGroup>();
            tvlg.spacing = 4f;
            tvlg.childForceExpandWidth = true;
            tvlg.childForceExpandHeight = false;
            tvlg.childAlignment = TextAnchor.MiddleCenter;
            Stretch(targets, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-120f, -70f), new Vector2(120f, 70f));

            var ui = canvasGO.AddComponent<BattleUI>();
            SetRef(ui, "battle", battle);
            SetRef(ui, "root", panel);
            SetRef(ui, "logText", log);
            SetRef(ui, "partyStatusText", statusText);
            SetRef(ui, "commandMenu", menu);
            SetRef(ui, "attackButton", attack);
            SetRef(ui, "skillButton", skill);
            SetRef(ui, "itemButton", item);
            SetRef(ui, "defendButton", defend);
            SetRef(ui, "runButton", run);
            SetRef(ui, "targetContainer", targets.transform);
            SetRef(ui, "targetButtonPrefab", choicePrefab);
        }

        static Button CommandButton(string text, Transform parent)
        {
            var go = UIObject(text + "Button", parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.24f, 0.44f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 28f;

            var label = Label("Label", go.transform, text, 16,
                TextAlignmentOptions.Center, Parchment);
            Stretch(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        static void BuildInventoryPanel(GameObject canvasGO, GameObject rowPrefab)
        {
            var panel = UIObject("InventoryPanel", canvasGO.transform);
            Panel(panel, WindowNavy);
            Stretch(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200f, -160f), new Vector2(200f, 160f));

            var title = Label("Title", panel.transform, "ITEMS", 20,
                TextAlignmentOptions.Left, GoldHi);
            Stretch(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, -40f), new Vector2(-16f, -10f));

            var list = UIObject("ListContainer", panel.transform);
            var vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            Stretch(list, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(16f, 46f), new Vector2(-16f, -46f));

            var gold = Label("Gold", panel.transform, "Gold 0", 17,
                TextAlignmentOptions.Left, GoldHi);
            Stretch(gold.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(16f, 12f), new Vector2(-16f, 40f));

            var ui = canvasGO.AddComponent<InventoryUI>();
            SetRef(ui, "root", panel);
            SetRef(ui, "listContainer", list.transform);
            SetRef(ui, "rowPrefab", rowPrefab);
            SetRef(ui, "goldText", gold);
            SetInt(ui, "toggleKey", (int)KeyCode.I);
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes) if (s.path == path) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
