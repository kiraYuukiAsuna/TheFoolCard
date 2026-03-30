using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FoolCard
{

public partial class GamePage : Node2D
{
	private static readonly Color ColGold      = new("#ffd56d");
	private static readonly Color ColCyan      = new("#84dcff");
	
	private static readonly Vector2 SizeCard   = new(90, 126);
	private static readonly Vector2 SizeHand   = new(110, 154);

	private Label _roundLabel = null!, _statusLabel = null!, _phaseLabel = null!, _selectionHint = null!, _discardLabel = null!;
	private VBoxContainer _logContainer = null!;
	private HBoxContainer _handContainer = null!;
	private Button _confirmBtn = null!;
	private Button _surrenderBtn = null!;
	private HBoxContainer[] _commRows = new HBoxContainer[3], _mySlots = new HBoxContainer[3], _oppSlots = new HBoxContainer[3];
	
	private Label[] _effectLabels = new Label[3];
	private Label[] _myScoreLabels = new Label[3];
	private Label[] _oppScoreLabels = new Label[3];
	private PanelContainer? _endOverlay;
	private Label _endTitle = null!;
	private VBoxContainer _endDetails = null!;
	private ConfirmationDialog _surrenderDialog = null!;

	private StateUpdate? _lastState;
	private int _selectedCardId = -1;
	private int _myId => NetworkManager.Instance.MyPlayerId;

	public partial class AreaDropZone : PanelContainer
	{
		public int AreaIndex { get; set; }
		public event System.Action<int, int>? OnCardDropped;

		public override bool _CanDropData(Vector2 atPosition, Variant data) => data.VariantType == Variant.Type.Int;

		public override void _DropData(Vector2 atPosition, Variant data)
		{
			if (data.VariantType == Variant.Type.Int)
				OnCardDropped?.Invoke(data.AsInt32(), AreaIndex);
		}
	}

	private Button _langBtn = null!;

	public override void _Ready()
	{
		BuildUI();
		NetworkManager.Instance.MessageReceived += OnMessage;
		AddLog($"[SYSTEM] PlayerID: {_myId}");

		var pending = NetworkManager.Instance.PendingStateUpdate;
		if (pending != null) { NetworkManager.Instance.PendingStateUpdate = null; OnMessage(pending); }
	}

	public override void _ExitTree()
	{
		NetworkManager.Instance.MessageReceived -= OnMessage;
	}

	private void BuildUI()
	{
		var canvas = new CanvasLayer(); AddChild(canvas);

		var uiRoot = new MarginContainer();
		uiRoot.Theme = I18n.GetTheme();
		uiRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		foreach (var s in new[]{"margin_top","margin_bottom","margin_left","margin_right"}) 
			uiRoot.AddThemeConstantOverride(s, 10);
		canvas.AddChild(uiRoot);

		var bgCol = new ColorRect { Color = new Color("#01030a"), ZIndex = -1 };
		bgCol.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		uiRoot.AddChild(bgCol);

		var bgTex = new TextureRect
		{
			Texture = HomePage.GetBg((int)(GD.Randi() % 9)),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			Modulate = new Color(1, 1, 1, 0.4f),
			ZIndex = -1
		};
		bgTex.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		uiRoot.AddChild(bgTex);

		var mainHbox = new HBoxContainer();
		mainHbox.AddThemeConstantOverride("separation", 10);
		uiRoot.AddChild(mainHbox);

		// --- HUD LEFT ---
		var hud = new PanelContainer();
		var hudStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.1f, 0.2f, 0.8f) };
		hudStyle.SetCornerRadiusAll(10);
		hud.AddThemeStyleboxOverride("panel", hudStyle);
		mainHbox.AddChild(hud);

		var hudV = new VBoxContainer();
		hud.AddChild(new MarginContainer()); 
		var hudMargin = new MarginContainer();
		foreach (var s in new[]{"margin_top","margin_bottom","margin_left","margin_right"}) hudMargin.AddThemeConstantOverride(s, 12);
		hud.AddChild(hudMargin);
		hudMargin.AddChild(hudV);

		var titleLbl = new Label { Text = I18n.T("THE_FOOL_CARD"), ThemeTypeVariation = "HeaderLarge" };
		hudV.AddChild(titleLbl);
		_statusLabel = new Label { Text = I18n.T("COMMUNICATING"), Modulate = ColGold }; hudV.AddChild(_statusLabel);
		_phaseLabel = new Label { Text = I18n.T("PHASE", "-"), Modulate = ColCyan }; hudV.AddChild(_phaseLabel);
		hudV.AddChild(new HSeparator());

		var logScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		hudV.AddChild(logScroll);
		_logContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		logScroll.AddChild(_logContainer);

		// --- MAIN STAGE (Center) ---
		var stageV = new VBoxContainer();
		stageV.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		stageV.AddThemeConstantOverride("separation", 10);
		mainHbox.AddChild(stageV);

		// Header (Round)
		var headerH = new HBoxContainer();
		stageV.AddChild(headerH);

		var titleV = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		headerH.AddChild(titleV);
		_roundLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_roundLabel.AddThemeFontSizeOverride("font_size", 32); _roundLabel.Modulate = ColGold;
		titleV.AddChild(_roundLabel);
		_selectionHint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.8f, 0.8f, 0.8f) };
		_selectionHint.AddThemeFontSizeOverride("font_size", 18);
		titleV.AddChild(_selectionHint);

		// Arena (Areas)
		var arena = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		arena.AddThemeConstantOverride("separation", 15);
		// 关键修改：不再使用 ExpandFill，或者给一个较小的 StretchRatio
		arena.SizeFlagsVertical = Control.SizeFlags.ExpandFill; 
		stageV.AddChild(arena);

		for (int i = 0; i < 3; i++)
		{
			var ap = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			var apStyle = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.03f) };
			apStyle.SetCornerRadiusAll(10);
			ap.AddThemeStyleboxOverride("panel", apStyle);
			arena.AddChild(ap);

			var av = new VBoxContainer(); ap.AddChild(av);

			// Opponent score and header
			var oppHeader = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			av.AddChild(oppHeader);
			_oppScoreLabels[i] = new Label { Text = "0", Modulate = new Color(0.8f, 0.4f, 0.4f) };
			_oppScoreLabels[i].AddThemeFontSizeOverride("font_size", 24);
			oppHeader.AddChild(new Label { Text = I18n.T("OPPONENT"), Modulate = new Color(0.8f, 0.4f, 0.4f) });
			oppHeader.AddChild(_oppScoreLabels[i]);

			_oppSlots[i] = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(0, 140) };
			av.AddChild(_oppSlots[i]);

			av.AddChild(new HSeparator());
			_effectLabels[i] = new Label { 
				Text = I18n.T("NO_EFFECT"), 
				HorizontalAlignment = HorizontalAlignment.Center,
				Modulate = new Color(0.7f, 0.9f, 1f),
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			_effectLabels[i].AddThemeFontSizeOverride("font_size", 20);
			av.AddChild(_effectLabels[i]);

			_commRows[i] = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(0, 140) };
			av.AddChild(_commRows[i]);
			av.AddChild(new HSeparator());

			// My score and header
			var myHeader = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			av.AddChild(myHeader);
			_myScoreLabels[i] = new Label { Text = "0", Modulate = ColCyan };
			_myScoreLabels[i].AddThemeFontSizeOverride("font_size", 24);
			myHeader.AddChild(new Label { Text = I18n.T("YOU"), Modulate = ColCyan });
			myHeader.AddChild(_myScoreLabels[i]);

			var myBox = new AreaDropZone { SizeFlagsVertical = Control.SizeFlags.ExpandFill, AreaIndex = i, CustomMinimumSize = new Vector2(0, 140) };
			var myBoxStyle = new StyleBoxFlat { BgColor = new Color(1,1,1,0.06f) };
			myBoxStyle.SetCornerRadiusAll(8);
			myBox.AddThemeStyleboxOverride("panel", myBoxStyle);
			av.AddChild(myBox);
			_mySlots[i] = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
			myBox.AddChild(_mySlots[i]);

			int idx = i;
			myBox.GuiInput += (e) => { if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) OnAreaClicked(idx); };
			myBox.OnCardDropped += (cId, aIdx) => { 
				NetworkManager.Instance.PlaySound("PJMS_UI_Button_Click"); 
				_selectedCardId = cId; 
				OnAreaClicked(aIdx); 
			};
		}

		// Bottom Hand
		var handP = new PanelContainer();
		var handStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.4f) };
		handStyle.SetCornerRadiusAll(15);
		handP.AddThemeStyleboxOverride("panel", handStyle);
		handP.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		stageV.AddChild(handP);

		var handV = new VBoxContainer(); handP.AddChild(handV);
		var handH = new HBoxContainer(); handV.AddChild(handH);
		handH.AddChild(new Label { Text = I18n.T("YOUR_HAND"), Modulate = ColCyan });
		_discardLabel = new Label { Text = I18n.T("DISCARDS", "0/5"), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right };
		handH.AddChild(_discardLabel);
		var disT = new Button { Text = I18n.T("DISCARD_MODE") }; 
		disT.Pressed += () => { NetworkManager.Instance.PlaySound("BH3_Tab_Select"); OnDiscardToggle(); }; 
		handH.AddChild(disT);

		var handS = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
		handV.AddChild(handS);
		_handContainer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		_handContainer.AddThemeConstantOverride("separation", -40);
		handS.AddChild(_handContainer);

		// --- RIGHT ACTION BAR ---
		var rightBar = new PanelContainer();
		rightBar.CustomMinimumSize = new Vector2(100, 0);
		var rightStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.1f, 0.2f, 0.6f) };
		rightStyle.SetCornerRadiusAll(10);
		rightBar.AddThemeStyleboxOverride("panel", rightStyle);
		mainHbox.AddChild(rightBar);

		var rightV = new VBoxContainer();
		rightV.Alignment = BoxContainer.AlignmentMode.Center;
		rightV.AddThemeConstantOverride("separation", 20);
		rightBar.AddChild(rightV);

		_confirmBtn = new Button { Text = I18n.T("CONFIRM_TURN"), CustomMinimumSize = new Vector2(100, 80) };
		var btnStyle = new StyleBoxFlat { BgColor = new Color(0.15f, 0.35f, 0.8f) }; btnStyle.SetCornerRadiusAll(12);
		_confirmBtn.AddThemeStyleboxOverride("normal", btnStyle);
		_confirmBtn.AddThemeFontSizeOverride("font_size", 18);
		_confirmBtn.Pressed += () => { NetworkManager.Instance.PlaySound("PJMS_UI_Button_Tab"); OnConfirmPressed(); };
		rightV.AddChild(_confirmBtn);

		_surrenderBtn = new Button { Text = I18n.T("SURRENDER"), CustomMinimumSize = new Vector2(100, 60) };
		var surStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.2f, 0.2f) }; surStyle.SetCornerRadiusAll(12);
		_surrenderBtn.AddThemeStyleboxOverride("normal", surStyle);
		_surrenderBtn.Pressed += () => { NetworkManager.Instance.PlaySound("BH3_Generic_Select"); OnSurrenderRequested(); };
		rightV.AddChild(_surrenderBtn);

		// --- DIALOGS & OVERLAYS ---
		_surrenderDialog = new ConfirmationDialog();
		_surrenderDialog.Theme = I18n.GetTheme();
		_surrenderDialog.Title = I18n.T("WARNING");
		_surrenderDialog.DialogText = I18n.T("SURRENDER_WARN");
		_surrenderDialog.Confirmed += OnSurrenderConfirmed;
		AddChild(_surrenderDialog);

		_endOverlay = new PanelContainer();
		_endOverlay.Theme = I18n.GetTheme();
		_endOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var endStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.85f) };
		_endOverlay.AddThemeStyleboxOverride("panel", endStyle);
		_endOverlay.Visible = false;
		canvas.AddChild(_endOverlay);

		var endCenter = new CenterContainer();
		_endOverlay.AddChild(endCenter);

		var endV = new VBoxContainer();
		endV.AddThemeConstantOverride("separation", 20);
		endV.Alignment = BoxContainer.AlignmentMode.Center;
		endCenter.AddChild(endV);

		_endTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_endTitle.AddThemeFontSizeOverride("font_size", 64);
		endV.AddChild(_endTitle);

		_endDetails = new VBoxContainer();
		endV.AddChild(_endDetails);

		var returnBtn = new Button { Text = I18n.T("RETURN_HOME"), CustomMinimumSize = new Vector2(240, 70) };
		var rbStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.6f, 0.2f) }; rbStyle.SetCornerRadiusAll(15);
		returnBtn.AddThemeStyleboxOverride("normal", rbStyle);
		returnBtn.AddThemeFontSizeOverride("font_size", 24);
		returnBtn.Pressed += () => { 
			NetworkManager.Instance.PlaySound("BH3_Window_Close"); 
			NetworkManager.Instance.Disconnect(); 
			GetTree().ChangeSceneToFile("res://homePage.tscn"); 
		};
		endV.AddChild(returnBtn);
	}
	private void OnSurrenderRequested() { _surrenderDialog.PopupCentered(); }
	
	private void OnSurrenderConfirmed() {
		NetworkManager.Instance.Disconnect();
		GetTree().ChangeSceneToFile("res://homePage.tscn");
	}

	private void OnMessage(string json)
	{
		string? type = Json.GetType(json);
		if (type == "state_update") {
			var su = Json.Deserialize<StateUpdate>(json);
			if (su != null) ApplyState(su);
		}
		else if (type == "opponent_disconnected") {
			ShowEndOverlay("胜利 (WIN)\n对手逃跑", null);
		}
	}

	private void ApplyState(StateUpdate s)
	{
		_lastState = s;
		if (_myId < 0 || _myId >= s.Players.Length) return;
		var me = s.Players[_myId];

		NetworkManager.Instance.StateVersion = s.StateVersion;
		_roundLabel.Text = $"回合: {s.CurrentRound}/4";
		_phaseLabel.Text = $"PHASE: {s.Phase}";
		_statusLabel.Text = me.HasConfirmed ? "WAITING" : "YOUR TURN";
		_discardLabel.Text = $"剩余换牌: {5 - me.DiscardCount}";
		_confirmBtn.Disabled = !(s.Phase == "IN_GAME" && !me.HasConfirmed && me.PendingCount == (s.CurrentRound == 4 ? 3 : 2));
		_selectionHint.Text = me.HasConfirmed ? "等待对手出牌..." : $"剩余出牌数: ? {(s.CurrentRound == 4 ? 3 : 2) - me.PendingCount}";

		ScoreInfo? myScore = s.Scores?.FirstOrDefault(sc => sc.PlayerId == _myId);
		ScoreInfo? oppScore = s.Scores?.FirstOrDefault(sc => sc.PlayerId == (1 - _myId));

		for (int i = 0; i < 3; i++) {
			ClearContainer(_commRows[i]);
			var area = s.Areas[i];
			
			_effectLabels[i].Text = string.IsNullOrEmpty(area.EffectText) ? "No Effect" : area.EffectText;

			if (myScore != null)
			{
				var asc = myScore.AreaScores.FirstOrDefault(a => a.AreaIndex == i);
				if (asc != null)
				{
					_myScoreLabels[i].Text = $"{asc.Total} ({asc.HandCategory}: {asc.CategoryPoints} + {asc.SumPoints} + {asc.SpecialPoints})";
				}
			}

			if (oppScore != null)
			{
				var asc = oppScore.AreaScores.FirstOrDefault(a => a.AreaIndex == i);
				if (asc != null)
				{
					_oppScoreLabels[i].Text = $"{asc.Total} ({asc.HandCategory}: {asc.CategoryPoints} + {asc.SumPoints} + {asc.SpecialPoints})";
				}
			}

			if (area.CommunityRevealed && area.CommunityCards?.Length > 0) {
				foreach (int c in area.CommunityCards) AddCard(_commRows[i], c, false, SizeCard);
			} else {
				for (int j=0; j<2; j++) AddCard(_commRows[i], 0, true, SizeCard);
			}

			ClearContainer(_mySlots[i]);
			var myP = area.Placements.FirstOrDefault(p => p.PlayerId == _myId);
			if (myP != null) {
				foreach (int c in myP.LockedCards) AddCard(_mySlots[i], c, false, SizeCard);
				foreach (int c in myP.PendingCards) {
					var cd = AddCard(_mySlots[i], c, false, SizeCard);
					cd.SelfModulate = new Color(1.2f, 1.2f, 0.5f);
					int cid = c, aid = i; cd.Clicked += _ => OnTakeBack(cid, aid);
				}
			}

			ClearContainer(_oppSlots[i]);
			var oppP = area.Placements.FirstOrDefault(p => p.PlayerId == (1-_myId));
			if (oppP != null) {
				foreach (int c in oppP.LockedCards) AddCard(_oppSlots[i], c, false, SizeCard);
				foreach (int c in oppP.PendingCards) AddCard(_oppSlots[i], 0, true, SizeCard);
			}
		}

		ClearContainer(_handContainer);
		if (me.Hand != null) {
			foreach (int c in me.Hand) {
				var cd = AddCard(_handContainer, c, false, SizeHand);
				cd.Interactable = !me.HasConfirmed && s.Phase == "IN_GAME";
				if (c == _selectedCardId) cd.SetSelected(true);
				int cid = c; cd.Clicked += _ => {
					if (_discardMode) { DoDiscard(cid); return; }
					_selectedCardId = (_selectedCardId == cid) ? -1 : cid;
					ApplyState(_lastState);
				};
			}
		}

		if (s.Phase == "GAME_END" && myScore != null && oppScore != null)
		{
			string title = myScore.IsWinner ? "胜利 (WINNER!)" : (myScore.IsDraw ? "平局 (DRAW)" : "失败 (DEFEAT)");
			ShowEndOverlay(title, myScore);
		}
	}

	private void ShowEndOverlay(string title, ScoreInfo? score)
	{
		if (_endOverlay == null) return;
		_endTitle.Text = title;
		_endTitle.Modulate = title.Contains("WIN") ? ColGold : (title.Contains("DRAW") ? Colors.White : new Color(0.8f, 0.4f, 0.4f));
		
		ClearContainer(_endDetails);
		if (score != null)
		{
			var l = new Label { Text = $"你赢得了 {score.AreasWon} 个区域",  HorizontalAlignment = HorizontalAlignment.Center };
			l.AddThemeFontSizeOverride("font_size", 28);
			_endDetails.AddChild(l);
		}
		
		_endOverlay.Visible = true;
	}

	private CardDisplay AddCard(Node p, int id, bool down, Vector2 sz) {
		var cd = new CardDisplay(); p.AddChild(cd);
		cd.SetCard(id, down); cd.SetSize(sz); return cd;
	}

	private void OnAreaClicked(int idx) {
		if (_selectedCardId == -1) return;
		NetworkManager.Instance.Send(ClientMsg.PlaceCard(NetworkManager.Instance.StateVersion, _selectedCardId, idx));
		_selectedCardId = -1;
	}

	private void OnTakeBack(int cid, int aid) => NetworkManager.Instance.Send(ClientMsg.TakeBackCard(NetworkManager.Instance.StateVersion, cid, aid));
	private void OnConfirmPressed() => NetworkManager.Instance.Send(ClientMsg.ConfirmTurn(NetworkManager.Instance.StateVersion));
	
	private bool _discardMode = false;
	private void OnDiscardToggle() { _discardMode = !_discardMode; AddLog(_discardMode ? "Discard ON" : "Discard OFF"); }
	private void DoDiscard(int cid) { NetworkManager.Instance.Send(ClientMsg.DiscardCard(NetworkManager.Instance.StateVersion, cid)); _discardMode = false; }

	private void ClearContainer(Node n) { foreach (Node c in n.GetChildren()) c.QueueFree(); }
	private void AddLog(string t, bool err = false) {
		var l = new Label { Text = t }; 
		l.AddThemeFontSizeOverride("font_size", 20);
		if (err) l.Modulate = new Color(1, 0.4f, 0.4f);
		_logContainer.AddChild(l); _logContainer.MoveChild(l, 0);
	}

}

}
