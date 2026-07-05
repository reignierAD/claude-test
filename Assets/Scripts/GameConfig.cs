using UnityEngine;

/// <summary>
/// Access point for configuration. Designer-tunable data lives in the
/// GameSettings asset (Resources/GameSettings); engine-level rules that the
/// gameplay code depends on stay here as constants.
/// </summary>
public static class GameConfig
{
    static GameSettings _settings;

    /// <summary>
    /// The active settings asset. Loaded from Resources/GameSettings when
    /// present, otherwise an in-memory instance with built-in defaults.
    /// </summary>
    public static GameSettings S
    {
        get
        {
            if (_settings == null)
            {
                _settings = Resources.Load<GameSettings>("GameSettings");
                if (_settings == null)
                {
                    _settings = ScriptableObject.CreateInstance<GameSettings>();
                    _settings.PopulateDefaults();
                }
            }
            return _settings;
        }
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
