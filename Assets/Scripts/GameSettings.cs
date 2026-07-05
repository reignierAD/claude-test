using System;
using UnityEngine;

/// <summary>
/// Designer-facing configuration asset. Create one via
/// "Tools > Raggler > Create Game Settings Asset" (or Assets > Create >
/// Raggler > Game Settings, placed in a Resources folder) and tweak
/// everything in the Inspector: card pictures, varieties, background,
/// per-level difficulty and Endless mode rules.
/// When no asset exists the game falls back to built-in defaults.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Raggler/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Serializable]
    public class CardKindDef
    {
        public string cardName = "Card";
        [Tooltip("Card picture. Leave empty to use a colored placeholder circle.")]
        public Sprite sprite;
        [Tooltip("Placeholder color used when no sprite is assigned.")]
        public Color color = Color.white;

        public CardKindDef() { }

        public CardKindDef(string name, Color color)
        {
            cardName = name;
            this.color = color;
        }
    }

    [Serializable]
    public class LevelDef
    {
        [Tooltip("Total cards dealt (rounded up to a multiple of 3).")]
        public int tiles = 24;
        [Tooltip("How many different card kinds appear in this level.")]
        public int cardVarieties = 3;
        [Range(1, 6)]
        public int layers = 3;
        [Tooltip("Finish under this many seconds for 3 stars.")]
        public float threeStarTime = 40f;
        [Tooltip("Finish under this many seconds for 2 stars (slower = 1 star).")]
        public float twoStarTime = 80f;
        [Tooltip("Face-down cards in EACH side pile beside the clearing zone (0 = no piles).")]
        public int sideStackCards = 0;

        public LevelDef() { }

        public LevelDef(int tiles, int cardVarieties, int layers, float threeStarTime, float twoStarTime, int sideStackCards = 0)
        {
            this.tiles = tiles;
            this.cardVarieties = cardVarieties;
            this.layers = layers;
            this.threeStarTime = threeStarTime;
            this.twoStarTime = twoStarTime;
            this.sideStackCards = sideStackCards;
        }
    }

    [Header("Cards")]
    [Tooltip("The pool of card kinds. Add entries and assign sprites to use your own pictures.")]
    public CardKindDef[] cardKinds;

    [Header("Background")]
    [Tooltip("Optional full-screen background image. Leave empty for the flat color below.")]
    public Sprite backgroundSprite;
    public Color backgroundColor = new Color(1.00f, 0.95f, 0.86f);

    [Header("Card Back & Power-up Icons")]
    [Tooltip("Picture shown on face-down cards in the side piles. Leave empty for the plain generated back.")]
    public Sprite cardBackSprite;
    [Tooltip("Optional icons drawn on the Remove / Undo / Refresh buttons in-game.")]
    public Sprite removeIcon;
    public Sprite undoIcon;
    public Sprite refreshIcon;

    [Header("Menu Peekaboo")]
    [Tooltip("Images that randomly peek from the screen edges on the main menu (memes welcome!). Empty = disabled.")]
    public Sprite[] peekabooSprites;
    [Tooltip("Seconds between appearances (random in this range).")]
    public float peekabooMinInterval = 10f;
    public float peekabooMaxInterval = 15f;
    [Tooltip("How long the image stays peeked out, in seconds.")]
    public float peekabooShowTime = 2.5f;
    [Tooltip("Approximate size of the peekaboo image in UI pixels (reference canvas is 1600x900). They render in front of the UI and can be tapped to shoo them away.")]
    public float peekabooSize = 770f;

    [Header("Levels")]
    [Tooltip("One entry per level. Card varieties per level are set here (e.g. 3 in level 1, 5 in level 2...).")]
    public LevelDef[] levels;

    [Header("Endless Mode")]
    [Tooltip("Fixed number of card varieties used in every Endless board.")]
    public int endlessVarieties = 8;
    [Tooltip("Each Endless board deals a random card count in this range (rounded to a multiple of 3).")]
    public int endlessMinTiles = 30;
    public int endlessMaxTiles = 51;
    [Range(1, 6)]
    public int endlessLayers = 5;
    [Tooltip("Endless run length in seconds. When it hits zero the run ends and the remaining stars become your reward.")]
    public float endlessDuration = 180f;
    [Tooltip("After this many seconds the 3rd star is lost (2 remain).")]
    public float endlessStar3Time = 60f;
    [Tooltip("After this many seconds the 2nd star is also lost (1 remains).")]
    public float endlessStar2Time = 120f;
    [Tooltip("Face-down cards in EACH side pile beside the clearing zone during Endless.")]
    public int endlessSideStackCards = 10;

    public int LevelCount => levels != null ? levels.Length : 0;

    public LevelDef GetLevel(int index)
    {
        return levels[Mathf.Clamp(index, 0, levels.Length - 1)];
    }

    /// <summary>Builds one Endless board definition (fixed variety pool, random size).</summary>
    public LevelDef GetEndlessRound(System.Random rng)
    {
        int min = Mathf.Max(3, Mathf.Min(endlessMinTiles, endlessMaxTiles));
        int max = Mathf.Max(min, Mathf.Max(endlessMinTiles, endlessMaxTiles));
        int tiles = min + rng.Next(max - min + 1);
        return new LevelDef(tiles, endlessVarieties, endlessLayers, 0f, 0f, endlessSideStackCards);
    }

    /// <summary>Fills the asset with the built-in Ragnarok-flavored defaults.</summary>
    public void PopulateDefaults()
    {
        cardKinds = new[]
        {
            new CardKindDef("Poring",   new Color(1.00f, 0.55f, 0.75f)),
            new CardKindDef("Drops",    new Color(1.00f, 0.62f, 0.25f)),
            new CardKindDef("Poporing", new Color(0.45f, 0.80f, 0.35f)),
            new CardKindDef("Lunatic",  new Color(0.88f, 0.88f, 0.94f)),
            new CardKindDef("Spore",    new Color(0.90f, 0.30f, 0.28f)),
            new CardKindDef("Marin",    new Color(0.35f, 0.60f, 0.95f)),
            new CardKindDef("Metaling", new Color(0.55f, 0.58f, 0.65f)),
            new CardKindDef("Angeling", new Color(1.00f, 0.84f, 0.30f)),
        };

        levels = new[]
        {
            new LevelDef( 9, 3, 2,  20f,  40f),
            new LevelDef(18, 5, 2,  30f,  60f),
            new LevelDef(24, 5, 3,  40f,  80f),
            new LevelDef(30, 6, 3,  50f,  95f,  6),
            new LevelDef(39, 6, 3,  60f, 110f),
            new LevelDef(48, 7, 4,  75f, 130f,  6),
            new LevelDef(57, 7, 4,  90f, 150f),
            new LevelDef(66, 8, 4, 105f, 170f,  9),
            new LevelDef(78, 8, 5, 120f, 195f,  9),
            new LevelDef(90, 8, 5, 140f, 220f, 10),
        };

        backgroundColor = new Color(1.00f, 0.95f, 0.86f);
        endlessVarieties = 8;
        endlessMinTiles = 30;
        endlessMaxTiles = 51;
        endlessLayers = 5;
        endlessDuration = 180f;
        endlessStar3Time = 60f;
        endlessStar2Time = 120f;
        endlessSideStackCards = 10;
    }
}
