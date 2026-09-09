using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class RadarJammer : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, Timer?> _restoreTimers = new Dictionary<CCSPlayerController, Timer>();

	public override string ClassName => "RadarJammer";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public RadarJammer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_cooldowns[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udce1 雷达干扰！击杀后黑掉敌人雷达20秒！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		RestoreRadarForPlayer(player);
		_players.Remove(player);
		_cooldowns.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RestoreRadarForPlayer(item);
		}
		_players.Clear();
		_cooldowns.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RestoreRadarForPlayer(CCSPlayerController jammer)
	{
		if (_restoreTimers.TryGetValue(jammer, out Timer value))
		{
			if (value != null)
			{
				value.Kill();
			}
			_restoreTimers.Remove(jammer);
		}
		if ((CEntityInstance)(object)jammer == (CEntityInstance)null || !((CEntityInstance)jammer).IsValid)
		{
			return;
		}
		IEnumerable<CCSPlayerController> enumerable = from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)jammer).TeamNum
			select p;
		foreach (CCSPlayerController item in enumerable)
		{
			item.ReplicateConVar("sv_disable_radar", "0");
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Expected O, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid || ((CBaseEntity)userid).TeamNum == ((CBaseEntity)attacker).TeamNum || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (_cooldowns.TryGetValue(attacker, out var value) && num < value)
		{
			return (HookResult)0;
		}
		float blackoutDuration = _config.Dices.RadarJammer.BlackoutDuration;
		float cooldown = _config.Dices.RadarJammer.Cooldown;
		_cooldowns[attacker] = num + cooldown;
		IEnumerable<CCSPlayerController> enumerable = from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)attacker).TeamNum
			select p;
		foreach (CCSPlayerController item in enumerable)
		{
			item.ReplicateConVar("sv_disable_radar", "1");
			item.PrintToCenterAlert("\ud83d\udce1 雷达被黑！20秒");
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + ((CBasePlayerController)attacker).PlayerName + " 击杀了敌人，敌方雷达全部被黑！");
		CCSPlayerController captured = attacker;
		if (_restoreTimers.TryGetValue(attacker, out Timer oldTimer) && oldTimer != null)
		{
			oldTimer.Kill();
		}
		Timer value2 = new Timer(blackoutDuration, (Action)delegate
		{
			IEnumerable<CCSPlayerController> enumerable2 = from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)captured).TeamNum
				select p;
			foreach (CCSPlayerController item2 in enumerable2)
			{
				item2.ReplicateConVar("sv_disable_radar", "0");
			}
			_restoreTimers.Remove(captured);
		}, (TimerFlags?)null);
		_restoreTimers[attacker] = value2;
		return (HookResult)0;
	}
}
