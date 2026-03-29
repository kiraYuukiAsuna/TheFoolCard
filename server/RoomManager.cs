using System;
using System.Collections.Concurrent;
using Fleck;

namespace FoolCardServer
{

public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms  = new();
    // socket guid �?(roomId, playerId)
    private readonly ConcurrentDictionary<Guid, (string roomId, int playerId)> _socketMap = new();
    
    private readonly ConcurrentQueue<(IWebSocketConnection socket, string playerName)> _matchQueue = new();

    public void OnOpen(IWebSocketConnection socket)
    {
        Console.WriteLine($"[+] Connected: {socket.ConnectionInfo.Id}");
    }

    public void OnClose(IWebSocketConnection socket)
    {
        Console.WriteLine($"[-] Disconnected: {socket.ConnectionInfo.Id}");
        
        // Remove from match queue if present
        // (ConcurrentQueue doesn't have Remove, but we can just filter it out when dequeuing by checking IsAvailable)

        if (_socketMap.TryRemove(socket.ConnectionInfo.Id, out var info))
        {
            if (_rooms.TryGetValue(info.roomId, out var room))
                room.HandleDisconnect(info.playerId);
        }
    }

    public void OnMessage(IWebSocketConnection socket, string json)
    {
        string? type = Json.GetType(json);
        if (type == null) return;

        Console.WriteLine($"[MSG] {type} from {socket.ConnectionInfo.Id}");

        if (type == "join_room")
        {
            var msg = Json.Deserialize<JoinRoomMsg>(json);
            if (msg == null) return;
            HandleJoin(socket, msg);
            return;
        }
        
        if (type == "find_match")
        {
            var msg = Json.Deserialize<FindMatchMsg>(json);
            if (msg == null) return;
            HandleFindMatch(socket, msg.PlayerName);
            return;
        }

        if (type == "cancel_match")
        {
            HandleCancelMatch(socket);
            return;
        }

        // All other messages require an established room association
        if (!_socketMap.TryGetValue(socket.ConnectionInfo.Id, out var info))
        {
            socket.Send(Json.Serialize(new ErrorMsg("error", "NOT_IN_ROOM", "Join a room first")));
            return;
        }

        if (!_rooms.TryGetValue(info.roomId, out var r)) return;

        switch (type)
        {
            case "set_ready":
                r.HandleSetReady(info.playerId);
                break;
            case "place_card":
                var pcm = Json.Deserialize<PlaceCardMsg>(json);
                if (pcm != null) r.HandlePlaceCard(info.playerId, pcm);
                break;
            case "take_back_card":
                var tbm = Json.Deserialize<TakeBackCardMsg>(json);
                if (tbm != null) r.HandleTakeBack(info.playerId, tbm);
                break;
            case "confirm_turn":
                var ctm = Json.Deserialize<ConfirmTurnMsg>(json);
                if (ctm != null) r.HandleConfirmTurn(info.playerId, ctm);
                break;
            case "discard_card":
                var dcm = Json.Deserialize<DiscardCardMsg>(json);
                if (dcm != null) r.HandleDiscard(info.playerId, dcm);
                break;
            case "ping":
                socket.Send(Json.Serialize(new { type = "pong" }));
                break;
        }
    }

    private void HandleJoin(IWebSocketConnection socket, JoinRoomMsg msg)
    {
        if (string.IsNullOrWhiteSpace(msg.RoomId) || string.IsNullOrWhiteSpace(msg.PlayerName))
        {
            socket.Send(Json.Serialize(new ErrorMsg("error", "INVALID_ARGS", "roomId and playerName required")));
            return;
        }

        var room = _rooms.GetOrAdd(msg.RoomId, id => new GameRoom(id));
        int playerId = room.TryJoin(socket, msg.PlayerName);

        if (playerId < 0)
        {
            socket.Send(Json.Serialize(new ErrorMsg("error", "ROOM_FULL", "Room is full")));
            return;
        }

        _socketMap[socket.ConnectionInfo.Id] = (msg.RoomId, playerId);
        Console.WriteLine($"  Player {playerId} '{msg.PlayerName}' joined room '{msg.RoomId}'");
    }

    private void HandleFindMatch(IWebSocketConnection socket, string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            socket.Send(Json.Serialize(new ErrorMsg("error", "INVALID_ARGS", "playerName required")));
            return;
        }

        while (_matchQueue.TryDequeue(out var opponent))
        {
            if (opponent.socket.IsAvailable)
            {
                // Found an opponent!
                string roomId = "Match_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var room = _rooms.GetOrAdd(roomId, id => new GameRoom(id));
                
                int oppId = room.TryJoin(opponent.socket, opponent.playerName);
                _socketMap[opponent.socket.ConnectionInfo.Id] = (roomId, oppId);
                
                int myId = room.TryJoin(socket, playerName);
                _socketMap[socket.ConnectionInfo.Id] = (roomId, myId);
                
                Console.WriteLine($"[MATCH] Paired '{opponent.playerName}' and '{playerName}' in room {roomId}");
                return;
            }
        }

        // No opponent found, join queue
        _matchQueue.Enqueue((socket, playerName));
        Console.WriteLine($"[MATCH] '{playerName}' joined matchmaking queue.");
    }

    private void HandleCancelMatch(IWebSocketConnection socket)
    {
        // To remove from ConcurrentQueue without a built-in Remove method, we could
        // rebuild the queue, or rely on the IsAvailable check when dequeuing later.
        // For immediate cancellation to work correctly if they disconnect or cancel:
        // We'll mark their socket as not available, or just ignore it when pulled.
        // Actually, just sending a confirmation is enough if we rely on IsAvailable, but
        // since they are still connected, IsAvailable is true.
        // Let's implement a simple rebuild or a hashset for cancelled sockets.
        // For simplicity, we just close the connection or we can rebuild the queue.
        
        var list = _matchQueue.ToArray();
        _matchQueue.Clear();
        foreach (var item in list)
        {
            if (item.socket.ConnectionInfo.Id != socket.ConnectionInfo.Id)
            {
                _matchQueue.Enqueue(item);
            }
        }
        
        Console.WriteLine($"[MATCH] Socket {socket.ConnectionInfo.Id} cancelled matchmaking.");
        socket.Send(Json.Serialize(new { type = "match_cancelled" }));
    }
}

}
