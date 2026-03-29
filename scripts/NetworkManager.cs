using Godot;

namespace FoolCard
{

/// <summary>
/// Singleton autoload that owns the WebSocket connection.
/// Persists across scene changes.
/// </summary>
public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; } = null!;

	[Signal] public delegate void ConnectedEventHandler();
	[Signal] public delegate void DisconnectedEventHandler();
	[Signal] public delegate void MessageReceivedEventHandler(string json);

	// State set after successful room_joined
	public int    MyPlayerId   { get; private set; } = -1;
	public string MyRoomId     { get; private set; } = "";
	public string MyName       { get; private set; } = "";
	public int    StateVersion { get; set;          } = 0;

	/// <summary>Last state_update JSON received. GamePage reads this on _Ready to catch up.</summary>
	public string? PendingStateUpdate { get; set; }

	private WebSocketPeer _ws = new();
	private WebSocketPeer.State _prevState = WebSocketPeer.State.Closed;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		// 手机端 UI 缩放
		if (OS.GetName() is "Android" or "iOS")
			GetWindow().ContentScaleFactor = 1.5f;
	}

	public override void _Process(double delta)
	{
		_ws.Poll();
		var state = _ws.GetReadyState();

		if (state != _prevState)
		{
			if (state == WebSocketPeer.State.Open)
				EmitSignal(SignalName.Connected);
			else if (_prevState == WebSocketPeer.State.Open)
				EmitSignal(SignalName.Disconnected);
			_prevState = state;
		}

		while (state == WebSocketPeer.State.Open && _ws.GetAvailablePacketCount() > 0)
		{
			string json = _ws.GetPacket().GetStringFromUtf8();
			EmitSignal(SignalName.MessageReceived, json);
		}
	}

	public Error ConnectToServer(string url)
	{
		_ws = new WebSocketPeer();
		_prevState = WebSocketPeer.State.Closed;
		return _ws.ConnectToUrl(url);
	}

	public void Send(string json)
	{
		if (_ws.GetReadyState() == WebSocketPeer.State.Open)
			_ws.SendText(json);
	}

	public new bool IsConnected => _ws.GetReadyState() == WebSocketPeer.State.Open;

	public void SetPlayerInfo(int playerId, string roomId, string name)
	{
		MyPlayerId = playerId;
		MyRoomId   = roomId;
		MyName     = name;
	}

	public void Disconnect()
	{
		_ws.Close();
	}

	public void PlaySound(string audioName)
	{
		var stream = ResourceLoader.Load<AudioStream>($"res://audio/{audioName}.mp3");
		if (stream == null) return;
		var player = new AudioStreamPlayer();
		player.Stream = stream;
		player.Finished += () => player.QueueFree();
		AddChild(player);
		player.Play();
	}
}

}
