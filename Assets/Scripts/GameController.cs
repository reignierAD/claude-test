using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the whole game: menu, HUD, gameplay rules, items, scoring,
/// stage progression, Endless mode and persistence. All UI is built from
/// code — the project needs no prefabs or imported assets.
/// </summary>
public class GameController : MonoBehaviour
{
    enum State { Menu, Playing, Finished }

    // ---- theme ----
    static readonly Color Cream = new Color(1.00f, 0.95f, 0.86f);
    static readonly Color CreamDark = new Color(0.99f, 0.88f, 0.72f);
    static readonly Color Orange = new Color(0.96f, 0.62f, 0.25f);
    static readonly Color DeepOrange = new Color(0.93f, 0.45f, 0.30f);
    static readonly Color Brown = new Color(0.45f, 0.30f, 0.15f);
    static readonly Color StarGold = new Color(1.00f, 0.78f, 0.15f);
    static readonly Color StarDim = new Color(0.62f, 0.56f, 0.50f, 0.55f);
    static readonly Color ButtonRose = new Color(0.95f, 0.55f, 0.50f);

    // ---- scene refs ----
    RectTransform _canvasRoot;
    RectTransform _menuScreen;
    RectTransform _gameScreen;
    RectTransform _boardRoot;
    RectTransform _trayRoot;
    RectTransform _holdRoot;
    RectTransform _popupLayer;

    // ---- gameplay ----
    readonly Board _board = new Board();
    readonly Tray _tray = new Tray();
    readonly List<Card> _held = new List<Card>();
    readonly List<Card> _undoStack = new List<Card>();
    readonly System.Random _rng = new System.Random();

    State _state = State.Menu;
    GameConfig.StageDef _currentDef;
    int _stageIndex;        // 0-based; -1 while in Endless mode
    int _endlessRound;
    float _elapsed;
    int _score;
    int _combo;
    float _lastMatchTime = -999f;

    int _usedRemove, _usedUndo, _usedRefresh;
    int _invRemove, _invUndo, _invRefresh;

    // ---- HUD refs ----
    Text _stageText, _timerText, _scoreText, _toastText;
    readonly Image[] _hudStars = new Image[3];
    Button _removeBtn, _undoBtn, _refreshBtn;
    Text _removeInv, _undoInv, _refreshInv;
    Text _removeUsed, _undoUsed, _refreshUsed;
    float _toastUntil;

    // =====================================================================
    // setup
    // =====================================================================

    public void Setup(Canvas canvas)
    {
        _canvasRoot = (RectTransform)canvas.transform;
        LoadInventory();
        BuildBackground();
        BuildGameScreen();
        _tray.onTriple = HandleTriple;
        ShowMenu();
    }

    void BuildBackground()
    {
        var bg = Ui.Stretch("Background", _canvasRoot);
        var img = bg.gameObject.AddComponent<Image>();
        img.color = Cream;

        // soft decorative blobs, roughly matching the event screen's warm look
        Deco(bg, new Vector2(-620, 380), 500, new Color(1f, 0.80f, 0.55f, 0.35f));
        Deco(bg, new Vector2(660, -400), 640, new Color(1f, 0.72f, 0.45f, 0.30f));
        Deco(bg, new Vector2(560, 400), 360, new Color(1f, 0.86f, 0.60f, 0.30f));
        Deco(bg, new Vector2(-640, -420), 420, new Color(1f, 0.86f, 0.60f, 0.25f));
    }

    static void Deco(Transform parent, Vector2 pos, float size, Color color)
    {
        var rt = Ui.Rect("Deco", parent, new Vector2(size, size), pos);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = SpriteFactory.Circle;
        img.color = color;
        img.raycastTarget = false;
    }

    // =====================================================================
    // menu
    // =====================================================================

    void ShowMenu()
    {
        _state = State.Menu;
        _gameScreen.gameObject.SetActive(false);
        ClosePopup();
        if (_menuScreen != null) Destroy(_menuScreen.gameObject);

        _menuScreen = Ui.Stretch("Menu", _canvasRoot);

        Ui.Label("Title", _menuScreen, "Raggler's Challenge", 68, DeepOrange,
            new Vector2(0, 330), new Vector2(1200, 90), anchor: new Vector2(0.5f, 0.5f));
        Ui.Label("Subtitle", _menuScreen, "Raggler made off with everyone's gifts! Match three to take them back!",
            26, Brown, new Vector2(0, 262), new Vector2(1200, 40), style: FontStyle.Normal);

        // total stars, top right
        int total = 0;
        for (int i = 0; i < GameConfig.Stages.Length; i++) total += StageStars(i);
        var starRt = Ui.Rect("TotalStar", _menuScreen, new Vector2(44, 44), new Vector2(-160, -50), new Vector2(1f, 1f));
        var starImg = starRt.gameObject.AddComponent<Image>();
        starImg.sprite = SpriteFactory.Star;
        starImg.color = StarGold;
        starImg.raycastTarget = false;
        Ui.Label("TotalStars", _menuScreen, total + "/" + (GameConfig.Stages.Length * 3), 30, Brown,
            new Vector2(-95, -50), new Vector2(120, 44), TextAnchor.MiddleLeft, anchor: new Vector2(1f, 1f));

        // help
        Ui.MakeButton("Help", _menuScreen, "?", new Vector2(56, 56), new Vector2(-50, -50),
            Orange, Color.white, 32, ShowHelpPopup, new Vector2(1f, 1f));

        // stage grid
        for (int i = 0; i < GameConfig.Stages.Length; i++)
        {
            int idx = i;
            float x = (i % 5 - 2) * 190f;
            float y = 110f - (i / 5) * 205f;
            bool unlocked = idx == 0 || StageStars(idx - 1) > 0;

            var btn = Ui.MakeButton("Stage" + (idx + 1), _menuScreen, "",
                new Vector2(160, 160), new Vector2(x, y),
                unlocked ? Color.white : new Color(0.85f, 0.80f, 0.74f),
                Brown, 30, () => StartStage(idx));
            btn.interactable = unlocked;

            Ui.Label("Num", btn.transform, (idx + 1).ToString(), 52,
                unlocked ? DeepOrange : new Color(0.6f, 0.55f, 0.5f),
                new Vector2(0, 18), new Vector2(160, 60));

            int earned = StageStars(idx);
            for (int s = 0; s < 3; s++)
            {
                var srt = Ui.Rect("S" + s, btn.transform, new Vector2(34, 34), new Vector2((s - 1) * 38f, -44));
                var simg = srt.gameObject.AddComponent<Image>();
                simg.sprite = SpriteFactory.Star;
                simg.color = s < earned ? StarGold : StarDim;
                simg.raycastTarget = false;
            }
        }

        // endless mode
        var endless = Ui.MakeButton("Endless", _menuScreen, "", new Vector2(360, 110), new Vector2(0, -330),
            ButtonRose, Color.white, 30, StartEndless);
        Ui.Label("EndlessLabel", endless.transform, "Endless Mode", 34, Color.white, new Vector2(0, 16), new Vector2(360, 44));
        Ui.Label("EndlessBest", endless.transform, "Best Score: " + PlayerPrefs.GetInt("endless_best", 0),
            22, new Color(1f, 0.93f, 0.85f), new Vector2(0, -24), new Vector2(360, 30), style: FontStyle.Normal);

        // reset progress (handy while testing)
        Ui.MakeButton("Reset", _menuScreen, "Reset Progress", new Vector2(190, 46), new Vector2(120, 45),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 20, ResetProgress, new Vector2(0f, 0f));
    }

    void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        LoadInventory();
        ShowMenu();
    }

    // =====================================================================
    // game screen (built once, reused)
    // =====================================================================

    void BuildGameScreen()
    {
        _gameScreen = Ui.Stretch("Game", _canvasRoot);

        // back + stage name (top left)
        Ui.MakeButton("Back", _gameScreen, "<", new Vector2(60, 60), new Vector2(55, -55),
            Orange, Color.white, 34, ExitToMenu, new Vector2(0f, 1f));
        _stageText = Ui.Label("Stage", _gameScreen, "Stage 1", 36, Brown,
            new Vector2(210, -55), new Vector2(300, 50), TextAnchor.MiddleLeft, anchor: new Vector2(0f, 1f));

        // timer + stars (top center)
        var timerBg = Ui.Rect("TimerBg", _gameScreen, new Vector2(190, 54), new Vector2(0, -42), new Vector2(0.5f, 1f));
        Ui.Panel(timerBg, CreamDark);
        _timerText = Ui.Label("Timer", timerBg, "00:00", 32, Brown, Vector2.zero, new Vector2(190, 54));
        for (int s = 0; s < 3; s++)
        {
            var srt = Ui.Rect("HudStar" + s, _gameScreen, new Vector2(42, 42), new Vector2((s - 1) * 48f, -95), new Vector2(0.5f, 1f));
            _hudStars[s] = srt.gameObject.AddComponent<Image>();
            _hudStars[s].sprite = SpriteFactory.Star;
            _hudStars[s].color = StarGold;
            _hudStars[s].raycastTarget = false;
        }

        // score (top right)
        _scoreText = Ui.Label("Score", _gameScreen, "Score  0", 32, Brown,
            new Vector2(-160, -55), new Vector2(300, 50), TextAnchor.MiddleRight, anchor: new Vector2(1f, 1f));

        // item buttons (right side)
        var itemPanel = Ui.Rect("Items", _gameScreen, new Vector2(160, 470), new Vector2(-105, 40), new Vector2(1f, 0.5f));
        Ui.Panel(itemPanel, new Color(0.98f, 0.80f, 0.50f, 0.85f));
        _removeBtn = BuildItemButton(itemPanel, "Remove", 150f, UseRemove, out _removeInv, out _removeUsed);
        _undoBtn = BuildItemButton(itemPanel, "Undo", 0f, UseUndo, out _undoInv, out _undoUsed);
        _refreshBtn = BuildItemButton(itemPanel, "Refresh", -150f, UseRefresh, out _refreshInv, out _refreshUsed);

        // board
        _boardRoot = Ui.Rect("Board", _gameScreen, new Vector2(10, 10), new Vector2(-40, 60));

        // hold area (bottom left) — Remove item drops cards here
        _holdRoot = Ui.Rect("Hold", _gameScreen, new Vector2(370, 132), new Vector2(-480, 105), new Vector2(0.5f, 0f));
        Ui.Panel(_holdRoot, new Color(0.45f, 0.40f, 0.36f, 0.45f));

        // clearing zone (bottom center)
        _trayRoot = Ui.Rect("Tray", _gameScreen, new Vector2(830, 132), new Vector2(160, 105), new Vector2(0.5f, 0f));
        Ui.Panel(_trayRoot, new Color(0.98f, 0.72f, 0.35f, 0.95f));

        // toast / combo text
        _toastText = Ui.Label("Toast", _gameScreen, "", 38, DeepOrange,
            new Vector2(-40, 330), new Vector2(900, 60));
        var outline = _toastText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        _toastText.enabled = false;

        _gameScreen.gameObject.SetActive(false);
    }

    Button BuildItemButton(Transform parent, string label, float y, System.Action onClick, out Text invText, out Text usedText)
    {
        var btn = Ui.MakeButton(label, parent, "", new Vector2(130, 120), new Vector2(0, y),
            Color.white, Brown, 24, onClick);
        Ui.Label("Name", btn.transform, label, 24, Brown, new Vector2(0, 30), new Vector2(130, 32));

        // inventory badge
        var badge = Ui.Rect("Badge", btn.transform, new Vector2(46, 46), new Vector2(52, 48));
        var bimg = badge.gameObject.AddComponent<Image>();
        bimg.sprite = SpriteFactory.Circle;
        bimg.color = DeepOrange;
        bimg.raycastTarget = false;
        invText = Ui.Label("Inv", badge, "0", 22, Color.white, Vector2.zero, new Vector2(46, 46));

        usedText = Ui.Label("Used", btn.transform, "0/" + GameConfig.ItemUseCapPerStage, 22,
            new Color(0.55f, 0.45f, 0.35f), new Vector2(0, -32), new Vector2(130, 30), style: FontStyle.Normal);
        return btn;
    }

    // =====================================================================
    // session control
    // =====================================================================

    void StartStage(int index)
    {
        _stageIndex = index;
        _endlessRound = 0;
        BeginSession(GameConfig.Stages[index]);
    }

    void StartEndless()
    {
        _stageIndex = -1;
        _endlessRound = 1;
        BeginSession(GameConfig.EndlessRound(1));
    }

    void BeginSession(GameConfig.StageDef def)
    {
        ClosePopup();
        if (_menuScreen != null) Destroy(_menuScreen.gameObject);
        _gameScreen.gameObject.SetActive(true);

        ClearTable();
        _elapsed = 0f;
        _score = 0;
        _combo = 0;
        _lastMatchTime = -999f;
        _usedRemove = _usedUndo = _usedRefresh = 0;

        DealBoard(def);
        _state = State.Playing;
        UpdateHud();
    }

    void DealBoard(GameConfig.StageDef def)
    {
        _currentDef = def;
        _board.Generate(_boardRoot, def, _rng, OnCardClicked);
    }

    void ClearTable()
    {
        _board.Clear();
        _tray.Clear();
        foreach (var c in _held)
            if (c != null) Destroy(c.gameObject);
        _held.Clear();
        _undoStack.Clear();
    }

    void ExitToMenu()
    {
        ClearTable();
        ShowMenu();
    }

    // =====================================================================
    // core play
    // =====================================================================

    void OnCardClicked(Card card)
    {
        if (_state != State.Playing || card.removed || card.inTray) return;

        if (card.inHold)
        {
            if (_tray.IsFull)
            {
                Toast("The clearing zone is full!");
                return;
            }
            _held.Remove(card);
            card.inHold = false;
            PlaceInTray(card);
            RelayoutHold();
            return;
        }

        // board card
        _board.Take(card);
        _undoStack.Add(card);
        PlaceInTray(card);
        _board.RecomputeBlocked();
    }

    void PlaceInTray(Card card)
    {
        card.inTray = true;
        card.SetBlocked(false);
        card.button.interactable = false;
        card.transform.SetParent(_trayRoot, true);
        card.transform.SetAsLastSibling();

        bool survived = _tray.Add(card);
        UpdateHud();

        if (!survived)
        {
            Lose();
            return;
        }
        CheckCleared();
    }

    void HandleTriple(int typeIndex)
    {
        _combo = (Time.time - _lastMatchTime <= GameConfig.ComboWindow)
            ? Mathf.Min(_combo + 1, GameConfig.MaxCombo)
            : 1;
        _lastMatchTime = Time.time;

        int points = GameConfig.MatchScore * _combo;
        _score += points;
        Toast(_combo > 1 ? "+" + points + "   Combo x" + _combo + "!" : "+" + points);
    }

    void CheckCleared()
    {
        if (_board.Count > 0 || _held.Count > 0 || _tray.cards.Count > 0) return;

        if (_stageIndex >= 0) WinStage();
        else AdvanceEndless();
    }

    void WinStage()
    {
        _state = State.Finished;

        var def = GameConfig.Stages[_stageIndex];
        int stars = _elapsed <= def.time3 ? 3 : _elapsed <= def.time2 ? 2 : 1;
        int bonus = Mathf.Max(0, Mathf.RoundToInt(def.time2 - _elapsed)) * GameConfig.TimeBonusPerSecond;
        _score += bonus;

        string key = "stars_" + (_stageIndex + 1);
        if (stars > PlayerPrefs.GetInt(key, 0)) PlayerPrefs.SetInt(key, stars);

        // small item reward for clearing a stage
        _invRemove++;
        _invUndo++;
        _invRefresh++;
        SaveInventory();

        ShowWinPopup(stars, bonus);
    }

    void AdvanceEndless()
    {
        int bonus = GameConfig.EndlessRoundBonus * _endlessRound;
        _score += bonus;
        Toast("Round " + _endlessRound + " clear!  +" + bonus);
        _endlessRound++;
        _undoStack.Clear();
        DealBoard(GameConfig.EndlessRound(_endlessRound));
        UpdateHud();
    }

    void Lose()
    {
        _state = State.Finished;

        if (_stageIndex < 0)
        {
            int best = PlayerPrefs.GetInt("endless_best", 0);
            if (_score > best)
            {
                best = _score;
                PlayerPrefs.SetInt("endless_best", best);
                PlayerPrefs.Save();
            }
            ShowEndlessOverPopup(best);
        }
        else
        {
            ShowLosePopup();
        }
    }

    // =====================================================================
    // items
    // =====================================================================

    bool CanUseRemove => _state == State.Playing && _invRemove > 0 &&
        _usedRemove < GameConfig.ItemUseCapPerStage && _held.Count == 0 && _tray.cards.Count > 0;

    bool CanUseUndo => _state == State.Playing && _invUndo > 0 &&
        _usedUndo < GameConfig.ItemUseCapPerStage && HasUndoTarget();

    bool CanUseRefresh => _state == State.Playing && _invRefresh > 0 &&
        _usedRefresh < GameConfig.ItemUseCapPerStage && _board.Count > 1;

    bool HasUndoTarget()
    {
        for (int i = _undoStack.Count - 1; i >= 0; i--)
        {
            var c = _undoStack[i];
            if (c != null && !c.removed && c.inTray) return true;
        }
        return false;
    }

    void UseRemove()
    {
        if (!CanUseRemove) return;

        var taken = _tray.TakeFromFront(GameConfig.HoldSize);
        foreach (var c in taken)
        {
            c.inTray = false;
            c.inHold = true;
            c.transform.SetParent(_holdRoot, true);
            c.button.interactable = true;
            _held.Add(c);
        }
        RelayoutHold();

        _invRemove--;
        _usedRemove++;
        SaveInventory();
        UpdateHud();
    }

    void UseUndo()
    {
        if (!CanUseUndo) return;

        Card target = null;
        while (_undoStack.Count > 0 && target == null)
        {
            var c = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            if (c != null && !c.removed && c.inTray) target = c;
        }
        if (target == null) return;

        _tray.Remove(target);
        _board.Return(target);

        _invUndo--;
        _usedUndo++;
        SaveInventory();
        UpdateHud();
    }

    void UseRefresh()
    {
        if (!CanUseRefresh) return;

        _board.ShuffleTypes(_rng);

        _invRefresh--;
        _usedRefresh++;
        SaveInventory();
        UpdateHud();
    }

    void RelayoutHold()
    {
        for (int i = 0; i < _held.Count; i++)
            Tween.MoveTo(_held[i].Rect, new Vector2((i - (GameConfig.HoldSize - 1) * 0.5f) * 114f, 0f), 0.18f);
    }

    // =====================================================================
    // HUD / popups
    // =====================================================================

    void Update()
    {
        if (_state == State.Playing)
        {
            _elapsed += Time.deltaTime;
            UpdateHud();
        }
        if (_toastText != null && _toastText.enabled && Time.unscaledTime > _toastUntil)
            _toastText.enabled = false;
    }

    void UpdateHud()
    {
        _timerText.text = FormatTime(_elapsed);
        _scoreText.text = "Score  " + _score;

        if (_stageIndex >= 0)
        {
            _stageText.text = "Stage " + (_stageIndex + 1);
            int would = _elapsed <= _currentDef.time3 ? 3 : _elapsed <= _currentDef.time2 ? 2 : 1;
            for (int s = 0; s < 3; s++)
            {
                _hudStars[s].gameObject.SetActive(true);
                _hudStars[s].color = s < would ? StarGold : StarDim;
            }
        }
        else
        {
            _stageText.text = "Endless  ·  Round " + _endlessRound;
            for (int s = 0; s < 3; s++) _hudStars[s].gameObject.SetActive(false);
        }

        _removeInv.text = _invRemove.ToString();
        _undoInv.text = _invUndo.ToString();
        _refreshInv.text = _invRefresh.ToString();
        _removeUsed.text = _usedRemove + "/" + GameConfig.ItemUseCapPerStage;
        _undoUsed.text = _usedUndo + "/" + GameConfig.ItemUseCapPerStage;
        _refreshUsed.text = _usedRefresh + "/" + GameConfig.ItemUseCapPerStage;
        _removeBtn.interactable = CanUseRemove;
        _undoBtn.interactable = CanUseUndo;
        _refreshBtn.interactable = CanUseRefresh;
    }

    void Toast(string message)
    {
        _toastText.text = message;
        _toastText.enabled = true;
        _toastUntil = Time.unscaledTime + 1.6f;
    }

    static string FormatTime(float t)
    {
        int total = Mathf.FloorToInt(t);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    RectTransform BuildPopup(string title, float height)
    {
        ClosePopup();
        _popupLayer = Ui.Stretch("PopupLayer", _canvasRoot);
        var dim = _popupLayer.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f); // also blocks clicks behind it

        var panel = Ui.Rect("Panel", _popupLayer, new Vector2(680, height), Vector2.zero);
        Ui.Panel(panel, Cream);
        Ui.Label("Title", panel, title, 46, DeepOrange,
            new Vector2(0, height * 0.5f - 60f), new Vector2(640, 60));
        return panel;
    }

    void ClosePopup()
    {
        if (_popupLayer != null) Destroy(_popupLayer.gameObject);
        _popupLayer = null;
    }

    void ShowWinPopup(int stars, int timeBonus)
    {
        var panel = BuildPopup("Stage Clear!", 520);

        for (int s = 0; s < 3; s++)
        {
            var srt = Ui.Rect("Star" + s, panel, new Vector2(86, 86), new Vector2((s - 1) * 100f, 90f));
            var img = srt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.Star;
            img.color = s < stars ? StarGold : StarDim;
            img.raycastTarget = false;
        }

        Ui.Label("Time", panel, "Time  " + FormatTime(_elapsed), 30, Brown,
            new Vector2(0, 10), new Vector2(600, 40), style: FontStyle.Normal);
        Ui.Label("Score", panel, "Score  " + _score + "   (time bonus +" + timeBonus + ")", 30, Brown,
            new Vector2(0, -35), new Vector2(600, 40), style: FontStyle.Normal);
        Ui.Label("Reward", panel, "Reward: +1 Remove, +1 Undo, +1 Refresh", 24,
            new Color(0.55f, 0.45f, 0.35f), new Vector2(0, -80), new Vector2(600, 34), style: FontStyle.Normal);

        int replayIndex = _stageIndex;
        Ui.MakeButton("Menu", panel, "Menu", new Vector2(170, 66), new Vector2(-210, -180),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Replay", panel, "Replay", new Vector2(170, 66), new Vector2(0, -180),
            Orange, Color.white, 26, () => StartStage(replayIndex));
        if (_stageIndex < GameConfig.Stages.Length - 1)
        {
            Ui.MakeButton("Next", panel, "Next", new Vector2(170, 66), new Vector2(210, -180),
                ButtonRose, Color.white, 26, () => StartStage(replayIndex + 1));
        }
    }

    void ShowLosePopup()
    {
        var panel = BuildPopup("Defeat...", 400);
        Ui.Label("Msg", panel, "The clearing zone overflowed!\nRaggler keeps the gifts... for now.", 28, Brown,
            new Vector2(0, 30), new Vector2(600, 90), style: FontStyle.Normal);

        int replayIndex = _stageIndex;
        Ui.MakeButton("Menu", panel, "Menu", new Vector2(190, 66), new Vector2(-110, -120),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Retry", panel, "Retry", new Vector2(190, 66), new Vector2(110, -120),
            ButtonRose, Color.white, 26, () => StartStage(replayIndex));
    }

    void ShowEndlessOverPopup(int best)
    {
        var panel = BuildPopup("Endless Over", 440);
        Ui.Label("Rounds", panel, "Rounds survived: " + _endlessRound, 30, Brown,
            new Vector2(0, 55), new Vector2(600, 40), style: FontStyle.Normal);
        Ui.Label("Score", panel, "Score  " + _score, 34, DeepOrange,
            new Vector2(0, 5), new Vector2(600, 44));
        Ui.Label("Best", panel, "Best  " + best, 26, Brown,
            new Vector2(0, -40), new Vector2(600, 36), style: FontStyle.Normal);

        Ui.MakeButton("Menu", panel, "Menu", new Vector2(190, 66), new Vector2(-110, -140),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Retry", panel, "Retry", new Vector2(190, 66), new Vector2(110, -140),
            ButtonRose, Color.white, 26, StartEndless);
    }

    void ShowHelpPopup()
    {
        var panel = BuildPopup("Notice", 560);
        string rules =
            "Raggler made off with everyone's gifts!\nDefeat him and take back the gifts!\n\n" +
            "1. Tap the cards to place them in the clearing zone below.\n" +
            "2. Match three identical cards to clear them automatically.\n     Clear all cards on the screen to win.\n" +
            "3. The clearing zone can hold up to 7 cards. Go over, and you lose!\n" +
            "4. You may use items: Remove Card, Undo Card, and Refresh Card.\n" +
            "5. The rating for each stage is based on how fast you complete it!";
        Ui.Label("Rules", panel, rules, 24, Brown, new Vector2(0, 20), new Vector2(600, 380),
            TextAnchor.UpperLeft, FontStyle.Normal);
        Ui.MakeButton("Ok", panel, "Got it!", new Vector2(190, 66), new Vector2(0, -220),
            Orange, Color.white, 26, ClosePopup);
    }

    // =====================================================================
    // persistence
    // =====================================================================

    static int StageStars(int index)
    {
        return PlayerPrefs.GetInt("stars_" + (index + 1), 0);
    }

    void LoadInventory()
    {
        if (!PlayerPrefs.HasKey("inv_remove"))
        {
            _invRemove = GameConfig.StartRemove;
            _invUndo = GameConfig.StartUndo;
            _invRefresh = GameConfig.StartRefresh;
            SaveInventory();
        }
        else
        {
            _invRemove = PlayerPrefs.GetInt("inv_remove");
            _invUndo = PlayerPrefs.GetInt("inv_undo");
            _invRefresh = PlayerPrefs.GetInt("inv_refresh");
        }
    }

    void SaveInventory()
    {
        PlayerPrefs.SetInt("inv_remove", _invRemove);
        PlayerPrefs.SetInt("inv_undo", _invUndo);
        PlayerPrefs.SetInt("inv_refresh", _invRefresh);
        PlayerPrefs.Save();
    }
}
