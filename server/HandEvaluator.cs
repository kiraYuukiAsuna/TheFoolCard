using System.Collections.Generic;
using System.Linq;

namespace FoolCardServer
{

public enum HandCategory { Single=0, Pair=1, TwoPair=2, Trips=3, Straight=4, Flush=5, FullHouse=6, Quads=7, StraightFlush=8 }

public static class HandCategoryExt {
    public static int Points(this HandCategory cat) => cat switch {
        HandCategory.Pair => 15, HandCategory.TwoPair => 30, HandCategory.Trips => 30,
        HandCategory.Straight => 60, HandCategory.Flush => 60, HandCategory.FullHouse => 80,
        HandCategory.Quads => 80, HandCategory.StraightFlush => 120, _ => 0
    };
    public static string Name(this HandCategory cat) => cat switch {
        HandCategory.StraightFlush => "同色序列", HandCategory.Quads => "四骑士", HandCategory.FullHouse => "满座",
        HandCategory.Flush => "同色", HandCategory.Straight => "序列", HandCategory.Trips => "三贤者",
        HandCategory.TwoPair => "双偶星", HandCategory.Pair => "偶星", _ => "无牌型"
    };
}

public record HandResult(HandCategory Category, int SumPoints, int SpecialPoints) {
    public int CategoryPoints => Category.Points();
    public int Total => CategoryPoints + SumPoints + SpecialPoints;
    public string CategoryName => Category.Name();
}

public static class HandEvaluator {
    private static readonly int[] RankCycle = new int[] {14, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13};

    public static HandResult Evaluate(List<int> placedCards, List<int> publicCards, AreaEffectType effect)
    {
        int sumPts = placedCards.Sum(Card.PointValue);
        int specialPts = 0;

        foreach (var c in placedCards)
        {
            int r = Card.Rank(c); // 0=2, ..., 12=A
            if (effect == AreaEffectType.BonusForA2358)
            {
                if (r == 12 || r == 0 || r == 1 || r == 3 || r == 6) specialPts += 15;
            }
            else if (effect == AreaEffectType.BonusForWands)
            {
                if (Card.Suit(c) == 2) specialPts += 15; // 0=Spade, 1=Heart, 2=Club(Wand), 3=Diamond
            }
            else if (effect == AreaEffectType.BonusForOdd)
            {
                // Odd card faces: 3, 5, 7, 9, J, K, A. (indices: 1, 3, 5, 7, 9, 11, 12)
                if (r == 1 || r == 3 || r == 5 || r == 7 || r == 9 || r == 11 || r == 12)
                    specialPts += 15;
            }
        }

        var allCards = new List<int>(placedCards);
        allCards.AddRange(publicCards);
        HandCategory cat = BestCategory(allCards);
        return new HandResult(cat, sumPts, specialPts);
    }

    private static HandCategory BestCategory(List<int> cards)
    {
        if (cards.Count == 0) return HandCategory.Single;

        var rankCounts = new Dictionary<int, int>();
        var suitCounts = new Dictionary<int, int>();
        foreach (var c in cards) {
            int rv = Card.RankValue(c);
            int s = Card.Suit(c);
            rankCounts[rv] = rankCounts.GetValueOrDefault(rv) + 1;
            suitCounts[s]  = suitCounts.GetValueOrDefault(s)  + 1;
        }

        bool hasFlush = suitCounts.Values.Any(v => v >= 5);
        bool hasStraight = CheckCircularStraight(rankCounts.Keys.ToList());

        if (hasStraight && hasFlush && CheckStraightFlush(cards, rankCounts, suitCounts))
            return HandCategory.StraightFlush;
        if (rankCounts.Values.Any(v => v >= 4)) return HandCategory.Quads;
        bool hasTrips = rankCounts.Values.Any(v => v >= 3);
        int pairCount = rankCounts.Count(kv => kv.Value >= 2);
        if (hasTrips && pairCount >= 2) return HandCategory.FullHouse;
        if (hasFlush) return HandCategory.Flush;
        if (hasStraight) return HandCategory.Straight;
        if (hasTrips) return HandCategory.Trips;
        if (pairCount >= 2) return HandCategory.TwoPair;
        if (pairCount >= 1) return HandCategory.Pair;
        return HandCategory.Single;
    }

    private static bool CheckCircularStraight(List<int> distinctRanks) {
        var set = new HashSet<int>(distinctRanks);
        if (set.Count < 5) return false;
        for (int start = 0; start < 13; start++) {
            bool ok = true;
            for (int i = 0; i < 5; i++) {
                if (!set.Contains(RankCycle[(start + i) % 13])) { ok = false; break; }
            }
            if (ok) return true;
        }
        return false;
    }

    private static bool CheckStraightFlush(List<int> cards, Dictionary<int, int> rankC, Dictionary<int, int> suitC) {
        if (cards.Count < 5) return false;
        if (cards.Count == 5) {
            int flushSuit = suitC.First(kv => kv.Value >= 5).Key;
            var sameSuit = cards.Where(c => Card.Suit(c) == flushSuit).ToList();
            return CheckCircularStraight(sameSuit.Select(Card.RankValue).Distinct().ToList());
        }
        var flushSubsets = suitC.Where(kv => kv.Value >= 5)
                                .SelectMany(kv => {
                                    var same = cards.Where(c => Card.Suit(c) == kv.Key).ToList();
                                    if (same.Count == 5) return new[] { same };
                                    return same.Select((_, skip) => same.Where((_, i) => i != skip).ToList());
                                }).ToList();
        var straightSubsets = cards.Select((_, skip) => cards.Where((_, i) => i != skip).ToList())
                                   .Where(sub => CheckCircularStraight(sub.Select(Card.RankValue).Distinct().ToList()))
                                   .ToList();
        return flushSubsets.Any(fs => straightSubsets.Any(ss => ss.Count(c => fs.Contains(c)) >= 4));
    }
}

}
