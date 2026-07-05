using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the whole game: menu, HUD, gameplay rules, items, scoring,
/// level progression, Endless mode, side stacks, persistence and the Web3
/// token flow. All UI is built from code; designer data comes from the
/// GameSettings asset (see GameConfig.S).
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
    static readonly Color WalletTeal = new Color(0.25f, 0.62f, 0.58f);
    static readonly Color SoftBrown = new Color(0.55f, 0.45f, 0.35f);

    // ---- scene refs ----
    RectTransform _canvasRoot;
    RectTransform _menuScreen;
    RectTransform _gameScreen;
    RectTransform _boardRoot;
    RectTransform _trayRoot;
    RectTransform _holdRoot;
    RectTransform _leftStackRoot;
    RectTransform _rightStackRoot;
    RectTransform _popupLayer;

    // ---- gameplay ----
    readonly Board _board = new Board();
    readonly Tray _tray = new Tray();
    readonly SideStack _leftStack = new SideStack();
    readonly SideStack _rightStack = new SideStack();
    readonly List<Card> _held = new List<Card>();
    readonly List<Card> _undoStack = new List<Card>();
    readonly System.Random _rng = new System.Random();

    State _state = State.Menu;
    GameSettings.LevelDef _currentDef;
    int _levelIndex;        // 0-based; -1 while in Endless mode
    int _endlessRound;
    int _endlessFinalStars;
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

    // ---- claim UI (inside result popups) ----
    Button _claimButton;
    Text _claimStatusText;

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
        _leftStack.Init(_leftStackRoot, -1f);
        _rightStack.Init(_rightStackRoot, 1f);

        var w3 = Web3Bridge.Instance;
        if (w3 != null)
        {
            w3.onStateChanged += OnWeb3StateChanged;
            w3.onClaimOk += OnWeb3ClaimOk;
            w3.onClaimError += OnWeb3ClaimError;
            w3.onError += msg => Toast(msg);
        }

        ShowMenu();
    }

    void BuildBackground()
    {
        var bg = Ui.Stretch("Background", _canvasRoot);
        var img = bg.gameObject.AddComponent<Image>();
        var s = GameConfig.S;

        if (s.backgroundSprite != null)
        {
            img.sprite = s.backgroundSprite;
            img.color = Color.white;
            return;
        }

        img.color = s.backgroundColor;
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
        int levelCount = GameConfig.S.LevelCount;

        Ui.Label("Title", _menuScreen, "Raggler's Challenge", 68, DeepOrange,
            new Vector2(0, 330), new Vector2(1200, 90));
        Ui.Label("Subtitle", _menuScreen, "Raggler made off with everyone's gifts! Match three to take them back!",
            26, Brown, new Vector2(0, 262), new Vector2(1200, 40), style: FontStyle.Normal);

        // total stars + help, top right (spaced away from the screen edge)
        int total = 0;
        for (int i = 0; i < levelCount; i++) total += LevelStars(i);
        var starRt = Ui.Rect("TotalStar", _menuScreen, new Vector2(42, 42), new Vector2(-290, -62), new Vector2(1f, 1f));
        var starImg = starRt.gameObject.AddComponent<Image>();
        starImg.sprite = SpriteFactory.Star;
        starImg.color = StarGold;
        starImg.raycastTarget = false;
        Ui.Label("TotalStars", _menuScreen, total + "/" + (levelCount * 3), 30, Brown,
            new Vector2(-195, -62), new Vector2(130, 44), TextAnchor.MiddleLeft, anchor: new Vector2(1f, 1f));
        Ui.MakeButton("Help", _menuScreen, "?", new Vector2(56, 56), new Vector2(-70, -62),
            Orange, Color.white, 32, ShowHelpPopup, new Vector2(1f, 1f));

        // level grid (5 per row)
        int rows = (levelCount + 4) / 5;
        for (int i = 0; i < levelCount; i++)
        {
            int idx = i;
            float x = (i % 5 - 2) * 190f;
            float y = 110f - (i / 5) * 205f;
            bool unlocked = idx == 0 || LevelStars(idx - 1) > 0;

            var btn = Ui.MakeButton("Level" + (idx + 1), _menuScreen, "",
                new Vector2(160, 160), new Vector2(x, y),
                unlocked ? Color.white : new Color(0.85f, 0.80f, 0.74f),
                Brown, 30, () => StartLevel(idx));
            btn.interactable = unlocked;

            Ui.Label("Num", btn.transform, (idx + 1).ToString(), 52,
                unlocked ? DeepOrange : new Color(0.6f, 0.55f, 0.5f),
                new Vector2(0, 18), new Vector2(160, 60));

            int earned = LevelStars(idx);
            for (int s = 0; s < 3; s++)
            {
                var srt = Ui.Rect("S" + s, btn.transform, new Vector2(34, 34), new Vector2((s - 1) * 38f, -44));
                var simg = srt.gameObject.AddComponent<Image>();
                simg.sprite = SpriteFactory.Star;
                simg.color = s < earned ? StarGold : StarDim;
                simg.raycastTarget = false;
            }

            // claimed marker (token already collected for this level)
            var w3c = Web3Bridge.Instance;
            if (w3c != null && w3c.Connected && w3c.IsLevelClaimed(idx + 1))
            {
                Ui.Label("Claimed", btn.transform, "RST claimed", 16, WalletTeal,
                    new Vector2(0, -68), new Vector2(160, 24), style: FontStyle.Normal);
            }
        }

        // endless mode (wide enough that the subtitle stays inside)
        float endlessY = 110f - (rows - 1) * 205f - 235f;
        var endless = Ui.MakeButton("Endless", _menuScreen, "", new Vector2(600, 116), new Vector2(0, endlessY),
            ButtonRose, Color.white, 30, StartEndless);
        Ui.Label("EndlessLabel", endless.transform, "Endless Mode", 34, Color.white, new Vector2(0, 18), new Vector2(560, 44));
        Ui.Label("EndlessBest", endless.transform,
            "Best Score: " + PlayerPrefs.GetInt("endless_best", 0) + "   ·   keep your stars before time runs out!",
            20, new Color(1f, 0.93f, 0.85f), new Vector2(0, -24), new Vector2(560, 30), style: FontStyle.Normal);

        // power-ups remaining (bottom center)
        var itemsPanel = Ui.Rect("ItemsLeft", _menuScreen, new Vector2(620, 56), new Vector2(0, 48), new Vector2(0.5f, 0f));
        Ui.Panel(itemsPanel, new Color(1f, 1f, 1f, 0.75f));
        Ui.Label("ItemsLeftText", itemsPanel,
            "Power-ups left   —   Remove: " + _invRemove + "     Undo: " + _invUndo + "     Refresh: " + _invRefresh,
            22, SoftBrown, Vector2.zero, new Vector2(580, 40), style: FontStyle.Normal);

        // wallet corner (bottom right)
        BuildWalletCorner();

        // reset progress (handy while testing)
        Ui.MakeButton("Reset", _menuScreen, "Reset Progress", new Vector2(190, 46), new Vector2(125, 48),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 20, ResetProgress, new Vector2(0f, 0f));
    }

    void BuildWalletCorner()
    {
        var w3 = Web3Bridge.Instance;
        if (w3 == null) return;

        if (!w3.Connected)
        {
            if (w3.Simulated)
            {
                Ui.Label("SimNote", _menuScreen, "(simulated outside WebGL builds)", 15,
                    new Color(0.55f, 0.50f, 0.45f), new Vector2(-155, 92), new Vector2(260, 22),
                    style: FontStyle.Normal, anchor: new Vector2(1f, 0f));
            }
            Ui.MakeButton("Connect", _menuScreen, "Connect Wallet", new Vector2(240, 60), new Vector2(-155, 52),
                WalletTeal, Color.white, 24, () => w3.Connect(), new Vector2(1f, 0f));
        }
        else
        {
            var panel = Ui.Rect("Wallet", _menuScreen, new Vector2(300, 82), new Vector2(-180, 64), new Vector2(1f, 0f));
            Ui.Panel(panel, new Color(1f, 1f, 1f, 0.85f));
            Ui.Label("Addr", panel, w3.ShortAddress + (w3.Simulated ? "  (sim)" : ""), 20, Brown,
                new Vector2(0, 17), new Vector2(270, 26), style: FontStyle.Normal);
            Ui.Label("Bal", panel, w3.Balance + " RST", 26, WalletTeal, new Vector2(0, -15), new Vector2(270, 32));
        }
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

        // back button (icon) + level name, spaced so they never overlap
        var backBtn = Ui.MakeButton("Back", _gameScreen, "", new Vector2(64, 64), new Vector2(60, -58),
            Orange, Color.white, 34, ExitToMenuRequested, new Vector2(0f, 1f));
        var arrowRt = Ui.Rect("Arrow", backBtn.transform, new Vector2(36, 36), new Vector2(-2, 0));
        var arrowImg = arrowRt.gameObject.AddComponent<Image>();
        arrowImg.sprite = SpriteFactory.Arrow;
        arrowImg.color = Color.white;
        arrowImg.raycastTarget = false;

        _stageText = Ui.Label("Stage", _gameScreen, "Level 1", 34, Brown,
            new Vector2(320, -58), new Vector2(400, 50), TextAnchor.MiddleLeft, anchor: new Vector2(0f, 1f));

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

        // score (top right, kept away from the edge)
        _scoreText = Ui.Label("Score", _gameScreen, "Score  0", 32, Brown,
            new Vector2(-190, -58), new Vector2(300, 50), TextAnchor.MiddleRight, anchor: new Vector2(1f, 1f));

        // item buttons (right side)
        var itemPanel = Ui.Rect("Items", _gameScreen, new Vector2(160, 440), new Vector2(-105, 70), new Vector2(1f, 0.5f));
        Ui.Panel(itemPanel, new Color(0.98f, 0.80f, 0.50f, 0.85f));
        _removeBtn = BuildItemButton(itemPanel, "Remove", 140f, UseRemove, out _removeInv, out _removeUsed);
        _undoBtn = BuildItemButton(itemPanel, "Undo", 0f, UseUndo, out _undoInv, out _undoUsed);
        _refreshBtn = BuildItemButton(itemPanel, "Refresh", -140f, UseRefresh, out _refreshInv, out _refreshUsed);

        // board
        _boardRoot = Ui.Rect("Board", _gameScreen, new Vector2(10, 10), new Vector2(-40, 60));

        // side stacks (face-down piles just above the clearing zone)
        _leftStackRoot = Ui.Rect("LeftStack", _gameScreen, new Vector2(10, 10), new Vector2(-420, 225), new Vector2(0.5f, 0f));
        _rightStackRoot = Ui.Rect("RightStack", _gameScreen, new Vector2(10, 10), new Vector2(420, 225), new Vector2(0.5f, 0f));

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
        Ui.Label("Name", btn.transform, label, 22, Brown, new Vector2(0, 12), new Vector2(130, 30));

        // inventory badge, tucked in the corner clear of the label
        var badge = Ui.Rect("Badge", btn.transform, new Vector2(40, 40), new Vector2(56, 52));
        var bimg = badge.gameObject.AddComponent<Image>();
        bimg.sprite = SpriteFactory.Circle;
        bimg.color = DeepOrange;
        bimg.raycastTarget = false;
        invText = Ui.Label("Inv", badge, "0", 19, Color.white, Vector2.zero, new Vector2(40, 40));

        usedText = Ui.Label("Used", btn.transform, "0/" + GameConfig.ItemUseCapPerStage, 20,
            SoftBrown, new Vector2(0, -30), new Vector2(130, 28), style: FontStyle.Normal);
        return btn;
    }

    // =====================================================================
    // session control
    // =====================================================================

    void StartLevel(int index)
    {
        _levelIndex = index;
        _endlessRound = 0;
        BeginSession(GameConfig.S.GetLevel(index));
    }

    void StartEndless()
    {
        _levelIndex = -1;
        _endlessRound = 1;
        BeginSession(GameConfig.S.GetEndlessRound(_rng));
    }

    void BeginSession(GameSettings.LevelDef def)
    {
        ClosePopup();
        if (_menuScreen != null) Destroy(_menuScreen.gameObject);
        _gameScreen.gameObject.SetActive(true);

        ClearTable();
        _elapsed = 0f;
        _score = 0;
        _combo = 0;
        _lastMatchTime = -999f;
        _endlessFinalStars = 0;
        _usedRemove = _usedUndo = _usedRefresh = 0;

        DealBoard(def);
        _state = State.Playing;
        UpdateHud();
    }

    /// <summary>
    /// Deals board + side stacks from one shared type bag so that every
    /// kind's total count (board + both stacks) is a multiple of 3.
    /// </summary>
    void DealBoard(GameSettings.LevelDef def)
    {
        _currentDef = def;
        _leftStack.Clear();
        _rightStack.Clear();

        int side = Mathf.Max(0, def.sideStackCards);
        int boardTiles = Mathf.Max(3, (def.tiles + 2) / 3 * 3);
        int total = boardTiles + side * 2;
        while (total % 3 != 0) { boardTiles++; total++; }
        int types = Mathf.Clamp(def.cardVarieties, 1, GameConfig.S.cardKinds.Length);

        var bag = new List<int>();
        for (int i = 0; i < total / 3; i++)
        {
            int t = _rng.Next(types);
            bag.Add(t); bag.Add(t); bag.Add(t);
        }
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            int tmp = bag[i]; bag[i] = bag[j]; bag[j] = tmp;
        }

        int idx = 0;
        for (int i = 0; i < side; i++) AddStackCard(_leftStack, _leftStackRoot, 0, bag[idx++]);
        for (int i = 0; i < side; i++) AddStackCard(_rightStack, _rightStackRoot, 1, bag[idx++]);
        _leftStack.Refresh(false);
        _rightStack.Refresh(false);

        _board.Generate(_boardRoot, def, _rng, OnCardClicked, bag.GetRange(idx, bag.Count - idx));
    }

    void AddStackCard(SideStack stack, RectTransform root, int sideId, int type)
    {
        var card = Card.Create(root, type);
        card.stackSide = sideId;
        card.onClicked = OnCardClicked;
        stack.AddInitial(card);
    }

    void ClearTable()
    {
        _board.Clear();
        _tray.Clear();
        _leftStack.Clear();
        _rightStack.Clear();
        foreach (var c in _held)
            if (c != null) Destroy(c.gameObject);
        _held.Clear();
        _undoStack.Clear();
    }

    void ExitToMenuRequested()
    {
        if (_state == State.Playing) ShowQuitConfirmPopup();
        else ExitToMenu();
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
        if (_state != State.Playing || _popupLayer != null || card.removed || card.inTray) return;

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

        // side stack card: only the face-up front card can be played
        if (card.stackSide >= 0)
        {
            var stack = card.stackSide == 0 ? _leftStack : _rightStack;
            if (stack.Front != card) return;
            stack.Take(card);
            _undoStack.Add(card);
            PlaceInTray(card);
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
        card.SetFaceDown(false);
        card.button.interactable = false;
        card.transform.SetParent(_trayRoot, true);
        card.transform.SetAsLastSibling();
        card.transform.localScale = Vector3.one;

        bool survived = _tray.Add(card);
        UpdateHud();

        if (!survived)
        {
            Overflowed();
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
        if (_board.Count > 0 || _leftStack.Count > 0 || _rightStack.Count > 0 ||
            _held.Count > 0 || _tray.cards.Count > 0) return;

        if (_levelIndex >= 0) WinLevel();
        else AdvanceEndless();
    }

    void WinLevel()
    {
        _state = State.Finished;

        var def = _currentDef;
        int stars = _elapsed <= def.threeStarTime ? 3 : _elapsed <= def.twoStarTime ? 2 : 1;
        int bonus = Mathf.Max(0, Mathf.RoundToInt(def.twoStarTime - _elapsed)) * GameConfig.TimeBonusPerSecond;
        _score += bonus;

        string key = "stars_" + (_levelIndex + 1);
        if (stars > PlayerPrefs.GetInt(key, 0)) PlayerPrefs.SetInt(key, stars);

        // small item reward for clearing a level
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
        DealBoard(GameConfig.S.GetEndlessRound(_rng));
        UpdateHud();
    }

    /// <summary>Stars still lit in Endless mode at the current elapsed time.</summary>
    int EndlessStarsRemaining()
    {
        var s = GameConfig.S;
        if (_elapsed < s.endlessStar3Time) return 3;
        if (_elapsed < s.endlessStar2Time) return 2;
        return 1;
    }

    void Overflowed()
    {
        if (_levelIndex < 0)
        {
            EndEndlessRun(false);
        }
        else
        {
            _state = State.Finished;
            ShowLosePopup();
        }
    }

    void EndEndlessRun(bool timeUp)
    {
        _state = State.Finished;
        _endlessFinalStars = EndlessStarsRemaining();

        int best = PlayerPrefs.GetInt("endless_best", 0);
        if (_score > best)
        {
            best = _score;
            PlayerPrefs.SetInt("endless_best", best);
            PlayerPrefs.Save();
        }

        ShowEndlessOverPopup(timeUp, best);
    }

    // =====================================================================
    // items
    // =====================================================================

    bool CanUseRemove => _state == State.Playing && _invRemove > 0 &&
        _usedRemove < GameConfig.ItemUseCapPerStage &&
        _held.Count < GameConfig.HoldSize && _tray.cards.Count > 0;

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

        // fill whatever space is free in the holding area (up to 3 cards)
        int space = GameConfig.HoldSize - _held.Count;
        var taken = _tray.TakeFromFront(space);
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
        if (target.stackSide == 0) _leftStack.PushFront(target);
        else if (target.stackSide == 1) _rightStack.PushFront(target);
        else _board.Return(target);

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
    // HUD
    // =====================================================================

    void Update()
    {
        // timer pauses while any popup (quit confirmation etc.) is open
        if (_state == State.Playing && _popupLayer == null)
        {
            _elapsed += Time.deltaTime;
            if (_levelIndex < 0 && GameConfig.S.endlessDuration > 0f && _elapsed >= GameConfig.S.endlessDuration)
            {
                EndEndlessRun(true);
                return;
            }
            UpdateHud();
        }
        if (_toastText != null && _toastText.enabled && Time.unscaledTime > _toastUntil)
            _toastText.enabled = false;
    }

    void UpdateHud()
    {
        _scoreText.text = "Score  " + _score;

        if (_levelIndex >= 0)
        {
            _stageText.text = "Level " + (_levelIndex + 1);
            _timerText.text = FormatTime(_elapsed);
            int would = _elapsed <= _currentDef.threeStarTime ? 3 : _elapsed <= _currentDef.twoStarTime ? 2 : 1;
            for (int s = 0; s < 3; s++)
            {
                _hudStars[s].gameObject.SetActive(true);
                _hudStars[s].color = s < would ? StarGold : StarDim;
            }
        }
        else
        {
            _stageText.text = "Endless  ·  Round " + _endlessRound;
            float remain = Mathf.Max(0f, GameConfig.S.endlessDuration - _elapsed);
            _timerText.text = FormatTime(remain);
            int stars = EndlessStarsRemaining();
            for (int s = 0; s < 3; s++)
            {
                _hudStars[s].gameObject.SetActive(true);
                _hudStars[s].color = s < stars ? StarGold : StarDim;
            }
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
        if (_toastText == null) return;
        _toastText.text = message;
        _toastText.enabled = true;
        _toastUntil = Time.unscaledTime + 1.8f;
    }

    static string FormatTime(float t)
    {
        int total = Mathf.FloorToInt(t);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    // =====================================================================
    // web3 events
    // =====================================================================

    void OnWeb3StateChanged()
    {
        if (_state == State.Menu) ShowMenu();
    }

    void OnWeb3ClaimOk(string payload)
    {
        if (_claimStatusText != null)
            _claimStatusText.text = "Claimed! Tokens minted to your wallet.";
        Toast("Star tokens claimed!");
    }

    void OnWeb3ClaimError(string message)
    {
        if (_claimStatusText != null)
            _claimStatusText.text = "Claim failed: " + message;
        if (_claimButton != null)
            _claimButton.interactable = true;
    }

    /// <summary>
    /// Claim row inside result popups. levelOneBased = -1 for Endless.
    /// Levels can only ever be claimed once (enforced by the contract);
    /// Endless pays out the stars remaining at the end of each run.
    /// </summary>
    void AddClaimSection(RectTransform panel, float labelY, float buttonY, int levelOneBased, int stars)
    {
        var w3 = Web3Bridge.Instance;
        if (w3 == null) return;

        if (!w3.Connected)
        {
            Ui.Label("ClaimHint", panel, "Connect your wallet to claim star tokens (1 star = 1 RST).", 22,
                SoftBrown, new Vector2(0, labelY), new Vector2(600, 30), style: FontStyle.Normal);
            Ui.MakeButton("ConnectPopup", panel, "Connect Wallet", new Vector2(240, 56), new Vector2(0, buttonY),
                WalletTeal, Color.white, 24, () => w3.Connect());
            return;
        }

        if (levelOneBased > 0 && w3.IsLevelClaimed(levelOneBased))
        {
            Ui.Label("ClaimDone", panel, "Reward already claimed — finished levels give no more tokens.", 22,
                WalletTeal, new Vector2(0, labelY), new Vector2(600, 30), style: FontStyle.Normal);
            return;
        }

        if (stars < 1) return;

        _claimStatusText = Ui.Label("ClaimStatus", panel, "1 star = 1 RST on BSC Testnet", 20,
            SoftBrown, new Vector2(0, labelY), new Vector2(600, 30), style: FontStyle.Normal);

        int lv = levelOneBased;
        int st = stars;
        _claimButton = Ui.MakeButton("Claim", panel, "Claim " + stars + " RST", new Vector2(240, 56),
            new Vector2(0, buttonY), WalletTeal, Color.white, 24, () =>
            {
                if (_claimButton != null) _claimButton.interactable = false;
                if (_claimStatusText != null) _claimStatusText.text = "Confirm the transaction in MetaMask...";
                if (lv > 0) Web3Bridge.Instance.ClaimLevel(lv, st);
                else Web3Bridge.Instance.ClaimEndless(st);
            });
    }

    // =====================================================================
    // popups
    // =====================================================================

    RectTransform BuildPopup(string title, float height, float width = 700f)
    {
        ClosePopup();
        _popupLayer = Ui.Stretch("PopupLayer", _canvasRoot);
        var dim = _popupLayer.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f); // also blocks clicks behind it

        var panel = Ui.Rect("Panel", _popupLayer, new Vector2(width, height), Vector2.zero);
        Ui.Panel(panel, Cream);
        Ui.Label("Title", panel, title, 46, DeepOrange,
            new Vector2(0, height * 0.5f - 60f), new Vector2(width - 80f, 60));
        return panel;
    }

    void ClosePopup()
    {
        if (_popupLayer != null) Destroy(_popupLayer.gameObject);
        _popupLayer = null;
        _claimButton = null;
        _claimStatusText = null;
    }

    void ShowQuitConfirmPopup()
    {
        var panel = BuildPopup("Quit Level?", 360);
        Ui.Label("Msg", panel,
            "Are you sure you want to quit?\nProgress in this run will be lost.", 26, Brown,
            new Vector2(0, 25), new Vector2(560, 80), style: FontStyle.Normal);

        Ui.MakeButton("Cancel", panel, "Keep Playing", new Vector2(210, 64), new Vector2(-120, -105),
            Orange, Color.white, 24, ClosePopup);
        Ui.MakeButton("Quit", panel, "Quit", new Vector2(210, 64), new Vector2(120, -105),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 24, ExitToMenu);
    }

    void ShowWinPopup(int stars, int timeBonus)
    {
        var panel = BuildPopup("Level Clear!", 640);

        for (int s = 0; s < 3; s++)
        {
            var srt = Ui.Rect("Star" + s, panel, new Vector2(86, 86), new Vector2((s - 1) * 100f, 160f));
            var img = srt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.Star;
            img.color = s < stars ? StarGold : StarDim;
            img.raycastTarget = false;
            if (s < stars) Tween.ScaleIn(srt, 0.25f + s * 0.22f, 0.5f);
        }

        Ui.Label("Time", panel, "Time  " + FormatTime(_elapsed), 28, Brown,
            new Vector2(0, 80), new Vector2(600, 36), style: FontStyle.Normal);
        Ui.Label("Score", panel, "Score  " + _score + "   (time bonus +" + timeBonus + ")", 28, Brown,
            new Vector2(0, 40), new Vector2(600, 36), style: FontStyle.Normal);
        Ui.Label("Reward", panel, "Items: +1 Remove, +1 Undo, +1 Refresh", 22,
            SoftBrown, new Vector2(0, 2), new Vector2(600, 30), style: FontStyle.Normal);

        AddClaimSection(panel, -50f, -110f, _levelIndex + 1, stars);

        int replayIndex = _levelIndex;
        Ui.MakeButton("Menu", panel, "Menu", new Vector2(170, 66), new Vector2(-210, -250),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Replay", panel, "Replay", new Vector2(170, 66), new Vector2(0, -250),
            Orange, Color.white, 26, () => StartLevel(replayIndex));
        if (_levelIndex < GameConfig.S.LevelCount - 1)
        {
            Ui.MakeButton("Next", panel, "Next", new Vector2(170, 66), new Vector2(210, -250),
                ButtonRose, Color.white, 26, () => StartLevel(replayIndex + 1));
        }

        // celebration!
        Confetti.Burst(_popupLayer, 70);
    }

    void ShowLosePopup()
    {
        var panel = BuildPopup("Defeat...", 400);
        Ui.Label("Msg", panel, "The clearing zone overflowed!\nRaggler keeps the gifts... for now.", 28, Brown,
            new Vector2(0, 30), new Vector2(600, 90), style: FontStyle.Normal);

        int replayIndex = _levelIndex;
        Ui.MakeButton("Menu", panel, "Menu", new Vector2(190, 66), new Vector2(-110, -120),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Retry", panel, "Retry", new Vector2(190, 66), new Vector2(110, -120),
            ButtonRose, Color.white, 26, () => StartLevel(replayIndex));
    }

    void ShowEndlessOverPopup(bool timeUp, int best)
    {
        var panel = BuildPopup(timeUp ? "Time's Up!" : "Endless Over", 620);

        Ui.Label("Rounds", panel, "Rounds cleared: " + (_endlessRound - 1), 28, Brown,
            new Vector2(0, 170), new Vector2(600, 36), style: FontStyle.Normal);
        Ui.Label("Score", panel, "Score  " + _score, 34, DeepOrange,
            new Vector2(0, 125), new Vector2(600, 44));
        Ui.Label("Best", panel, "Best  " + best, 24, Brown,
            new Vector2(0, 82), new Vector2(600, 32), style: FontStyle.Normal);

        Ui.Label("StarsKept", panel, "Stars kept before the timer ran out:", 24, Brown,
            new Vector2(0, 35), new Vector2(600, 32), style: FontStyle.Normal);
        for (int s = 0; s < 3; s++)
        {
            var srt = Ui.Rect("KStar" + s, panel, new Vector2(56, 56), new Vector2((s - 1) * 66f, -15f));
            var img = srt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.Star;
            img.color = s < _endlessFinalStars ? StarGold : StarDim;
            img.raycastTarget = false;
            if (s < _endlessFinalStars) Tween.ScaleIn(srt, 0.25f + s * 0.22f, 0.5f);
        }

        AddClaimSection(panel, -70f, -130f, -1, _endlessFinalStars);

        Ui.MakeButton("Menu", panel, "Menu", new Vector2(190, 66), new Vector2(-110, -240),
            new Color(0.8f, 0.72f, 0.62f), Color.white, 26, ExitToMenu);
        Ui.MakeButton("Retry", panel, "Retry", new Vector2(190, 66), new Vector2(110, -240),
            ButtonRose, Color.white, 26, StartEndless);

        if (_endlessFinalStars >= 2)
            Confetti.Burst(_popupLayer, 45);
    }

    void ShowHelpPopup()
    {
        var panel = BuildPopup("Notice", 640, 800);
        string rules =
            "Raggler made off with everyone's gifts!\nDefeat him and take back the gifts!\n\n" +
            "1. Tap the cards to place them in the clearing zone below.\n" +
            "2. Match three identical cards to clear them automatically. Clear all cards on the screen to win.\n" +
            "3. The clearing zone can hold up to 7 cards. Go over, and you lose!\n" +
            "4. You may use items: Remove Card, Undo Card, and Refresh Card.\n" +
            "5. Some boards have face-down piles beside the clearing zone — only the front card can be played.\n" +
            "6. The rating for each level is based on how fast you complete it!\n\n" +
            "Tokens: connect MetaMask to convert stars into RST (1 star = 1 token). Each level pays out once. " +
            "In Endless, you keep the stars still lit when the timer runs out.";
        Ui.Label("Rules", panel, rules, 23, Brown, new Vector2(0, -10), new Vector2(700, 440),
            TextAnchor.UpperLeft, FontStyle.Normal, null, true);
        Ui.MakeButton("Ok", panel, "Got it!", new Vector2(190, 66), new Vector2(0, -260),
            Orange, Color.white, 26, ClosePopup);
    }

    // =====================================================================
    // persistence
    // =====================================================================

    static int LevelStars(int index)
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
