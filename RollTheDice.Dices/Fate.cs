using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Fate : DiceBlueprint
{
	private static readonly Dictionary<ulong, string> _assignments = new Dictionary<ulong, string>();

	private static readonly Dictionary<ulong, float> _orbitDeathTime = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> _diceLuckNextRoll = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> _balanceNextTick = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> _compassNextReveal = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, List<(CDynamicProp?, CDynamicProp?)>> _compassActiveGlows = new Dictionary<ulong, List<(CDynamicProp, CDynamicProp)>>();

	private static readonly Dictionary<ulong, float> _webDeathTime = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, bool> _webTriggered = new Dictionary<ulong, bool>();

	private static readonly Dictionary<ulong, float> _darktideFreezeUntil = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, float> _dawnRespawnTime = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, bool> _dawnActivated = new Dictionary<ulong, bool>();

	private static readonly Dictionary<ulong, int> _dawnOriginalMaxHealth = new Dictionary<ulong, int>();

	private static bool _assignedThisRound;

	private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private static readonly string[] _fatePool = new string[6] { "orbit", "balance", "compass", "web", "darktide", "dawn" };

	public override string ClassName => "Fate";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public override List<string> Events
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "EventPlayerDeath";
			num2++;
			span[num2] = "EventWeaponFire";
			return list;
		}
	}

	public Fate(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		if (_assignedThisRound)
		{
			return;
		}
		_assignedThisRound = true;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			AssignFate(item);
		}
	}

	private void AssignFate(CCSPlayerController player)
	{
		ulong steamID = ((CBasePlayerController)player).SteamID;
		string text = _fatePool[_random.Next(_fatePool.Length)];
		_assignments[steamID] = text;
		CCSPlayerPawn value = player.PlayerPawn.Value;
		switch (text)
		{
		case "orbit":
			_orbitDeathTime[steamID] = Server.CurrentTime + _config.Dices.Fate.OrbitDeathTime;
			SpeedBonusManager.Register(player, "FateOrbit", 0.2f);
			player.PrintToCenterAlert($"\ud83c\udf20 命轨：无限子弹，移速+20%，{_config.Dices.Fate.OrbitDeathTime:F0}秒后死亡");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udf20 命运·命轨：{_config.Dices.Fate.OrbitDeathTime:F0}秒后死亡，无限子弹+移速+20%");
			break;
		case "dice_luck":
			_diceLuckNextRoll[steamID] = Server.CurrentTime + _config.Dices.Fate.DiceLuckInterval;
			player.PrintToCenterAlert($"\ud83c\udfb2 骰运：每{_config.Dices.Fate.DiceLuckInterval:F0}秒更换一次命运，抽中命轨立即死亡");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udfb2 命运·骰运：每{_config.Dices.Fate.DiceLuckInterval:F0}秒命运改写");
			break;
		case "balance":
			_balanceNextTick[steamID] = Server.CurrentTime + 1f;
			DamageBonusManager.Register(player, "FateBalance", _config.Dices.Fate.BalanceDamageBonus);
			player.PrintToCenterAlert("⚖ 天秤：每秒流失1HP，伤害+60%");
			player.PrintToChat(" " + _localizer["command.prefix"].Value + "⚖ 命运·天秤：每秒-1HP，伤害+60%");
			break;
		case "compass":
			_compassNextReveal[steamID] = Server.CurrentTime + _config.Dices.Fate.CompassInterval;
			player.PrintToCenterAlert($"\ud83e\udded 罗盘：每{_config.Dices.Fate.CompassInterval:F0}秒透视2名敌人{_config.Dices.Fate.CompassDuration:F0}秒，自身也会暴露发光");
			player.PrintToChat(" " + _localizer["command.prefix"].Value + "\ud83e\udded 命运·罗盘：定时透视敌人，自身也会暴露");
			break;
		case "web":
			_webTriggered[steamID] = false;
			player.PrintToCenterAlert($"\ud83d\udd78 织网：致命伤害时与最近敌人换位，无敌{_config.Dices.Fate.WebInvulDuration:F0}秒——之后死亡");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83d\udd78 命运·织网：致死时换位，无敌{_config.Dices.Fate.WebInvulDuration:F0}秒后死亡");
			break;
		case "darktide":
			SpeedBonusManager.Register(player, "FateDarktide", 0f - _config.Dices.Fate.DarktideSlowAmount);
			player.PrintToCenterAlert($"\ud83c\udf11 暗潮：死亡时冻结所有敌人{_config.Dices.Fate.DarktideFreezeDuration:F0}秒，移速-{(int)(_config.Dices.Fate.DarktideSlowAmount * 100f)}%");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udf11 命运·暗潮：死亡时冻结敌人{_config.Dices.Fate.DarktideFreezeDuration:F0}秒");
			break;
		case "dawn":
			_dawnActivated[steamID] = false;
			_dawnOriginalMaxHealth[steamID] = ((CBaseEntity)value).MaxHealth;
			((CBaseEntity)value).MaxHealth = Math.Max(1, ((CBaseEntity)value).MaxHealth - _config.Dices.Fate.DawnHpPenalty);
			((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, ((CBaseEntity)value).MaxHealth);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			player.PrintToCenterAlert($"\ud83c\udf05 黎明：死亡{_config.Dices.Fate.DawnFreezeDuration:F0}秒后满血满甲复活，开局HP-{_config.Dices.Fate.DawnHpPenalty}");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83c\udf05 命运·黎明：死后复活，HP-{_config.Dices.Fate.DawnHpPenalty}");
			break;
		}
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udd2e {((CBasePlayerController)player).PlayerName} 的命运——{FateDisplayName(text)}！");
	}

	private static string FateDisplayName(string fate)
	{
		if (1 == 0)
		{
		}
		string result = fate switch
		{
			"orbit" => "命轨", 
			"dice_luck" => "骰运", 
			"balance" => "天秤", 
			"compass" => "罗盘", 
			"web" => "织网", 
			"darktide" => "暗潮", 
			"dawn" => "黎明", 
			_ => fate, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid)
		{
			ulong steamID = ((CBasePlayerController)player).SteamID;
			if (_assignments.TryGetValue(steamID, out string value) && value == "dawn" && _dawnRespawnTime.ContainsKey(steamID))
			{
				if (_dawnActivated.GetValueOrDefault(steamID))
				{
					return;
				}
				_orbitDeathTime.Remove(steamID);
				_diceLuckNextRoll.Remove(steamID);
				_balanceNextTick.Remove(steamID);
				_compassNextReveal.Remove(steamID);
				ClearCompassGlows(steamID);
				_webDeathTime.Remove(steamID);
				_webTriggered.Remove(steamID);
				_darktideFreezeUntil.Remove(steamID);
			}
			else
			{
				CleanupFate(steamID);
			}
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid)
			{
				CleanupFate(((CBasePlayerController)item).SteamID);
			}
		}
		_players.Clear();
		_assignments.Clear();
		_orbitDeathTime.Clear();
		_diceLuckNextRoll.Clear();
		_balanceNextTick.Clear();
		_compassNextReveal.Clear();
		foreach (KeyValuePair<ulong, List<(CDynamicProp, CDynamicProp)>> item2 in _compassActiveGlows.ToList())
		{
			ClearCompassGlows(item2.Key);
		}
		_webDeathTime.Clear();
		_webTriggered.Clear();
		_darktideFreezeUntil.Clear();
		_dawnRespawnTime.Clear();
		_dawnActivated.Clear();
		_assignedThisRound = false;
	}

	private void CleanupFate(ulong sid)
	{
		CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CEntityInstance)x).IsValid && ((CBasePlayerController)x).SteamID == sid);
		if ((CEntityInstance)(object)val != (CEntityInstance)null)
		{
			SpeedBonusManager.Unregister(val, "FateOrbit");
			DamageBonusManager.Unregister(val, "FateBalance");
			SpeedBonusManager.Unregister(val, "FateDarktide");
			if (_dawnOriginalMaxHealth.TryGetValue(sid, out int origMax))
			{
				CCSPlayerPawn pawn = val.PlayerPawn?.Value;
				if ((CEntityInstance)(object)pawn != (CEntityInstance)null && ((CEntityInstance)pawn).IsValid && ((CBaseEntity)pawn).LifeState == 0)
				{
					((CBaseEntity)pawn).MaxHealth = origMax;
					if (((CBaseEntity)pawn).Health > origMax)
					{
						((CBaseEntity)pawn).Health = origMax;
					}
					Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iMaxHealth", 0);
					Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CBaseEntity", "m_iHealth", 0);
				}
				_dawnOriginalMaxHealth.Remove(sid);
			}
		}
		_assignments.Remove(sid);
		_orbitDeathTime.Remove(sid);
		_diceLuckNextRoll.Remove(sid);
		_balanceNextTick.Remove(sid);
		_compassNextReveal.Remove(sid);
		ClearCompassGlows(sid);
		_webDeathTime.Remove(sid);
		_webTriggered.Remove(sid);
		_darktideFreezeUntil.Remove(sid);
		_dawnRespawnTime.Remove(sid);
		_dawnActivated.Remove(sid);
		_dawnOriginalMaxHealth.Remove(sid);
	}

	private static void ClearCompassGlows(ulong sid)
	{
		if (!_compassActiveGlows.TryGetValue(sid, out List<(CDynamicProp, CDynamicProp)> value))
		{
			return;
		}
		foreach (var item in value)
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)item.Item1, (CBaseEntity?)(object)item.Item2);
		}
		_compassActiveGlows.Remove(sid);
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RerollFate(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		ulong steamID = ((CBasePlayerController)player).SteamID;
		string text = (_assignments.TryGetValue(steamID, out string value) ? value : "");
		SpeedBonusManager.Unregister(player, "FateOrbit");
		DamageBonusManager.Unregister(player, "FateBalance");
		SpeedBonusManager.Unregister(player, "FateDarktide");
		_orbitDeathTime.Remove(steamID);
		_balanceNextTick.Remove(steamID);
		_compassNextReveal.Remove(steamID);
		ClearCompassGlows(steamID);
		_webTriggered.Remove(steamID);
		_webDeathTime.Remove(steamID);
		string text2 = _fatePool[_random.Next(_fatePool.Length)];
		_assignments[steamID] = text2;
		CCSPlayerPawn val = player.PlayerPawn?.Value;
		float num = Server.CurrentTime;
		switch (text2)
		{
		case "orbit":
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——命轨降临！");
			player.PrintToChat($" {_localizer["command.prefix"].Value}\ud83d\udc80 {((CBasePlayerController)player).PlayerName} 骰运翻转——命轨！");
			if (!((CEntityInstance)(object)val != (CEntityInstance)null) || !((CEntityInstance)val).IsValid || ((CBaseEntity)val).LifeState != 0)
			{
				return;
			}
			if (!player.IsBot && !((CBasePlayerController)player).IsHLTV)
			{
				((CBasePlayerPawn)val).CommitSuicide(false, true);
				return;
			}
			try
			{
				((CBasePlayerPawn)val).CommitSuicide(false, true);
				return;
			}
			catch
			{
				((CBaseEntity)val).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iHealth", 0);
				return;
			}
		case "balance":
			_balanceNextTick[steamID] = num + 1f;
			DamageBonusManager.Register(player, "FateBalance", _config.Dices.Fate.BalanceDamageBonus);
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——天秤！每秒-1HP，伤害+60%");
			break;
		case "compass":
			_compassNextReveal[steamID] = num + _config.Dices.Fate.CompassInterval;
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——罗盘！透视敌人，自身也会暴露");
			break;
		case "web":
			_webTriggered[steamID] = false;
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——织网！致死时换位，无敌3秒后死亡");
			break;
		case "darktide":
			SpeedBonusManager.Register(player, "FateDarktide", 0f - _config.Dices.Fate.DarktideSlowAmount);
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——暗潮！死亡时冻结所有敌人");
			break;
		case "dawn":
		{
			if (_dawnActivated.TryGetValue(steamID, out var value2) & value2)
			{
				player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——黎明！（已用过复活，本次无效）");
				break;
			}
			if (text != "dawn" && (CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid)
			{
				_dawnActivated[steamID] = false;
				((CBaseEntity)val).MaxHealth = Math.Max(1, ((CBaseEntity)val).MaxHealth - _config.Dices.Fate.DawnHpPenalty);
				((CBaseEntity)val).Health = Math.Min(((CBaseEntity)val).Health, ((CBaseEntity)val).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iHealth", 0);
			}
			player.PrintToCenterAlert("\ud83c\udfb2 骰运翻转——黎明！死后2秒满血满甲复活");
			break;
		}
		}
		if (text2 == "dice_luck")
		{
			_diceLuckNextRoll[steamID] = num + _config.Dices.Fate.DiceLuckInterval;
		}
	}

	public void OnTick()
	{
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		float num = Server.CurrentTime;
		foreach (KeyValuePair<ulong, string> item in _assignments.ToList())
		{
			ulong sid = item.Key;
			string value = item.Value;
			if (value != "dawn")
			{
				continue;
			}
			CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == sid);
			CCSPlayerController obj = player;
			if ((CEntityInstance)(object)((obj == null) ? null : obj.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
			{
				continue;
			}
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			if (!_dawnRespawnTime.TryGetValue(sid, out var value3) || !(num >= value3))
			{
				continue;
			}
			_dawnRespawnTime.Remove(sid);
			if (((CBaseEntity)value2).LifeState == 0)
			{
				continue;
			}
			player.Respawn();
			Server.NextFrame((Action)delegate
			{
				Server.NextFrame((Action)delegate
				{
					CCSPlayerController obj6 = player;
					CCSPlayerPawn val5 = ((obj6 == null) ? null : obj6.PlayerPawn?.Value);
					if (val5 != null && ((CEntityInstance)val5).IsValid && ((CBaseEntity)val5).LifeState == 0)
					{
						((CBaseEntity)val5).MaxHealth = Math.Max(((CBaseEntity)val5).MaxHealth, 100);
						((CBaseEntity)val5).Health = ((CBaseEntity)val5).MaxHealth;
						val5.ArmorValue = 100;
						Utilities.SetStateChanged((CBaseEntity)(object)val5, "CBaseEntity", "m_iMaxHealth", 0);
						Utilities.SetStateChanged((CBaseEntity)(object)val5, "CBaseEntity", "m_iHealth", 0);
						Utilities.SetStateChanged((CBaseEntity)(object)val5, "CCSPlayerPawn", "m_ArmorValue", 0);
						((CBaseEntity)val5).MoveType = (MoveType_t)2;
						Schema.SetSchemaValue<int>(((NativeEntity)val5).Handle, "CBaseEntity", "m_nActualMoveType", 2);
						((CBaseModelEntity)val5).Render = Color.FromArgb(255, 255, 255, 255);
						Utilities.SetStateChanged((CBaseEntity)(object)val5, "CBaseModelEntity", "m_clrRender", 0);
						((CBaseEntity)val5).TakesDamage = true;
						player.PrintToCenterAlert("\ud83c\udf05 黎明降临！你已重生！");
						Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf05 {((CBasePlayerController)player).PlayerName} 的命运·黎明降临！浴火重生！");
					}
				});
			});
		}
		foreach (KeyValuePair<ulong, string> item2 in _assignments.ToList())
		{
			ulong sid2 = item2.Key;
			string value4 = item2.Value;
			CCSPlayerController player2 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == sid2);
			CCSPlayerController obj2 = player2;
			if ((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)player2.PlayerPawn.Value).IsValid)
			{
				continue;
			}
			CCSPlayerPawn value5 = player2.PlayerPawn.Value;
			if (((CBaseEntity)value5).LifeState != 0)
			{
				continue;
			}
			switch (value4)
			{
			case "orbit":
			{
				value5.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player2, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)value5, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				if (!_orbitDeathTime.TryGetValue(sid2, out var value9) || !(num >= value9))
				{
					break;
				}
				if (!player2.IsBot && !((CBasePlayerController)player2).IsHLTV)
				{
					((CBasePlayerPawn)value5).CommitSuicide(false, true);
				}
				else
				{
					try
					{
						((CBasePlayerPawn)value5).CommitSuicide(false, true);
					}
					catch
					{
						((CBaseEntity)value5).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseEntity", "m_iHealth", 0);
					}
				}
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf20 {((CBasePlayerController)player2).PlayerName} 命轨应验——一切早已注定。");
				break;
			}
			case "dice_luck":
			{
				if (_diceLuckNextRoll.TryGetValue(sid2, out var value8) && num >= value8)
				{
					_diceLuckNextRoll[sid2] = num + _config.Dices.Fate.DiceLuckInterval;
					RerollFate(player2);
				}
				break;
			}
			case "balance":
			{
				if (!_balanceNextTick.TryGetValue(sid2, out var value7) || !(num >= value7))
				{
					break;
				}
				_balanceNextTick[sid2] = num + 1f;
				((CBaseEntity)value5).Health -= _config.Dices.Fate.BalanceHpLoss;
				Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseEntity", "m_iHealth", 0);
				if (((CBaseEntity)value5).Health > 0)
				{
					break;
				}
				if (!player2.IsBot && !((CBasePlayerController)player2).IsHLTV)
				{
					((CBasePlayerPawn)value5).CommitSuicide(false, true);
					break;
				}
				try
				{
					((CBasePlayerPawn)value5).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)value5).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseEntity", "m_iHealth", 0);
				}
				break;
			}
			case "compass":
			{
				if (!_compassNextReveal.TryGetValue(sid2, out var value6) || !(num >= value6))
				{
					break;
				}
				_compassNextReveal[sid2] = num + _config.Dices.Fate.CompassInterval;
				float compassDuration = _config.Dices.Fate.CompassDuration;
				ClearCompassGlows(sid2);
				List<CCSPlayerController> list = (from _ in Utilities.GetPlayers()
					where ((CEntityInstance)_).IsValid && !((CBasePlayerController)_).IsHLTV && (CEntityInstance)(object)_ != (CEntityInstance)(object)player2 && ((CBaseEntity)_).TeamNum != ((CBaseEntity)player2).TeamNum && (CEntityInstance)(object)_.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)_.PlayerPawn.Value).IsValid && ((CBaseEntity)_.PlayerPawn.Value).LifeState == 0
					orderby _random.Next()
					select _).Take(2).ToList();
				List<(CDynamicProp, CDynamicProp)> list2 = new List<(CDynamicProp, CDynamicProp)>();
				foreach (CCSPlayerController item3 in list)
				{
					list2.Add(GlowUtil.CreateGlow((CBaseEntity)(object)item3.PlayerPawn.Value, Color.Cyan));
				}
				list2.Add(GlowUtil.CreateGlow((CBaseEntity)(object)value5, Color.Yellow));
				_compassActiveGlows[sid2] = list2;
				ulong capSid = sid2;
				new Timer(compassDuration, (Action)delegate
				{
					ClearCompassGlows(capSid);
				}, (TimerFlags?)null);
				player2.PrintToCenterAlert($"\ud83e\udded 罗盘揭示了{list.Count}名敌人的位置！{compassDuration:F0}秒");
				break;
			}
			case "darktide":
			{
				float effective = SpeedBonusManager.GetEffective(player2, 0f - _config.Dices.Fate.DarktideSlowAmount);
				value5.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)value5, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				break;
			}
			}
		}
		foreach (KeyValuePair<ulong, float> item4 in _webDeathTime.ToList())
		{
			if (!(num >= item4.Value))
			{
				continue;
			}
			ulong sid3 = item4.Key;
			_webDeathTime.Remove(sid3);
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CEntityInstance)x).IsValid && ((CBasePlayerController)x).SteamID == sid3);
			CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
			if (val2 == null || !((CEntityInstance)val2).IsValid || ((CBaseEntity)val2).LifeState != 0)
			{
				continue;
			}
			((CBaseEntity)val2).TakesDamage = true;
			MoveLockManager.Unlock(val, "FateWeb");
			if (!val.IsBot && !((CBasePlayerController)val).IsHLTV)
			{
				((CBasePlayerPawn)val2).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)val2).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)val2).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
			}
		}
		foreach (KeyValuePair<ulong, float> item5 in _darktideFreezeUntil.ToList())
		{
			if (num >= item5.Value)
			{
				ulong sid4 = item5.Key;
				_darktideFreezeUntil.Remove(sid4);
				CCSPlayerController val3 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CEntityInstance)x).IsValid && ((CBasePlayerController)x).SteamID == sid4);
				CCSPlayerPawn val4 = ((val3 == null) ? null : val3.PlayerPawn?.Value);
				if (val4 != null && ((CEntityInstance)val4).IsValid)
				{
					MoveLockManager.Unlock(val3, "FateDarktide");
					val3.PrintToCenterAlert("\ud83c\udf11 暗潮退去，你恢复了移动！");
				}
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController victim = (CCSPlayerController)obj2;
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj3;
		if (attacker == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value2 = attacker.Value;
			if (value2 == null)
			{
				obj3 = null;
			}
			else
			{
				CCSPlayerPawn obj4 = ((NativeObject)value2).As<CCSPlayerPawn>();
				if (obj4 == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerController value3 = controller2.Value;
						obj3 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj3;
		bool flag = false;
		if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && _assignments.TryGetValue(((CBasePlayerController)val).SteamID, out string value4) && value4 == "balance")
		{
			float effective = DamageBonusManager.GetEffective(val, _config.Dices.Fate.BalanceDamageBonus);
			info.Damage = (int)(info.Damage * (1f + effective));
			flag = true;
		}
		if ((CEntityInstance)(object)victim != (CEntityInstance)null && ((CEntityInstance)victim).IsValid && _assignments.TryGetValue(((CBasePlayerController)victim).SteamID, out string value5) && value5 == "web" && !_webTriggered.GetValueOrDefault(((CBasePlayerController)victim).SteamID))
		{
			int num = (int)((float)entity.Health - info.Damage);
			if (num <= 0)
			{
				info.Damage = 0f;
				_webTriggered[((CBasePlayerController)victim).SteamID] = true;
				ulong vSid = ((CBasePlayerController)victim).SteamID;
				CCSPlayerController val2 = null;
				float num2 = float.MaxValue;
				CHandle<CCSPlayerPawn> playerPawn = victim.PlayerPawn;
				object obj5;
				if (playerPawn == null)
				{
					obj5 = null;
				}
				else
				{
					CCSPlayerPawn value6 = playerPawn.Value;
					obj5 = ((value6 != null) ? ((CBaseEntity)value6).AbsOrigin : null);
				}
				Vector val3 = (Vector)obj5;
				if (val3 == null)
				{
					return (HookResult)1;
				}
				foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)victim && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
					select p)
				{
					float num3 = val3.X - ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin.X;
					float num4 = val3.Y - ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin.Y;
					float num5 = MathF.Sqrt(num3 * num3 + num4 * num4);
					if (num5 < num2)
					{
						num2 = num5;
						val2 = item;
					}
				}
				CCSPlayerController capV = victim;
				CCSPlayerController capE = val2;
				Server.NextFrame((Action)delegate
				{
					//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
					//IL_00aa: Expected O, but got Unknown
					//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
					//IL_00e4: Expected O, but got Unknown
					//IL_0105: Unknown result type (might be due to invalid IL or missing references)
					//IL_0128: Unknown result type (might be due to invalid IL or missing references)
					//IL_0132: Expected O, but got Unknown
					//IL_0132: Expected O, but got Unknown
					//IL_0153: Unknown result type (might be due to invalid IL or missing references)
					//IL_0176: Unknown result type (might be due to invalid IL or missing references)
					//IL_0180: Expected O, but got Unknown
					//IL_0180: Expected O, but got Unknown
					CCSPlayerController obj6 = capV;
					CCSPlayerPawn val4 = ((obj6 == null) ? null : obj6.PlayerPawn?.Value);
					if (val4 != null && ((CEntityInstance)val4).IsValid)
					{
						CCSPlayerController obj7 = capE;
						CCSPlayerPawn val5 = ((obj7 == null) ? null : obj7.PlayerPawn?.Value);
						if (val5 != null && ((CEntityInstance)val5).IsValid && ((CBaseEntity)val4).AbsOrigin != null && ((CBaseEntity)val5).AbsOrigin != null)
						{
							Vector val6 = new Vector((float?)((CBaseEntity)val4).AbsOrigin.X, (float?)((CBaseEntity)val4).AbsOrigin.Y, (float?)((CBaseEntity)val4).AbsOrigin.Z);
							Vector val7 = new Vector((float?)((CBaseEntity)val5).AbsOrigin.X, (float?)((CBaseEntity)val5).AbsOrigin.Y, (float?)((CBaseEntity)val5).AbsOrigin.Z);
							((CBaseEntity)val4).Teleport(val7, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
							((CBaseEntity)val5).Teleport(val6, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
							float webInvulDuration = _config.Dices.Fate.WebInvulDuration;
							_webDeathTime[vSid] = Server.CurrentTime + webInvulDuration;
							MoveLockManager.Lock(capV, "FateWeb");
							((CBaseEntity)val4).TakesDamage = false;
							capV.PrintToCenterAlert($"\ud83d\udd78 织网触发！与 {((CBasePlayerController)capE).PlayerName} 换位，无敌{webInvulDuration:F0}s...");
							Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udd78 {((CBasePlayerController)capV).PlayerName} 的命运·织网触发！与 {((CBasePlayerController)capE).PlayerName} 换位！");
						}
					}
				});
				return (HookResult)1;
			}
		}
		return (HookResult)(flag ? 1 : 0);
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		ulong sid = ((CBasePlayerController)victim).SteamID;
		if (!_assignments.TryGetValue(sid, out string value))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		string text = value;
		string text2 = text;
		if (!(text2 == "darktide"))
		{
			if (text2 == "dawn")
			{
				if (_dawnActivated.TryGetValue(sid, out var value2) & value2)
				{
					return (HookResult)0;
				}
				_dawnActivated[sid] = true;
				float freezeDur = _config.Dices.Fate.DawnFreezeDuration;
				_dawnRespawnTime[sid] = num + freezeDur;
				CCSPlayerController capV = victim;
				Server.NextFrame((Action)delegate
				{
					CCSPlayerController obj = capV;
					CCSPlayerPawn val = ((obj == null) ? null : obj.PlayerPawn?.Value);
					if (val != null && ((CEntityInstance)val).IsValid)
					{
						if (((CBaseEntity)val).LifeState == 0)
						{
							_dawnRespawnTime.Remove(sid);
							_dawnActivated[sid] = false;
						}
						else
						{
							((CBaseEntity)val).MoveType = (MoveType_t)0;
							Schema.SetSchemaValue<int>(((NativeEntity)val).Handle, "CBaseEntity", "m_nActualMoveType", 0);
							((CBaseModelEntity)val).Render = Color.FromArgb(255, 255, 215, 0);
							Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseModelEntity", "m_clrRender", 0);
							((CBaseEntity)val).TakesDamage = false;
							capV.PrintToCenterAlert($"\ud83c\udf05 黎明将至...{freezeDur:F0}s后重生！");
							Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf05 {((CBasePlayerController)capV).PlayerName} 的命运·黎明触发！{freezeDur:F0}s后重生！");
						}
					}
				});
			}
		}
		else
		{
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p).TeamNum != ((CBaseEntity)victim).TeamNum
				select p)
			{
				float darktideFreezeDuration = _config.Dices.Fate.DarktideFreezeDuration;
				_darktideFreezeUntil[((CBasePlayerController)item).SteamID] = num + darktideFreezeDuration;
				MoveLockManager.Lock(item, "FateDarktide");
				item.PrintToCenterAlert($"\ud83c\udf11 暗潮降临！无法移动{darktideFreezeDuration:F0}s！");
			}
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf11 {((CBasePlayerController)victim).PlayerName} 的命运·暗潮！敌人全被定身！");
		}
		return (HookResult)0;
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (!_assignments.TryGetValue(((CBasePlayerController)userid).SteamID, out string value) || value != "orbit")
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value2 = playerPawn.Value;
			if (value2 == null)
			{
				obj = null;
			}
			else
			{
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value2).WeaponServices;
				obj = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
			}
		}
		CBasePlayerWeapon val = (CBasePlayerWeapon)obj;
		if (val != null && ((CEntityInstance)val).IsValid && val.VData != null)
		{
			string designerName = ((CEntityInstance)val).DesignerName;
			if (!designerName.Contains("knife") && !designerName.Contains("bayonet") && !designerName.Contains("hegrenade") && !designerName.Contains("flashbang") && !designerName.Contains("smokegrenade") && !designerName.Contains("molotov") && !designerName.Contains("decoy") && !designerName.Contains("c4") && !designerName.Contains("taser"))
			{
				val.Clip1++;
				val.ReserveAmmo[0] = Math.Max(val.ReserveAmmo[0], 30);
			}
		}
		return (HookResult)0;
	}
}
