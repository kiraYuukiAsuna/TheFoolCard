using System.Collections.Generic;
using System.Linq;

namespace FoolCardServer
{

public enum GamePhase { WAITING, IN_GAME, GAME_END }
public enum AreaEffectType { None, BonusForA2358, BonusForWands, BonusForOdd }

public static class Card
{
    private static readonly int[] PointValues = new int[] {2,3,4,5,6,7,8,9,10,10,10,10,11};
    public static int Rank(int id)       => id / 4;
    public static int Suit(int id)       => id % 4;
    public static int PointValue(int id) => PointValues[Rank(id)];
    public static int RankValue(int id)  => Rank(id) + 2; // 2-14
}

public class AreaPlayerSlot
{
    public List<int> LockedCards  = new();
    public List<int> PendingCards = new();
}

public class AreaState
{
    public int          AreaIndex;
    public AreaEffectType EffectType;
    public string       EffectText = "";
    public List<int>    CommunityCards = new();
    public bool         CommunityRevealed;
    public AreaPlayerSlot[] Slots = new AreaPlayerSlot[] {new(), new()};
}

public class PlayerState
{
    public int        PlayerId;
    public string     Name          = "";
    public bool       IsReady;
    public List<int>  Hand          = new();
    public int        DiscardCount;
    public bool       HasConfirmed;
    public int        PendingCount { get; set; }
}

public class GameState
{
    public string     RoomId        = "";
    public GamePhase  Phase         = GamePhase.WAITING;
    public int        StateVersion;
    public int        CurrentRound;
    public long       Seed;

    public PlayerState[] Players = new PlayerState[] {new() { PlayerId = 0 }, new() { PlayerId = 1 }};
    public AreaState[]   Areas   = new AreaState[] {new() { AreaIndex = 0 }, new() { AreaIndex = 1 }, new() { AreaIndex = 2 }};
    public List<int> DrawPile = new();

    public int PlayerPendingCount(int playerId) => Areas.Sum(a => a.Slots[playerId].PendingCards.Count);
    public int PlayerTotalPlacedInArea(int playerId, int areaIndex) =>
        Areas[areaIndex].Slots[playerId].LockedCards.Count + Areas[areaIndex].Slots[playerId].PendingCards.Count;

    public List<int> GetAllAreaCards(int playerId, int areaIndex)
    {
        var slot = Areas[areaIndex].Slots[playerId];
        var result = new List<int>(slot.LockedCards);
        result.AddRange(slot.PendingCards);
        return result;
    }
}

}
