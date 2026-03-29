using System.Collections.Generic;
using Godot;

namespace FoolCard
{
	public static class I18n
	{
		public static string CurrentLang = "zh";

		private static readonly Dictionary<string, Dictionary<string, string>> Dict = new()
		{
			{ "zh", new Dictionary<string, string>
				{
					{ "THE_FOOL_CARD", "愚者之战 THE FOOL CARD" },
					{ "AUTHORITATIVE_DUEL", "权威对决" },
					{ "SERVER_ADDRESS", "服务器地址" },
					{ "PLAYER_IDENTITY", "玩家名称" },
					{ "PRIVATE_ROOM", "私人房间 (选填)" },
					{ "FIND_MATCH", "寻找对局" },
					{ "CANCEL_MATCH", "取消匹配" },
					{ "JOIN_ROOM", "加入房间" },
					{ "READY_UP", "准备就绪" },
					{ "OFFLINE", "离线" },
					{ "COMMUNICATING", "连接中..." },
					{ "WAITING_OPPONENT", "等待对手..." },
					{ "SEARCHING_OPPONENT", "搜索对手中..." },
					{ "JOINING_ROOM", "加入房间中..." },
					{ "DISCONNECTED", "已断开连接" },
					{ "OPPONENT_READY", "对手已准备" },
					{ "IDENTITY_VERIFIED", "身份验证成功: {0}" },
					{ "CONNECTION_FAILED", "连接失败" },
					{ "ERROR", "错误: {0}" },
					{ "ROUND", "回合: {0}/4" },
					{ "PHASE", "阶段: {0}" },
					{ "WAITING", "等待中" },
					{ "YOUR_TURN", "你的回合" },
					{ "DISCARDS", "剩余换牌: {0}" },
					{ "CONFIRM_TURN", "结束回合\nCONFIRM" },
					{ "SURRENDER", "认输返回\nSURRENDER" },
					{ "OPPONENT", "对手 Opponent: " },
					{ "YOU", "我方 You: " },
					{ "COMMUNITY", "公共 Community" },
					{ "AREA", "区域 {0}" },
					{ "YOUR_HAND", "你的手牌" },
					{ "YOUR_CARDS", "你的出牌" },
					{ "NO_EFFECT", "无特殊效果" },
					{ "WIN", "胜利 (WINNER!)" },
					{ "DEFEAT", "失败 (DEFEAT)" },
					{ "DRAW", "平局 (DRAW)" },
					{ "YOU_WON_AREAS", "你赢得了 {0} 个区域" },
					{ "RETURN_HOME", "返回主页" },
					{ "DISCARD_MODE", "换牌" },
					{ "SELECT_MORE", "剩余出牌数: {0}" },
					{ "READY_TO_CONFIRM", "可结束回合" },
					{ "WAIT_OPP_PLAY", "等待对手出牌..." },
					{ "SURRENDER_WARN", "确定要认输并返回主页吗？\n(对手将直接获胜)" },
					{ "WARNING", "警告" },
					{ "OPP_DISCONNECTED", "胜利 (WIN)\n对手逃跑" }
				}
			},
			{ "en", new Dictionary<string, string>
				{
					{ "THE_FOOL_CARD", "THE FOOL CARD" },
					{ "AUTHORITATIVE_DUEL", "Authoritative Duel" },
					{ "SERVER_ADDRESS", "SERVER ADDRESS" },
					{ "PLAYER_IDENTITY", "PLAYER IDENTITY" },
					{ "PRIVATE_ROOM", "PRIVATE ROOM (OPTIONAL)" },
					{ "FIND_MATCH", "FIND MATCH" },
					{ "JOIN_ROOM", "JOIN ROOM" },
					{ "READY_UP", "READY UP" },
					{ "OFFLINE", "OFFLINE" },
					{ "COMMUNICATING", "COMMUNICATING..." },
					{ "WAITING_OPPONENT", "WAITING FOR OPPONENT..." },
					{ "SEARCHING_OPPONENT", "SEARCHING FOR OPPONENT..." },
					{ "JOINING_ROOM", "JOINING ROOM..." },
					{ "DISCONNECTED", "DISCONNECTED" },
					{ "OPPONENT_READY", "OPPONENT IS READY" },
					{ "IDENTITY_VERIFIED", "IDENTITY VERIFIED: {0}" },
					{ "CONNECTION_FAILED", "CONNECTION FAILED" },
					{ "ERROR", "ERROR: {0}" },
					{ "ROUND", "ROUND {0}/4" },
					{ "PHASE", "PHASE: {0}" },
					{ "WAITING", "WAITING" },
					{ "YOUR_TURN", "YOUR TURN" },
					{ "DISCARDS", "Discards: {0}" },
					{ "CONFIRM_TURN", "CONFIRM\nTURN" },
					{ "SURRENDER", "SURRENDER\n& LEAVE" },
					{ "OPPONENT", "Opponent: " },
					{ "YOU", "You: " },
					{ "COMMUNITY", "Community" },
					{ "AREA", "AREA {0}" },
					{ "YOUR_HAND", "YOUR HAND" },
					{ "YOUR_CARDS", "Your Cards" },
					{ "NO_EFFECT", "No Effect" },
					{ "WIN", "WINNER!" },
					{ "DEFEAT", "DEFEAT" },
					{ "DRAW", "DRAW" },
					{ "YOU_WON_AREAS", "You won {0} areas" },
					{ "RETURN_HOME", "RETURN TO HOME" },
					{ "DISCARD_MODE", "Discard" },
					{ "SELECT_MORE", "Select {0} more" },
					{ "READY_TO_CONFIRM", "Ready to Confirm" },
					{ "WAIT_OPP_PLAY", "Waiting for opponent..." },
					{ "SURRENDER_WARN", "Are you sure you want to surrender?\n(Opponent will win automatically)" },
					{ "WARNING", "Warning" },
					{ "OPP_DISCONNECTED", "WINNER\nOpponent Disconnected" }
				}
			}
		};

		public static string T(string key, params object[] args)
		{
			if (Dict.TryGetValue(CurrentLang, out var langDict) && langDict.TryGetValue(key, out var text))
			{
				if (args.Length > 0) return string.Format(text, args);
				return text;
			}
			return key;
		}

		private static Font? _font;
		public static Theme GetTheme()
		{
			if (_font == null)
			{
				_font = ResourceLoader.Load<Font>("res://font/zh-cn.ttf");
			}
			var theme = new Theme();
			theme.DefaultFont = _font;
			return theme;
		}
	}
}
