using UnityEngine;

/// <summary>
/// All tuning data for the game: card kinds, stage definitions, scoring rules.
/// </summary>
public static class GameConfig
{
    public struct CardKind
    {
        public string name;
        public Color color;

        public CardKind(string name, Color color)
        {
            this.name = name;
            this.color = color;
        }
    }

    // Ragnarok-flavored card kinds. Icon is a tinted circle, so each kind
    // needs a clearly distinguishable color.
    public static readonly CardKind[] Kinds =
    {
        new CardKind("Poring",   new Color(1.00f, 0.55f, 0.75f)),
        new CardKind("Drops",    new Color(1.00f, 0.62f, 0.25f)),
        new CardKind("Poporing", new Color(0.45f, 0.80f, 0.35f)),
        new CardKind("Lunatic",  new Color(0.88f, 0.88f, 0.94f)),
        new CardKind("Spore",    new Color(0.90f, 0.30f, 0.28f)),
        new CardKind("Marin",    new Color(0.35f, 0.60f, 0.95f)),
        new CardKind("Metaling", new Color(0.55f, 0.58f, 0.65f)),
        new CardKind("Angeling", new Color(1.00f, 0.84f, 0.30f)),
    };

    public struct StageDef
    {
        public int tiles;    // total cards, always a multiple of 3
        public int types;    // how many distinct card kinds are dealt
        public int layers;   // stacking depth of the board
        public float time3;  // finish under this => 3 stars
        public float time2;  // finish under this => 2 stars, above => 1 star

        public StageDef(int tiles, int types, int layers, float time3, float time2)
        {
            this.tiles = tiles;
            this.types = types;
            this.layers = layers;
            this.time3 = time3;
            this.time2 = time2;
        }
    }

    public static readonly StageDef[] Stages =
    {
        new StageDef( 9, 2, 2,  20f,  40f),
        new StageDef(18, 3, 2,  30f,  60f),
        new StageDef(24, 4, 3,  40f,  80f),
        new StageDef(30, 4, 3,  50f,  95f),
        new StageDef(39, 5, 3,  60f, 110f),
        new StageDef(48, 5, 4,  75f, 130f),
        new StageDef(57, 6, 4,  90f, 150f),
        new StageDef(66, 7, 4, 105f, 170f),
        new StageDef(78, 7, 5, 120f, 195f),
        new StageDef(90, 8, 5, 140f, 220f),
    };

    /// <summary>Escalating board definitions for Endless mode (round is 1-based).</summary>
    public static StageDef EndlessRound(int round)
    {
        int tiles = Mathf.Min(24 + round * 9, 90);
        int types = Mathf.Min(3 + round, Kinds.Length);
        int layers = Mathf.Min(2 + (round + 1) / 2, 5);
        return new StageDef(tiles, types, layers, 0f, 0f);
    }

    // Layout
    public const float CardSize = 110f;     // UI pixels
    public const float HalfStep = 55f;      // board grid half-step (cards overlap on a half-grid)

    // Rules
    public const int TraySize = 7;
    public const int HoldSize = 3;
    public const int ItemUseCapPerStage = 10;

    // Scoring
    public const int MatchScore = 30;       // base points per cleared triple
    public const int MaxCombo = 5;          // combo multiplier cap
    public const float ComboWindow = 4f;    // seconds between matches to keep the combo alive
    public const int TimeBonusPerSecond = 10;
    public const int EndlessRoundBonus = 100;

    // Starting item inventory on first launch (a nod to the original screenshots)
    public const int StartRemove = 37;
    public const int StartUndo = 5;
    public const int StartRefresh = 23;
}
