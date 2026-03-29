using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoolCardServer
{

// ── Client �?Server ──────────────────────────────────────────────────────────

public record JoinRoomMsg(
    [property: JsonPropertyName("roomId")] string RoomId,
    [property: JsonPropertyName("playerName")] string PlayerName);

public record FindMatchMsg(
    [property: JsonPropertyName("playerName")] string PlayerName);

public record PlaceCardMsg(
    [property: JsonPropertyName("stateVersion")] int StateVersion,
    [property: JsonPropertyName("cardId")] int CardId,
    [property: JsonPropertyName("areaIndex")] int AreaIndex);

public record TakeBackCardMsg(
    [property: JsonPropertyName("stateVersion")] int StateVersion,
    [property: JsonPropertyName("cardId")] int CardId,
    [property: JsonPropertyName("areaIndex")] int AreaIndex);

public record ConfirmTurnMsg(
    [property: JsonPropertyName("stateVersion")] int StateVersion);

public record DiscardCardMsg(
    [property: JsonPropertyName("stateVersion")] int StateVersion,
    [property: JsonPropertyName("cardId")] int CardId);

// ── Server �?Client ──────────────────────────────────────────────────────────

public record RoomJoinedMsg(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("playerId")] int PlayerId,
    [property: JsonPropertyName("roomId")] string RoomId,
    [property: JsonPropertyName("playerName")] string PlayerName,
    [property: JsonPropertyName("opponentName")] string? OpponentName);

public record PlayerJoinedMsg(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("playerId")] int PlayerId,
    [property: JsonPropertyName("playerName")] string PlayerName);

public record PlayerReadyMsg(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("playerId")] int PlayerId);

public record ErrorMsg(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);

public record PlacementInfoDto(
    [property: JsonPropertyName("playerId")] int PlayerId,
    [property: JsonPropertyName("lockedCards")] int[] LockedCards,
    [property: JsonPropertyName("pendingCards")] int[] PendingCards);

public record AreaInfoDto(
    [property: JsonPropertyName("areaIndex")] int AreaIndex,
    [property: JsonPropertyName("effectType")] int EffectType,
    [property: JsonPropertyName("effectText")] string EffectText,
    [property: JsonPropertyName("communityCards")] int[]? CommunityCards,
    [property: JsonPropertyName("communityRevealed")] bool CommunityRevealed,
    [property: JsonPropertyName("placements")] PlacementInfoDto[] Placements);

public record PlayerInfoDto(
    [property: JsonPropertyName("playerId")] int PlayerId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isReady")] bool IsReady,
    [property: JsonPropertyName("hand")] int[]? Hand,
    [property: JsonPropertyName("discardCount")] int DiscardCount,
    [property: JsonPropertyName("pendingCount")] int PendingCount,
    [property: JsonPropertyName("hasConfirmed")] bool HasConfirmed);

public record StateUpdateMsg(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("stateVersion")] int StateVersion,
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("currentRound")] int CurrentRound,
    [property: JsonPropertyName("players")] PlayerInfoDto[] Players,
    [property: JsonPropertyName("areas")] AreaInfoDto[] Areas,
    [property: JsonPropertyName("scores")] ScoreDto[]? Scores);

public record AreaScoreDto(
    [property: JsonPropertyName("areaIndex")] int AreaIndex,
    [property: JsonPropertyName("handCategory")] string HandCategory,
    [property: JsonPropertyName("categoryPoints")] int CategoryPoints,
    [property: JsonPropertyName("sumPoints")] int SumPoints,
    [property: JsonPropertyName("specialPoints")] int SpecialPoints,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("won")] bool Won,
    [property: JsonPropertyName("draw")] bool Draw);

public record ScoreDto(
    [property: JsonPropertyName("playerId")] int PlayerId,
    [property: JsonPropertyName("areaScores")] AreaScoreDto[] AreaScores,
    [property: JsonPropertyName("areasWon")] int AreasWon,
    [property: JsonPropertyName("isWinner")] bool IsWinner,
    [property: JsonPropertyName("isDraw")] bool IsDraw);

// ── JSON helpers ─────────────────────────────────────────────────────────────

public static class Json
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, Opts);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Opts);

    public static string? GetType(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var el))
                return el.GetString();
        }
        catch { }
        return null;
    }
}

}
