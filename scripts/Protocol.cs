using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoolCard
{

public static class ClientMsg
{
	public static string JoinRoom(string roomId, string playerName) =>
		Json.Serialize(new { type = "join_room", roomId, playerName });

	public static string FindMatch(string playerName) =>
		Json.Serialize(new { type = "find_match", playerName });

	public static string CancelMatch() =>
		Json.Serialize(new { type = "cancel_match" });

	public static string SetReady() =>
		Json.Serialize(new { type = "set_ready" });

	public static string PlaceCard(int stateVersion, int cardId, int areaIndex) =>
		Json.Serialize(new { type = "place_card", stateVersion, cardId, areaIndex });

	public static string TakeBackCard(int stateVersion, int cardId, int areaIndex) =>
		Json.Serialize(new { type = "take_back_card", stateVersion, cardId, areaIndex });

	public static string ConfirmTurn(int stateVersion) =>
		Json.Serialize(new { type = "confirm_turn", stateVersion });

	public static string DiscardCard(int stateVersion, int cardId) =>
		Json.Serialize(new { type = "discard_card", stateVersion, cardId });

	public static string Ping() =>
		Json.Serialize(new { type = "ping" });
}

public class PlacementInfo
{
	[JsonPropertyName("playerId")]    public int   PlayerId    { get; set; }
	[JsonPropertyName("lockedCards")] public int[] LockedCards { get; set; } = System.Array.Empty<int>();
	[JsonPropertyName("pendingCards")]public int[] PendingCards{ get; set; } = System.Array.Empty<int>();
}

public class AreaInfo
{
	[JsonPropertyName("areaIndex")]         public int              AreaIndex         { get; set; }
	[JsonPropertyName("effectType")]        public int              EffectType        { get; set; }
	[JsonPropertyName("effectText")]        public string           EffectText        { get; set; } = "";
	[JsonPropertyName("communityCards")]    public int[]?           CommunityCards    { get; set; }
	[JsonPropertyName("communityRevealed")] public bool             CommunityRevealed { get; set; }
	[JsonPropertyName("placements")]        public PlacementInfo[]  Placements        { get; set; } = System.Array.Empty<PlacementInfo>();
}

public class PlayerInfo
{
	[JsonPropertyName("playerId")]    public int    PlayerId     { get; set; }
	[JsonPropertyName("name")]        public string Name         { get; set; } = "";
	[JsonPropertyName("isReady")]     public bool   IsReady      { get; set; }
	[JsonPropertyName("hand")]        public int[]? Hand         { get; set; }
	[JsonPropertyName("discardCount")]public int    DiscardCount { get; set; }
	[JsonPropertyName("pendingCount")]public int    PendingCount { get; set; }
	[JsonPropertyName("hasConfirmed")]public bool   HasConfirmed { get; set; }
}

public class AreaScoreInfo
{
	[JsonPropertyName("areaIndex")]       public int    AreaIndex       { get; set; }
	[JsonPropertyName("handCategory")]    public string HandCategory    { get; set; } = "";
	[JsonPropertyName("categoryPoints")]  public int    CategoryPoints  { get; set; }
	[JsonPropertyName("sumPoints")]       public int    SumPoints       { get; set; }
	[JsonPropertyName("specialPoints")]   public int    SpecialPoints   { get; set; }
	[JsonPropertyName("total")]           public int    Total           { get; set; }
	[JsonPropertyName("won")]             public bool   Won             { get; set; }
	[JsonPropertyName("draw")]            public bool   Draw            { get; set; }
}

public class ScoreInfo
{
	[JsonPropertyName("playerId")]   public int            PlayerId  { get; set; }
	[JsonPropertyName("areaScores")] public AreaScoreInfo[] AreaScores{ get; set; } = System.Array.Empty<AreaScoreInfo>();
	[JsonPropertyName("areasWon")]   public int            AreasWon  { get; set; }
	[JsonPropertyName("isWinner")]   public bool           IsWinner  { get; set; }
	[JsonPropertyName("isDraw")]     public bool           IsDraw    { get; set; }
}

public class StateUpdate
{
	[JsonPropertyName("type")]          public string        Type         { get; set; } = "";
	[JsonPropertyName("stateVersion")]  public int           StateVersion { get; set; }
	[JsonPropertyName("phase")]         public string        Phase        { get; set; } = "";
	[JsonPropertyName("currentRound")]  public int           CurrentRound { get; set; }
	[JsonPropertyName("players")]       public PlayerInfo[]  Players      { get; set; } = System.Array.Empty<PlayerInfo>();
	[JsonPropertyName("areas")]         public AreaInfo[]    Areas        { get; set; } = System.Array.Empty<AreaInfo>();
	[JsonPropertyName("scores")]        public ScoreInfo[]?  Scores       { get; set; }
}

public class RoomJoined
{
	[JsonPropertyName("type")]         public string  Type         { get; set; } = "";
	[JsonPropertyName("playerId")]     public int     PlayerId     { get; set; }
	[JsonPropertyName("roomId")]       public string  RoomId       { get; set; } = "";
	[JsonPropertyName("playerName")]   public string  PlayerName   { get; set; } = "";
	[JsonPropertyName("opponentName")] public string? OpponentName { get; set; }
}

public class PlayerReady
{
	[JsonPropertyName("type")]     public string Type     { get; set; } = "";
	[JsonPropertyName("playerId")] public int    PlayerId { get; set; }
}

public class ServerError
{
	[JsonPropertyName("type")]    public string Type    { get; set; } = "";
	[JsonPropertyName("code")]    public string Code    { get; set; } = "";
	[JsonPropertyName("message")] public string Message { get; set; } = "";
}

public static class CardInfo
{
	private static readonly string[] RankNames = new string[]
		{"2","3","4","5","6","7","8","9","10","J","Q","K","A"};

	public static string ResPath(int cardId)
	{
		if (cardId < 0 || cardId >= 52) return "res://card/cardback.png";
		string rank = RankNames[cardId / 4];
		int suit = (cardId % 4) + 1;
		return $"res://card/{rank}_{suit}.png";
	}

	public static int PointValue(int cardId)
	{
		int rank = cardId / 4;
		if (rank <= 8) return rank + 2;    // 2-10
		if (rank <= 11) return 10;         // J Q K
		return 11;                          // A
	}
}

public static class Json
{
	private static readonly JsonSerializerOptions Opts = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, Opts);

	public static T? Deserialize<T>(string json)
	{
		try { return JsonSerializer.Deserialize<T>(json, Opts); }
		catch { return default; }
	}

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
