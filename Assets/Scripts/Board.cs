using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the layered card layout: generation, coverage (blocked) computation,
/// returning cards (undo) and type reshuffling (refresh).
/// Cards sit on a half-step grid; two cards overlap when both their column
/// and row differ by less than 2 half-steps.
/// </summary>
public class Board
{
    public readonly List<Card> cards = new List<Card>();

    readonly List<RectTransform> _layerRoots = new List<RectTransform>();

    public int Count => cards.Count;

    public void Generate(RectTransform boardRoot, GameSettings.LevelDef level, System.Random rng, Action<Card> onClick)
    {
        Clear();

        // sanitize designer input: multiples of 3, valid ranges
        int tiles = Mathf.Max(3, (level.tiles + 2) / 3 * 3);
        int types = Mathf.Clamp(level.cardVarieties, 1, GameConfig.S.cardKinds.Length);
        int layers = Mathf.Clamp(level.layers, 1, 6);

        // one container per layer => sibling order gives correct draw order
        for (int l = 0; l < layers; l++)
            _layerRoots.Add(Ui.Stretch("Layer" + l, boardRoot));

        int[] layerCounts = SplitAcrossLayers(tiles, layers);

        // type bag: triples of random kinds from the allowed pool
        var bag = new List<int>();
        for (int i = 0; i < tiles / 3; i++)
        {
            int t = rng.Next(types);
            bag.Add(t); bag.Add(t); bag.Add(t);
        }
        Shuffle(bag, rng);

        int bagIndex = 0;
        for (int l = 0; l < layers; l++)
        {
            foreach (var pos in PickPositions(layerCounts[l], l, layers, rng))
            {
                var card = Card.Create(_layerRoots[l], bag[bagIndex++]);
                card.layer = l;
                card.col = pos.x;
                card.row = pos.y;
                card.Rect.anchoredPosition = card.BoardPosition;
                card.onClicked = onClick;
                cards.Add(card);
            }
        }

        RecomputeBlocked();
    }

    /// <summary>
    /// Positions for one layer: start from a sparse lattice (guaranteed
    /// non-overlapping within the layer), then jitter each pick by one
    /// half-step when it stays conflict-free. Higher layers use a smaller
    /// footprint so the pile narrows toward the top, like the original.
    /// </summary>
    static List<Vector2Int> PickPositions(int count, int layer, int totalLayers, System.Random rng)
    {
        int maxCol = Mathf.Max(2, 6 - layer);
        int maxRow = Mathf.Max(2, 4 - layer / 2);

        List<Vector2Int> lattice;
        while (true)
        {
            lattice = new List<Vector2Int>();
            for (int c = -maxCol; c <= maxCol; c += 2)
                for (int r = -maxRow; r <= maxRow; r += 2)
                    lattice.Add(new Vector2Int(c, r));
            if (lattice.Count >= count || (maxCol >= 6 && maxRow >= 4)) break;
            if (maxCol <= maxRow * 2) maxCol++; else maxRow++;
        }

        Shuffle(lattice, rng);
        var chosen = new List<Vector2Int>();
        for (int i = 0; i < count && i < lattice.Count; i++)
            chosen.Add(lattice[i]);

        // organic jitter: nudge by one half-step where it causes no overlap
        for (int i = 0; i < chosen.Count; i++)
        {
            int jc = rng.Next(-1, 2);
            int jr = rng.Next(-1, 2);
            if (jc == 0 && jr == 0) continue;
            var candidate = new Vector2Int(chosen[i].x + jc, chosen[i].y + jr);
            bool conflict = false;
            for (int k = 0; k < chosen.Count; k++)
            {
                if (k == i) continue;
                if (Mathf.Abs(chosen[k].x - candidate.x) < 2 &&
                    Mathf.Abs(chosen[k].y - candidate.y) < 2)
                {
                    conflict = true;
                    break;
                }
            }
            if (!conflict) chosen[i] = candidate;
        }

        return chosen;
    }

    static int[] SplitAcrossLayers(int total, int layers)
    {
        // heavier at the bottom, lighter at the top
        var weights = new float[layers];
        float sum = 0f;
        for (int l = 0; l < layers; l++)
        {
            weights[l] = layers - l * 0.6f;
            sum += weights[l];
        }
        var counts = new int[layers];
        int assigned = 0;
        for (int l = 0; l < layers; l++)
        {
            counts[l] = Mathf.FloorToInt(total * weights[l] / sum);
            assigned += counts[l];
        }
        for (int l = 0; assigned < total; l = (l + 1) % layers)
        {
            counts[l]++;
            assigned++;
        }
        return counts;
    }

    /// <summary>A card is blocked while any card on a higher layer overlaps it.</summary>
    public void RecomputeBlocked()
    {
        foreach (var a in cards)
        {
            bool blocked = false;
            foreach (var b in cards)
            {
                if (b.layer > a.layer &&
                    Mathf.Abs(b.col - a.col) < 2 &&
                    Mathf.Abs(b.row - a.row) < 2)
                {
                    blocked = true;
                    break;
                }
            }
            a.SetBlocked(blocked);
        }
    }

    public void Take(Card card)
    {
        cards.Remove(card);
    }

    /// <summary>Puts a card back at its original spot (Undo item).</summary>
    public void Return(Card card)
    {
        card.inTray = false;
        card.inHold = false;
        card.transform.SetParent(_layerRoots[card.layer], true);
        cards.Add(card);
        Tween.MoveTo(card.Rect, card.BoardPosition, 0.2f);
        RecomputeBlocked();
    }

    /// <summary>Reshuffles the kinds of all remaining board cards (Refresh item).</summary>
    public void ShuffleTypes(System.Random rng)
    {
        var types = new List<int>();
        foreach (var c in cards) types.Add(c.typeIndex);
        Shuffle(types, rng);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].typeIndex = types[i];
            cards[i].RefreshVisual();
        }
    }

    public void Clear()
    {
        foreach (var c in cards)
            if (c != null) UnityEngine.Object.Destroy(c.gameObject);
        cards.Clear();
        foreach (var lr in _layerRoots)
            if (lr != null) UnityEngine.Object.Destroy(lr.gameObject);
        _layerRoots.Clear();
    }

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
