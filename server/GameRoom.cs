using System;
using Fleck;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FoolCardServer
{

public class GameRoom
{
    public string RoomId { get; }
    private readonly object _lock = new();
    private readonly GameState _state;
    private readonly IWebSocketConnection?[] _sockets = new IWebSocketConnection?[2];

    private const int MaxDiscards = 5;
    private const int MaxPerArea = 4;
    private const int InitialHandSize = 5;

    private int GetCardsPerRound() => _state.CurrentRound == 4 ? 3 : 2;

    public GameRoom(string roomId)
    {
        RoomId = roomId;
        _state = new GameState { RoomId = roomId };
    }

    public int TryJoin(IWebSocketConnection socket, string playerName)
    {
        lock (_lock)
        {
            for (int i = 0; i < 2; i++)
            {
                if (_sockets[i] == null || !_sockets[i]!.IsAvailable)
                {
                    string assignedName = playerName;
                    // Check if opponent has the same name
                    if (_sockets[1 - i] != null && _sockets[1 - i]!.IsAvailable && _state.Players[1 - i].Name == assignedName)
                    {
                        assignedName += " (2)";
                    }

                    _sockets[i] = socket;
                    _state.Players[i].Name = assignedName;
                    _state.Players[i].IsReady = false;
                    _state.Players[i].HasConfirmed = false;
                    _state.Players[i].Hand = new List<int>();
                    _state.Players[i].DiscardCount = 0;

                    if (i == 1 && _state.Phase == GamePhase.GAME_END) {
                        _state.Phase = GamePhase.WAITING;
                        _state.CurrentRound = 0;
                    }

                    socket.Send(Json.Serialize(new RoomJoinedMsg(
                        "room_joined", i, RoomId, assignedName, 
                        _sockets[1 - i]?.IsAvailable == true ? _state.Players[1 - i].Name : null)));

                    _sockets[1 - i]?.Send(Json.Serialize(
                        new PlayerJoinedMsg("player_joined", i, assignedName)));

                    return i;
                }
            }
            return -1;
        }
    }

    public void HandleSetReady(int playerId)
    {
        lock (_lock)
        {
            if (_state.Phase != GamePhase.WAITING) return;
            _state.Players[playerId].IsReady = true;

            _sockets[1 - playerId]?.Send(Json.Serialize(new PlayerReadyMsg("player_ready", playerId)));

            if (_state.Players[0].IsReady && _state.Players[1].IsReady)
                StartGame();
        }
    }

    private void StartGame()
    {
        _state.Phase = GamePhase.IN_GAME;
        _state.CurrentRound = 1;
        
        var deck = Enumerable.Range(0, 52).ToList();
        var rng = new Random();
        for (int i = deck.Count - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        _state.Areas[0].EffectType = AreaEffectType.BonusForA2358;
        _state.Areas[0].EffectText = "点数为A、2、3、5、8的牌额外加15分";
        _state.Areas[1].EffectType = AreaEffectType.BonusForWands;
        _state.Areas[1].EffectText = "权杖牌额外加15分";
        _state.Areas[2].EffectType = AreaEffectType.BonusForOdd;
        _state.Areas[2].EffectText = "每有1张奇数牌加15分";

        for (int a = 0; a < 3; a++) {
            _state.Areas[a].CommunityCards = new List<int> { deck[a * 2], deck[a * 2 + 1] };
            _state.Areas[a].CommunityRevealed = (a == 0);
            _state.Areas[a].Slots[0].PendingCards.Clear();
            _state.Areas[a].Slots[0].LockedCards.Clear();
            _state.Areas[a].Slots[1].PendingCards.Clear();
            _state.Areas[a].Slots[1].LockedCards.Clear();
        }

        _state.Players[0].Hand = deck.Skip(6).Take(InitialHandSize).ToList();
        _state.Players[1].Hand = deck.Skip(6 + InitialHandSize).Take(InitialHandSize).ToList();
        _state.DrawPile = deck.Skip(6 + InitialHandSize * 2).ToList();
        _state.Players[0].HasConfirmed = false;
        _state.Players[1].HasConfirmed = false;
        
        BroadcastState();
    }

    public void HandlePlaceCard(int p, PlaceCardMsg m) { 
        lock(_lock) {
            if (_state.Phase != GamePhase.IN_GAME || _state.Players[p].HasConfirmed) return;
            if (!_state.Players[p].Hand.Contains(m.CardId)) return;
            if (_state.PlayerTotalPlacedInArea(p, m.AreaIndex) >= MaxPerArea) return;
            if (_state.PlayerPendingCount(p) >= GetCardsPerRound()) return;

            _state.Players[p].Hand.Remove(m.CardId);
            _state.Areas[m.AreaIndex].Slots[p].PendingCards.Add(m.CardId);
            BroadcastState();
        }
    }

    public void HandleTakeBack(int p, TakeBackCardMsg m) {
        lock(_lock) {
            if (_state.Phase != GamePhase.IN_GAME || _state.Players[p].HasConfirmed) return;
            var pending = _state.Areas[m.AreaIndex].Slots[p].PendingCards;
            if (!pending.Contains(m.CardId)) return;
            pending.Remove(m.CardId);
            _state.Players[p].Hand.Add(m.CardId);
            BroadcastState();
        }
    }

    public void HandleConfirmTurn(int p, ConfirmTurnMsg m) {
        lock(_lock) {
            if (_state.Phase != GamePhase.IN_GAME || _state.Players[p].HasConfirmed) return;
            if (_state.PlayerPendingCount(p) != GetCardsPerRound()) return;
            _state.Players[p].HasConfirmed = true;
            if (_state.Players[0].HasConfirmed && _state.Players[1].HasConfirmed) AdvanceRound();
            else BroadcastState();
        }
    }

    public void HandleDiscard(int p, DiscardCardMsg m) {
        lock(_lock) {
            if (_state.Phase != GamePhase.IN_GAME || _state.Players[p].HasConfirmed) return;
            var player = _state.Players[p];
            if (player.DiscardCount >= MaxDiscards || !player.Hand.Contains(m.CardId) || _state.DrawPile.Count == 0) return;
            player.Hand.Remove(m.CardId);
            player.DiscardCount++;
            player.Hand.Add(_state.DrawPile[0]);
            _state.DrawPile.RemoveAt(0);
            BroadcastState();
        }
    }

    public void HandleDisconnect(int p) { 
        lock(_lock) { 
            _sockets[p] = null; 
            if (_state.Phase == GamePhase.IN_GAME) {
                _state.Phase = GamePhase.GAME_END;
                var msg = new ErrorMsg("opponent_disconnected", "DISCONNECT", "Opponent left. You win!");
                _sockets[1 - p]?.Send(Json.Serialize(msg));
            }
        } 
    }

    private void AdvanceRound() {
        for (int p = 0; p < 2; p++) {
            foreach (var a in _state.Areas) {
                a.Slots[p].LockedCards.AddRange(a.Slots[p].PendingCards);
                a.Slots[p].PendingCards.Clear();
            }
            _state.Players[p].HasConfirmed = false;
        }
        _state.CurrentRound++;
        if (_state.CurrentRound > 4) { _state.Phase = GamePhase.GAME_END; BroadcastState(); return; }
        
        if (_state.CurrentRound == 2) _state.Areas[1].CommunityRevealed = true;
        if (_state.CurrentRound == 3) _state.Areas[2].CommunityRevealed = true;
        
        for (int p = 0; p < 2; p++) {
            for (int d = 0; d < 2 && _state.DrawPile.Count > 0; d++) {
                _state.Players[p].Hand.Add(_state.DrawPile[0]);
                _state.DrawPile.RemoveAt(0);
            }
        }
        BroadcastState();
    }

    private ScoreDto[] CalculateScores()
    {
        var scores = new ScoreDto[2];
        var areaWins = new int[2];
        var areaScores = new AreaScoreDto[2][];
        for (int pid = 0; pid < 2; pid++) areaScores[pid] = new AreaScoreDto[3];

        for (int a = 0; a < 3; a++)
        {
            var area = _state.Areas[a];
            var pub = area.CommunityRevealed ? area.CommunityCards : new List<int>();

            var results = new HandResult[2];
            for (int pid = 0; pid < 2; pid++)
            {
                var placed = _state.GetAllAreaCards(pid, a);
                results[pid] = HandEvaluator.Evaluate(placed, pub, area.EffectType);
            }

            bool draw = results[0].Total == results[1].Total;
            for (int pid = 0; pid < 2; pid++)
            {
                bool won = !draw && results[pid].Total > results[1 - pid].Total;
                if (won) areaWins[pid]++;
                areaScores[pid][a] = new AreaScoreDto(
                    a, results[pid].CategoryName, results[pid].CategoryPoints,
                    results[pid].SumPoints, results[pid].SpecialPoints, results[pid].Total, won, draw);
            }
        }

        bool gameDraw = areaWins[0] == areaWins[1];
        for (int pid = 0; pid < 2; pid++)
            scores[pid] = new ScoreDto(
                pid, areaScores[pid], areaWins[pid],
                !gameDraw && areaWins[pid] > areaWins[1 - pid], gameDraw);
                
        return scores;
    }

    private void BroadcastState()
    {
        _state.StateVersion++;
        string phaseStr = _state.Phase.ToString();
        var scores = CalculateScores(); // Always calculate scores for real-time display

        for (int pid = 0; pid < 2; pid++) {
            var sock = _sockets[pid];
            if (sock == null || !sock.IsAvailable) continue;

            var areaDtos = _state.Areas.Select(a => new AreaInfoDto(
                a.AreaIndex, (int)a.EffectType, a.EffectText,
                a.CommunityRevealed ? a.CommunityCards.ToArray() : new int[0],
                a.CommunityRevealed,
                a.Slots.Select((s, spid) => new PlacementInfoDto(spid, s.LockedCards.ToArray(), s.PendingCards.ToArray())).ToArray()
            )).ToArray();

            var playerDtos = _state.Players.Select(p => new PlayerInfoDto(
                p.PlayerId, p.Name, p.IsReady,
                p.PlayerId == pid ? p.Hand.ToArray() : new int[0],
                p.DiscardCount, _state.PlayerPendingCount(p.PlayerId), p.HasConfirmed
            )).ToArray();

            var msg = new StateUpdateMsg("state_update", _state.StateVersion, phaseStr, 
                                        _state.CurrentRound, playerDtos, areaDtos, scores);
            sock.Send(Json.Serialize(msg));
        }
    }
}

}
