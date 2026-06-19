using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates card layouts for each stage and tracks remaining board cards.
/// Card counts must always be multiples of 3 so every card has a valid match.
/// </summary>
public class BoardGenerator : MonoBehaviour
{
    public static BoardGenerator Instance { get; private set; }

    public GameObject cardPrefab;
    public Sprite[] cardSprites;          // One sprite per card type; index == cardTypeId

    private List<Card> boardCards = new List<Card>();

    // ── Stage layout definitions ──────────────────────────────────────────────
    // Each StageLayout lists CardData entries: (typeId, gridX, gridY, layer).
    // layer 0 = bottom; higher layers are placed on top and block lower ones.

    [System.Serializable]
    public struct CardData
    {
        public int typeId;
        public float x, y;
        public int layer;
    }

    void Awake() => Instance = this;

    public void BuildStage(int stageIndex)
    {
        ClearBoard();
        List<CardData> layout = GenerateLayout(stageIndex);
        SpawnCards(layout);
        AssignCoveringRelationships();
    }

    // ── Layout generator ─────────────────────────────────────────────────────

    List<CardData> GenerateLayout(int stageIndex)
    {
        // Number of card types scales with stage difficulty
        int numTypes = Mathf.Clamp(3 + stageIndex, 3, cardSprites.Length);
        // Total cards: must be multiple of 3; increases with stage
        int totalCards = (6 + stageIndex * 3);
        totalCards = Mathf.CeilToInt(totalCards / 3f) * 3;
        totalCards = Mathf.Min(totalCards, 60);

        // Build a balanced deck (each type appears in multiples of 3)
        List<int> deck = new List<int>();
        int remaining = totalCards;
        List<int> typePool = Enumerable.Range(0, numTypes).ToList();

        while (remaining > 0)
        {
            int t = typePool[Random.Range(0, typePool.Count)];
            deck.Add(t); deck.Add(t); deck.Add(t);
            remaining -= 3;
        }
        deck = deck.Take(totalCards).ToList();
        Shuffle(deck);

        // Assign positions in a grid-like pattern with some stacking
        var result = new List<CardData>();
        float spacing = 1.2f;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(totalCards));

        // Predefined stage-flavoured shapes
        var positions = BuildShapePositions(stageIndex, totalCards, spacing);

        for (int i = 0; i < totalCards; i++)
        {
            var pos = positions[i % positions.Count];
            // Higher stageIndex → more stacking layers
            int maxLayer = 1 + stageIndex / 2;
            int layer = (i < positions.Count) ? 0 : Random.Range(1, maxLayer + 1);

            result.Add(new CardData
            {
                typeId = deck[i],
                x = pos.x + (layer == 0 ? 0 : Random.Range(-0.2f, 0.2f)),
                y = pos.y + (layer == 0 ? 0 : Random.Range(-0.2f, 0.2f)),
                layer = layer
            });
        }

        return result;
    }

    List<Vector2> BuildShapePositions(int stageIndex, int count, float spacing)
    {
        var positions = new List<Vector2>();
        int shape = stageIndex % 5;

        switch (shape)
        {
            case 0: // Simple grid
                int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                for (int i = 0; i < count; i++)
                    positions.Add(new Vector2((i % cols) * spacing - cols * spacing * 0.5f,
                                              (i / cols) * spacing));
                break;

            case 1: // Diamond
                for (int r = -3; r <= 3 && positions.Count < count; r++)
                    for (int c = -(3 - Mathf.Abs(r)); c <= (3 - Mathf.Abs(r)) && positions.Count < count; c++)
                        positions.Add(new Vector2(c * spacing, r * spacing));
                break;

            case 2: // Cross
                for (int i = -3; i <= 3; i++)
                {
                    positions.Add(new Vector2(i * spacing, 0));
                    if (i != 0) positions.Add(new Vector2(0, i * spacing));
                }
                break;

            case 3: // Pyramid rows
                int row = 1;
                while (positions.Count < count)
                {
                    for (int c = 0; c < row && positions.Count < count; c++)
                        positions.Add(new Vector2((c - row * 0.5f + 0.5f) * spacing, -row * spacing * 0.6f));
                    row++;
                }
                break;

            default: // Scattered clusters
                for (int i = 0; i < count; i++)
                    positions.Add(new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f)));
                break;
        }

        while (positions.Count < count)
            positions.Add(new Vector2(Random.Range(-4f, 4f), Random.Range(-3f, 3f)));

        return positions;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    void SpawnCards(List<CardData> layout)
    {
        foreach (var data in layout)
        {
            var go = Instantiate(cardPrefab);
            var card = go.GetComponent<Card>();
            card.cardTypeId = data.typeId;
            card.layer = data.layer;
            card.faceSprite = cardSprites[data.typeId % cardSprites.Length];

            go.transform.position = new Vector3(data.x, data.y, -data.layer * 0.01f);
            go.GetComponent<SpriteRenderer>().sortingOrder = data.layer;

            boardCards.Add(card);
        }
    }

    // ── Blocking relationships ────────────────────────────────────────────────

    void AssignCoveringRelationships()
    {
        float overlapThreshold = 0.6f;

        foreach (var card in boardCards)
        {
            var covers = boardCards
                .Where(other => other != card
                    && other.layer == card.layer + 1
                    && Mathf.Abs(other.transform.position.x - card.transform.position.x) < overlapThreshold
                    && Mathf.Abs(other.transform.position.y - card.transform.position.y) < overlapThreshold)
                .ToList();
            card.SetCoveringCards(covers);
        }
    }

    public void RefreshAllCardAccessibility()
    {
        foreach (var c in boardCards)
            if (c != null) c.ForceRefresh();
    }

    public bool AllCardsCleared()
    {
        return boardCards.All(c => c == null || c.isRemoved || c.isInClearingZone);
    }

    public void MarkCardRemoved(Card card)
    {
        // Notify any card that was blocked by this one
        foreach (var c in boardCards)
            if (c != null && !c.isRemoved)
                c.NotifyCoverRemoved(card);
    }

    /// <summary>Refresh power-up: randomly relocate all accessible board cards.</summary>
    public void ShuffleBoardPositions()
    {
        var accessible = boardCards.FindAll(c => c != null && !c.isRemoved && !c.isInClearingZone);
        var positions = accessible.ConvertAll(c => c.transform.position);
        Shuffle(positions);
        for (int i = 0; i < accessible.Count; i++)
            accessible[i].transform.position = positions[i];
        AssignCoveringRelationships();
    }

    void ClearBoard()
    {
        foreach (var c in boardCards)
            if (c != null) Destroy(c.gameObject);
        boardCards.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
