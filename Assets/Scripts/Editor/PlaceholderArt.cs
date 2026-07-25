using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SuikodenLike.EditorTools
{
    /// <summary>
    /// Bakes the placeholder pixel art into real PNG assets with correct
    /// pixel-art import settings. Everything here is a stand-in: replace the
    /// PNGs (or point the content assets at your own art) and nothing breaks.
    /// </summary>
    public static class PlaceholderArt
    {
        public const int PPU = 16;
        public const string ArtFolder = "Assets/Content/Art";

        // ---------------------------------------------------------------
        // A tiny top-down image buffer. Row 0 is the TOP row, which matches
        // how the art below reads; ToTexture() flips it for Unity.
        // ---------------------------------------------------------------
        public class Bitmap
        {
            public readonly int width, height;
            public readonly Color32[] pixels;

            public Bitmap(int w, int h)
            {
                width = w; height = h;
                pixels = new Color32[w * h];
            }

            public void Set(int x, int y, Color32 c)
            {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                pixels[y * width + x] = c;
            }

            public void FillRect(int x, int y, int w, int h, Color32 c)
            {
                for (int yy = y; yy < y + h; yy++)
                    for (int xx = x; xx < x + w; xx++)
                        Set(xx, yy, c);
            }

            public void Blit(string[] art, Dictionary<char, Color32> pal, int x, int y)
            {
                for (int r = 0; r < art.Length; r++)
                {
                    string row = art[r];
                    for (int c = 0; c < row.Length; c++)
                    {
                        Color32 col;
                        if (!pal.TryGetValue(row[c], out col)) continue;
                        if (col.a == 0) continue;
                        Set(x + c, y + r, col);
                    }
                }
            }

            public Texture2D ToTexture()
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var flipped = new Color32[pixels.Length];
                for (int y = 0; y < height; y++)
                {
                    int src = y * width;
                    int dst = (height - 1 - y) * width;
                    for (int x = 0; x < width; x++) flipped[dst + x] = pixels[src + x];
                }
                tex.SetPixels32(flipped);
                tex.Apply();
                return tex;
            }
        }

        static Color32 C(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>Base palette. Individual sprites override entries to recolour.</summary>
        public static Dictionary<char, Color32> Palette()
        {
            return new Dictionary<char, Color32>
            {
                { '.', Clear },
                { 'k', C("#17121f") }, { 's', C("#f2c79a") }, { 'S', C("#c99366") },
                { 'e', C("#1b2340") }, { 'h', C("#7b4a24") }, { 'H', C("#a8702f") },
                { 'w', C("#f0ead8") }, { 't', C("#d4a83c") }, { 'n', C("#4a3524") },
                { 'b', C("#3f74b8") }, { 'B', C("#2a4f85") }, { 'G', C("#3f7a34") },
                { 'g', C("#4a7c3f") }, { 'M', C("#a4703c") }, { 'm', C("#7c4f26") },
                { 'r', C("#c8443a") }, { 'd', C("#241a12") }
            };
        }

        static Dictionary<char, Color32> With(params object[] overrides)
        {
            var p = Palette();
            for (int i = 0; i + 1 < overrides.Length; i += 2)
                p[(char)overrides[i]] = C((string)overrides[i + 1]);
            return p;
        }

        // ---------------------------------------------------------------
        // Sprite art (16x16)
        // ---------------------------------------------------------------
        public static readonly string[] Hero = {
            "................",
            ".....kkkkkk.....",
            "....khhhhhhk....",
            "...khHHHHHHhk...",
            "...khssssssHk...",
            "...khsesesshk...",
            "...khssssssHk...",
            "....kSssssSk....",
            "....kkwwwwkk....",
            "...kbbtbbtbbk...",
            "..kbBbbbbbbBbk..",
            "..kbBbbbbbbBbk..",
            "..kk.bbbbbb.kk..",
            ".....kbbbbk.....",
            "....knn..nnk....",
            "....kkk..kkk...."
        };

        public static readonly string[] Tree = {
            "................",
            "......kkkk......",
            "....kkGGGGkk....",
            "...kGGGGGGGGk...",
            "..kGGgGGGGgGGk..",
            "..kGGGGGGGGGGk..",
            ".kGGgGGGGGGgGGk.",
            ".kGGGGGGGGGGGGk.",
            "..kGGGGGGGGGGk..",
            "..kkGGGGGGGGkk..",
            "....kkGGGGkk....",
            "......ktbk......",
            "......ktbk......",
            "......ktbk......",
            ".....kttbbk.....",
            "....kkttbbkk...."
        };

        public static readonly string[] ChestClosed = {
            "................",
            "................",
            "..kkkkkkkkkkkk..",
            "..kMMMMMMMMMMk..",
            "..kMwwMMMMwwMk..",
            "..kMMMMMMMMMMk..",
            "..kkkkkkkkkkkk..",
            "..kmmmmmmmmmmk..",
            "..kmmmmttmmmmk..",
            "..kmmmmttmmmmk..",
            "..kmmmmmmmmmmk..",
            "..kmmmmmmmmmmk..",
            "..kkkkkkkkkkkk..",
            "................",
            "................",
            "................"
        };

        public static readonly string[] ChestOpen = {
            "................",
            "..kkkkkkkkkkkk..",
            "..kMMMMMMMMMMk..",
            "..kMwwMMMMwwMk..",
            "..kkkkkkkkkkkk..",
            "................",
            "..kkkkkkkkkkkk..",
            "..kddddddddddk..",
            "..kdttttttttdk..",
            "..kddddddddddk..",
            "..kmmmmmmmmmmk..",
            "..kmmmmmmmmmmk..",
            "..kkkkkkkkkkkk..",
            "................",
            "................",
            "................"
        };

        public static readonly string[] Slime = {
            "................",
            "................",
            ".....kkkkkk.....",
            "...kkggggggkk...",
            "..kggggggggggk..",
            ".kgggwwggwwgggk.",
            ".kgggwewwewgggk.",
            ".kggggggggggggk.",
            ".kggggggggggggk.",
            ".kggggggggggggk.",
            ".kggggggggggggk.",
            "..kggggggggggk..",
            "..kkkkkkkkkkkk..",
            "................",
            "................",
            "................"
        };

        public static readonly string[] Potion = {
            "................",
            "......kkkk......",
            "......kwwk......",
            ".....kkwwkk.....",
            ".....kwwwwk.....",
            "....kwrrrrwk....",
            "...kwrrrrrrwk...",
            "...kwrrrrrrwk...",
            "...kwrrrrrrwk...",
            "...kwrrrrrrwk...",
            "...kwrrrrrrwk...",
            "...kwrrrrrrwk...",
            "....kwrrrrwk....",
            ".....kkkkkk.....",
            "................",
            "................"
        };

        public static readonly string[] Sword = {
            "................",
            ".......kk.......",
            "......kwwk......",
            "......kwwk......",
            "......kwwk......",
            "......kwwk......",
            "......kwwk......",
            "......kwwk......",
            "......kwwk......",
            "....kkttttkk....",
            "......kbbk......",
            "......kbbk......",
            "......kbbk......",
            ".....kttttk.....",
            "................",
            "................"
        };

        public static readonly string[] Shield = {
            "................",
            "................",
            "....kkkkkkkk....",
            "...kwwwwwwwwk...",
            "...kwbbbbbbwk...",
            "...kwbttttbwk...",
            "...kwbttttbwk...",
            "...kwbbbbbbwk...",
            "....kwbbbbwk....",
            "....kwbbbbwk....",
            ".....kwbbwk.....",
            ".....kwbbwk.....",
            "......kwwk......",
            ".......kk.......",
            "................",
            "................"
        };

        public static readonly string[] Key = {
            "................",
            "................",
            "....kkkk........",
            "...kttttk.......",
            "..kttkkttk......",
            "..kttkkttk......",
            "...kttttk.......",
            "....kttk........",
            ".....ktk........",
            ".....ktk........",
            ".....kttk.......",
            ".....ktk........",
            ".....kttk.......",
            ".....kkk........",
            "................",
            "................"
        };

        // ---------------------------------------------------------------
        // Deterministic noise so tiles never reshuffle between bakes
        // ---------------------------------------------------------------
        static float Noise(int x, int y, int salt)
        {
            float n = Mathf.Sin(x * 127.1f + y * 311.7f + salt * 74.7f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        static void GrassTile(Bitmap b, int px, int py, bool dark)
        {
            Color32 baseCol = dark ? C("#2f5527") : C("#3f6b34");
            Color32 lit = dark ? C("#3d6b31") : C("#4a7c3f");
            Color32 shade = dark ? C("#264420") : C("#35592c");
            b.FillRect(px, py, 16, 16, baseCol);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float r = Noise(px + x, py + y, 1);
                    if (r > 0.90f) b.Set(px + x, py + y, lit);
                    else if (r < 0.05f) b.Set(px + x, py + y, shade);
                }
        }

        static void TallGrassTile(Bitmap b, int px, int py)
        {
            GrassTile(b, px, py, true);
            Color32 blade = C("#4f8742");
            for (int i = 0; i < 5; i++)
            {
                int bx = px + Mathf.FloorToInt(Noise(px, py, i + 2) * 14f) + 1;
                int bh = 4 + Mathf.FloorToInt(Noise(px, py, i + 9) * 5f);
                for (int k = 0; k < bh; k++) b.Set(bx, py + 15 - k, blade);
                for (int k = 0; k < bh - 2; k++) b.Set(bx + 1, py + 15 - k, blade);
            }
        }

        static void PathTile(Bitmap b, int px, int py)
        {
            b.FillRect(px, py, 16, 16, C("#a2865c"));
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float r = Noise(px + x, py + y, 3);
                    if (r > 0.88f) b.Set(px + x, py + y, C("#b89c70"));
                    else if (r < 0.10f) b.Set(px + x, py + y, C("#8a7049"));
                }
        }

        // ---------------------------------------------------------------
        // The sample map. One grid drives the texture, the colliders and
        // every object placement, so they can never drift apart.
        //   # tree/solid   G grass   P path   T tall grass (encounter zone)
        // ---------------------------------------------------------------
        public const int MapCols = 40;
        public const int MapRows = 30;

        public static char[,] BuildMapGrid()
        {
            var g = new char[MapRows, MapCols];

            for (int r = 0; r < MapRows; r++)
                for (int c = 0; c < MapCols; c++)
                {
                    bool border = r < 2 || r >= MapRows - 2 || c < 2 || c >= MapCols - 2;
                    if (border) { g[r, c] = '#'; continue; }
                    if (c >= 22 && r >= 14) { g[r, c] = 'T'; continue; }   // encounter field
                    if (c == 18 || c == 19) { g[r, c] = 'P'; continue; }   // north-south road
                    if (r == 8 && c < 19) { g[r, c] = 'P'; continue; }     // spur into the village
                    g[r, c] = 'G';
                }

            // A few trees breaking up the open ground.
            int[,] trees = { {5,5}, {6,12}, {11,4}, {13,15}, {21,6}, {24,11}, {16,25}, {9,30}, {5,33} };
            for (int i = 0; i < trees.GetLength(0); i++)
            {
                int r = trees[i, 0], c = trees[i, 1];
                if (g[r, c] == 'G') g[r, c] = '#';
            }
            return g;
        }

        public static Bitmap BuildMapBitmap(char[,] grid)
        {
            var b = new Bitmap(MapCols * 16, MapRows * 16);
            var pal = Palette();

            for (int r = 0; r < MapRows; r++)
                for (int c = 0; c < MapCols; c++)
                {
                    int px = c * 16, py = r * 16;
                    char t = grid[r, c];
                    if (t == 'P') PathTile(b, px, py);
                    else if (t == 'T') TallGrassTile(b, px, py);
                    else GrassTile(b, px, py, false);

                    if (t == '#') b.Blit(Tree, pal, px, py);
                }
            return b;
        }

        public static Bitmap BuildBattleBackdrop()
        {
            var b = new Bitmap(320, 240);

            // Night sky, top-down gradient.
            for (int y = 0; y < 150; y++)
            {
                float t = y / 150f;
                Color32 col = Color32.Lerp(C("#141a33"), C("#3b3f66"), t);
                b.FillRect(0, y, 320, 1, col);
            }

            // Stars.
            for (int i = 0; i < 70; i++)
            {
                int sx = Mathf.FloorToInt(Noise(i, 7, 5) * 320f);
                int sy = Mathf.FloorToInt(Noise(i, 13, 6) * 120f);
                b.Set(sx, sy, Noise(i, 3, 8) > 0.6f ? C("#e8e4ff") : C("#9aa0cc"));
            }

            // Moon: a light disc with a darker disc bitten out of it.
            for (int y = -14; y <= 14; y++)
                for (int x = -14; x <= 14; x++)
                {
                    if (x * x + y * y > 13 * 13) continue;
                    int px = 266 + x, py = 34 + y;
                    int dx = px - 260, dy = py - 30;
                    b.Set(px, py, dx * dx + dy * dy <= 11 * 11 ? C("#2c3157") : C("#f2eddc"));
                }

            // Far hills.
            for (int x = 0; x < 320; x++)
            {
                int hh = Mathf.RoundToInt(22 + Mathf.Sin(x * 0.031f) * 10f + Mathf.Sin(x * 0.011f) * 7f);
                for (int y = 150 - hh; y < 150; y++) b.Set(x, y, C("#242a45"));
            }

            // Ground.
            b.FillRect(0, 150, 320, 90, C("#2c3b26"));
            for (int y = 150; y < 240; y++)
                for (int x = 0; x < 320; x++)
                {
                    float r = Noise(x, y, 11);
                    if (r > 0.955f) b.Set(x, y, C("#374a2f"));
                    else if (r < 0.03f) b.Set(x, y, C("#222e1d"));
                }
            return b;
        }

        public static Bitmap BuildPortrait(Dictionary<char, Color32> pal)
        {
            var b = new Bitmap(32, 32);
            for (int y = 0; y < 32; y++)
            {
                Color32 col = Color32.Lerp(C("#3a4a7a"), C("#1a2340"), y / 32f);
                b.FillRect(0, y, 32, 1, col);
            }
            // Zoom the head and shoulders of the character sprite 2x.
            var src = new Bitmap(16, 16);
            src.Blit(Hero, pal, 0, 0);
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 14; x++)
                {
                    Color32 c = src.pixels[(y + 1) * 16 + (x + 1)];
                    if (c.a == 0) continue;
                    b.FillRect(2 + x * 2, 2 + y * 2, 2, 2, c);
                }
            return b;
        }

        // ---------------------------------------------------------------
        // Baking
        // ---------------------------------------------------------------
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static Sprite SaveSprite(Bitmap bmp, string name)
        {
            EnsureFolder(ArtFolder);
            string path = ArtFolder + "/" + name + ".png";
            var tex = bmp.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PPU;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite SaveSprite(string[] art, Dictionary<char, Color32> pal, string name)
        {
            var b = new Bitmap(16, 16);
            b.Blit(art, pal, 0, 0);
            return SaveSprite(b, name);
        }

        /// <summary>Every placeholder sprite the sample scene needs.</summary>
        public class Baked
        {
            public Sprite heroBlue, heroRed, heroTeal, heroPurple;
            public Sprite villager, elder;
            public Sprite chestClosed, chestOpen;
            public Sprite slimeGreen, slimeRed;
            public Sprite potion, sword, shield, key;
            public Sprite portraitHero, portraitElder, portraitVillager;
            public Sprite map, backdrop;
        }

        public static Baked BakeAll()
        {
            var heroPal     = Palette();
            var redPal      = With('b', "#b04a40", 'B', "#7d322b", 'h', "#3a2a20", 'H', "#5c4232");
            var tealPal     = With('b', "#3f9a94", 'B', "#2a6b68", 'h', "#2f2622", 'H', "#4a3a30");
            var purplePal   = With('b', "#7c5aa8", 'B', "#553a78", 'h', "#c8a24a", 'H', "#e5c351");
            var villagerPal = With('b', "#5f8f4c", 'B', "#3f6432", 'h', "#8f8f8f", 'H', "#bcbcbc");
            var elderPal    = With('b', "#8a7fa8", 'B', "#5f5678", 'h', "#d8d8d8", 'H', "#f0f0f0");
            var redSlimePal = With('g', "#c1503f", 'G', "#9c3a2c");

            var baked = new Baked();
            baked.heroBlue   = SaveSprite(Hero, heroPal, "char_hero");
            baked.heroRed    = SaveSprite(Hero, redPal, "char_corin");
            baked.heroTeal   = SaveSprite(Hero, tealPal, "char_yves");
            baked.heroPurple = SaveSprite(Hero, purplePal, "char_sable");
            baked.villager   = SaveSprite(Hero, villagerPal, "npc_villager");
            baked.elder      = SaveSprite(Hero, elderPal, "npc_elder");

            baked.chestClosed = SaveSprite(ChestClosed, heroPal, "obj_chest_closed");
            baked.chestOpen   = SaveSprite(ChestOpen, heroPal, "obj_chest_open");

            baked.slimeGreen = SaveSprite(Slime, heroPal, "enemy_slime");
            baked.slimeRed   = SaveSprite(Slime, redSlimePal, "enemy_great_slime");

            baked.potion = SaveSprite(Potion, heroPal, "icon_medicine");
            baked.sword  = SaveSprite(Sword, heroPal, "icon_iron_blade");
            baked.shield = SaveSprite(Shield, heroPal, "icon_kite_shield");
            baked.key    = SaveSprite(Key, heroPal, "icon_gate_key");

            baked.portraitHero     = SaveSprite(BuildPortrait(heroPal), "portrait_hero");
            baked.portraitElder    = SaveSprite(BuildPortrait(elderPal), "portrait_elder");
            baked.portraitVillager = SaveSprite(BuildPortrait(villagerPal), "portrait_villager");

            baked.map      = SaveSprite(BuildMapBitmap(BuildMapGrid()), "map_field");
            baked.backdrop = SaveSprite(BuildBattleBackdrop(), "battle_backdrop");

            AssetDatabase.SaveAssets();
            return baked;
        }
    }
}
