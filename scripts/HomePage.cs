using Godot;
using System.Collections.Generic;

namespace FoolCard
{
	public partial class HomePage : Node2D
	{
		private static string DefaultServer => OS.IsDebugBuild() ? "ws://127.0.0.1:8080"
																 : "ws://kirayuukiasuna.cloud:61018";

		private LineEdit _serverInput  = null!;
		private LineEdit _roomInput    = null!;
		private LineEdit _nameInput    = null!;
		private Button   _joinBtn      = null!;
		private Button   _matchBtn     = null!;
		private Button   _readyBtn     = null!;
		private Label    _statusLabel  = null!;
		private TextureRect _bgTex     = null!;
		private Label    _title        = null!;
		private Label    _subTitle     = null!;
		private Label    _serverLabel  = null!;
		private Label    _nameLabel    = null!;
		private Label    _roomLabel    = null!;
		private Button   _langBtn      = null!;
		private int _bgIndex           = 0;
		private bool _isMatchmaking    = false;
		private bool _suppressDisconnectStatus = false;

		public override void _Ready()
		{
			BuildUI();
			UpdateLangUI();
			NetworkManager.Instance.Connected += OnConnected;
			NetworkManager.Instance.Disconnected += OnDisconnected;
			NetworkManager.Instance.MessageReceived += OnMessage;
			
			var timer = new Godot.Timer();
			timer.WaitTime = 5.0f;
			timer.Autostart = true;
			timer.Timeout += () => {
				_bgIndex++;
				_bgTex.Texture = GetBg(_bgIndex);
			};
			AddChild(timer);
		}

		public override void _ExitTree()
		{
			NetworkManager.Instance.Connected -= OnConnected;
			NetworkManager.Instance.Disconnected -= OnDisconnected;
			NetworkManager.Instance.MessageReceived -= OnMessage;
		}

		private void BuildUI()
		{
			// 1. 背景层
			var bgCanvas = new CanvasLayer { Layer = -1 };
			AddChild(bgCanvas);
			var bgCol = new ColorRect { Color = new Color("#01030a") };
			bgCol.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			bgCanvas.AddChild(bgCol);
			_bgTex = new TextureRect {
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				Modulate = new Color(1, 1, 1, 0.4f)
			};
			_bgTex.Texture = GetBg(_bgIndex);
			_bgTex.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			bgCanvas.AddChild(_bgTex);

			// 2. UI层
			var uiCanvas = new CanvasLayer();
			AddChild(uiCanvas);
			var uiRoot = new Control();
			uiRoot.Theme = I18n.GetTheme();
			uiRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			uiCanvas.AddChild(uiRoot);

			// 多语言切换 (右上角)
			_langBtn = new Button { CustomMinimumSize = new Vector2(100, 50) };
			var langStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.2f, 0.4f, 0.8f) };
			langStyle.SetCornerRadiusAll(10);
			_langBtn.AddThemeStyleboxOverride("normal", langStyle);
			_langBtn.AddThemeStyleboxOverride("hover", langStyle);
			_langBtn.AddThemeStyleboxOverride("pressed", langStyle);
			_langBtn.AddThemeFontSizeOverride("font_size", 20);
			_langBtn.SetAnchorsPreset(Control.LayoutPreset.TopRight);
			_langBtn.OffsetLeft = -140; _langBtn.OffsetTop = 40;
			_langBtn.OffsetRight = -40; _langBtn.OffsetBottom = 90;
			_langBtn.Pressed += () => {
				NetworkManager.Instance.PlaySound("BH3_Tab_Select");
				I18n.CurrentLang = I18n.CurrentLang == "zh" ? "en" : "zh";
				UpdateLangUI();
			};

			// 中央卡牌
			var center = new CenterContainer();
			center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			uiRoot.AddChild(center);
			// 語言按鈕必須在 center 之後加入，確保它在最上層接收輸入
			uiRoot.AddChild(_langBtn);

			var card = new PanelContainer();
			card.CustomMinimumSize = new Vector2(440, 0);
			var cardStyle = new StyleBoxFlat {
				BgColor = new Color(0.08f, 0.12f, 0.18f, 0.9f),
				BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
				BorderColor = new Color("#84dcff66")
			};
			cardStyle.SetCornerRadiusAll(20);
			card.AddThemeStyleboxOverride("panel", cardStyle);
			center.AddChild(card);

			var margin = new MarginContainer();
			foreach (var s in new[]{"margin_top","margin_bottom","margin_left","margin_right"})
				margin.AddThemeConstantOverride(s, 35);
			card.AddChild(margin);

			var vbox = new VBoxContainer();
			vbox.AddThemeConstantOverride("separation", 18);
			margin.AddChild(vbox);

			_title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
			_title.AddThemeFontSizeOverride("font_size", 42);
			_title.Modulate = new Color("#ffd56d");
			vbox.AddChild(_title);

			_subTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.6f, 0.7f, 0.8f) };
			vbox.AddChild(_subTitle);
			vbox.AddChild(new HSeparator());

			_serverInput = CreateInput(DefaultServer);
			vbox.AddChild(CreateField(out _serverLabel, _serverInput));

			_nameInput = CreateInput("Player" + GD.Randi() % 1000);
			vbox.AddChild(CreateField(out _nameLabel, _nameInput));

			_roomInput = CreateInput("");
			vbox.AddChild(CreateField(out _roomLabel, _roomInput));

			var btnHbox = new HBoxContainer();
			btnHbox.AddThemeConstantOverride("separation", 10);
			vbox.AddChild(btnHbox);

			_matchBtn = CreateButton(new Color("#d151ce"), Colors.White);
			_matchBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_matchBtn.Pressed += () => { NetworkManager.Instance.PlaySound("PJMS_UI_Button_Click"); OnConnectPressed(true); };
			btnHbox.AddChild(_matchBtn);

			_joinBtn = CreateButton(new Color("#ffd56d"), new Color("#321d00"));
			_joinBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_joinBtn.Pressed += () => { NetworkManager.Instance.PlaySound("PJMS_UI_Button_Click"); OnConnectPressed(false); };
			btnHbox.AddChild(_joinBtn);

			_readyBtn = CreateButton(new Color("#84dcff"), Colors.Black);
			_readyBtn.Disabled = true;
			_readyBtn.Pressed += () => { NetworkManager.Instance.PlaySound("BH3_Generic_Select"); OnReadyPressed(); };
			vbox.AddChild(_readyBtn);

			_statusLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.5f, 0.5f, 0.5f) };
			vbox.AddChild(_statusLabel);
		}

		private void UpdateLangUI()
		{
			_langBtn.Text = I18n.CurrentLang == "zh" ? "中/EN" : "EN/ZH";
			_title.Text = I18n.T("THE_FOOL_CARD");
			_subTitle.Text = I18n.T("AUTHORITATIVE_DUEL");
			_serverLabel.Text = I18n.T("SERVER_ADDRESS");
			_nameLabel.Text = I18n.T("PLAYER_IDENTITY");
			_roomLabel.Text = I18n.T("PRIVATE_ROOM");
			_matchBtn.Text = _isMatchmaking ? I18n.T("CANCEL_MATCH") : I18n.T("FIND_MATCH");
			_joinBtn.Text = I18n.T("JOIN_ROOM");
			_readyBtn.Text = I18n.T("READY_UP");
			if (_statusLabel.Text == "OFFLINE" || _statusLabel.Text == "离线" || _statusLabel.Text == "Offline")
				SetStatus(I18n.T("OFFLINE"), false);
		}

		private VBoxContainer CreateField(out Label l, Control input)
		{
			var v = new VBoxContainer();
			l = new Label { Modulate = new Color("#84dcffaa") };
			l.AddThemeFontSizeOverride("font_size", 14);
			v.AddChild(l);
			v.AddChild(input);
			return v;
		}

		private LineEdit CreateInput(string text)
		{
			var edit = new LineEdit { Text = text, CustomMinimumSize = new Vector2(0, 45) };
			var style = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.4f) };
			style.SetCornerRadiusAll(10);
			style.SetBorderWidthAll(1);
			style.BorderColor = new Color(1, 1, 1, 0.1f);
			edit.AddThemeStyleboxOverride("normal", style);
			return edit;
		}

		private Button CreateButton(Color bg, Color fg)
		{
			var btn = new Button { CustomMinimumSize = new Vector2(0, 55) };
			var style = new StyleBoxFlat { BgColor = bg };
			style.SetCornerRadiusAll(12);
			btn.AddThemeStyleboxOverride("normal", style);
			btn.AddThemeStyleboxOverride("hover", style);
			btn.AddThemeStyleboxOverride("pressed", style);
			btn.AddThemeColorOverride("font_color", fg);
			btn.AddThemeColorOverride("font_hover_color", fg);
			btn.AddThemeFontSizeOverride("font_size", 18);
			return btn;
		}

		private void OnConnectPressed(bool isMatchmaking)
		{
			string url = _serverInput.Text;
			if (string.IsNullOrEmpty(url)) return;

			if (isMatchmaking && _isMatchmaking)
			{
				NetworkManager.Instance.Send(ClientMsg.CancelMatch());
				_isMatchmaking = false;
				UpdateLangUI();
				_joinBtn.Disabled = false;
				SetStatus(I18n.T("OFFLINE"), false);
				NetworkManager.Instance.Disconnect();
				return;
			}

			_isMatchmaking = isMatchmaking;
			SetStatus(I18n.T("COMMUNICATING"), false);
			_matchBtn.Text = isMatchmaking ? I18n.T("CANCEL_MATCH") : I18n.T("FIND_MATCH");
			_joinBtn.Disabled = true;
			
			Error err = NetworkManager.Instance.ConnectToServer(url);
			if (err != Error.Ok) {
				SetStatus(I18n.T("CONNECTION_FAILED"), true);
				_matchBtn.Disabled = false;
				_isMatchmaking = false;
				UpdateLangUI();
				_joinBtn.Disabled = false;
			}
		}

		private void OnReadyPressed()
		{
			NetworkManager.Instance.Send(ClientMsg.SetReady());
			_readyBtn.Disabled = true;
			SetStatus(I18n.T("WAITING_OPPONENT"), false);
		}

		private void OnConnected()
		{
			if (_isMatchmaking)
			{
				SetStatus(I18n.T("SEARCHING_OPPONENT"), false);
				NetworkManager.Instance.Send(ClientMsg.FindMatch(_nameInput.Text));
			}
			else
			{
				SetStatus(I18n.T("JOINING_ROOM"), false);
				NetworkManager.Instance.Send(ClientMsg.JoinRoom(_roomInput.Text, _nameInput.Text));
			}
		}

		private void OnDisconnected()
		{
			if (!_suppressDisconnectStatus)
				SetStatus(I18n.T("DISCONNECTED"), true);
			_suppressDisconnectStatus = false;
			_joinBtn.Disabled = false;
			_matchBtn.Disabled = false;
			_isMatchmaking = false;
			UpdateLangUI();
			_readyBtn.Disabled = true;
		}

		private void OnMessage(string json)
		{
			string? type = Json.GetType(json);
			switch (type)
			{
				case "match_cancelled":
					SetStatus(I18n.T("OFFLINE"), false);
					_isMatchmaking = false;
					UpdateLangUI();
					_joinBtn.Disabled = false;
					_matchBtn.Disabled = false;
					NetworkManager.Instance.Disconnect();
					break;
				case "room_joined":
					var rj = Json.Deserialize<RoomJoined>(json);
					if (rj != null)
					{
						NetworkManager.Instance.SetPlayerInfo(rj.PlayerId, rj.RoomId, rj.PlayerName);
						SetStatus(I18n.T("IDENTITY_VERIFIED", rj.PlayerName), false);
						_joinBtn.Disabled = true;
						_matchBtn.Disabled = true;
						_readyBtn.Disabled = false;
					}
					break;
				case "player_ready":
					var pr = Json.Deserialize<PlayerReady>(json);
					if (pr != null && pr.PlayerId != NetworkManager.Instance.MyPlayerId)
					{
						NetworkManager.Instance.PlaySound("BH3_Generic_Select");
						SetStatus(I18n.T("OPPONENT_READY"), false);
					}
					break;
				case "state_update":
					var su = Json.Deserialize<StateUpdate>(json);
					if (su != null && su.Phase == "IN_GAME")
					{
						NetworkManager.Instance.PlaySound("BH3_Window_Open");
						NetworkManager.Instance.StateVersion = su.StateVersion;
						NetworkManager.Instance.PendingStateUpdate = json;
						GetTree().ChangeSceneToFile("res://gamePage.tscn");
					}
					break;
				case "error":
					var err = Json.Deserialize<ServerError>(json);
					SetStatus(I18n.T("ERROR", err?.Message ?? ""), true);
					_joinBtn.Disabled = false;
					_matchBtn.Disabled = false;
					_isMatchmaking = false;
					UpdateLangUI();
					_suppressDisconnectStatus = true; // 不让断开事件覆盖错误信息
					NetworkManager.Instance.Disconnect();
					break;
			}
		}

		private void SetStatus(string text, bool error = false)
		{
			_statusLabel.Text = text;
			_statusLabel.Modulate = error ? new Color(1, 0.4f, 0.4f) : new Color("#ffd56d");
		}

		public static Texture2D GetBg(int index)
		{
			string[] bgs = {
				"1a06e953363c6cfc798ee90bd6cbb1db_2542912024253540717.png",
				"1b3015542e19d8ffd63124c2a276f14d_7134451685971220867.png",
                "5b60764dabc3bf50c2ad7b5ff8eae80b_2607687582866761407.png"
			};
			return ResourceLoader.Load<Texture2D>($"res://bg/{bgs[index % bgs.Length]}");
		}
	}
}
