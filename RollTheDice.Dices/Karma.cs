using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Karma : DiceBlueprint
{
	private bool _comboActive;

	public static readonly HashSet<ulong> BuffedPlayers = new HashSet<ulong>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private float _lastHealTime;

	public override string ClassName => "Karma";

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

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			return list;
		}
	}

	public Karma(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Plague");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "因果循环", "双倍回血 双倍感染");
			}
			BuffedPlayers.Add(((CBasePlayerController)player).SteamID);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			SpeedBonusManager.Register(player, "Karma", _config.Dices.Karma.SpeedMultiplier - 1f);
			value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("☸ 因果报应！移速×1.5 + 每秒回复1HP！杀敌传播！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		SpeedBonusManager.Unregister(player, "Karma");
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid)
		{
			ulong steamID = ((CBasePlayerController)player).SteamID;
			BuffedPlayers.Remove(steamID);
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if ((CEntityInstance)(object)pawn != (CEntityInstance)null && ((CEntityInstance)pawn).IsValid && ((CBaseEntity)pawn).LifeState == 0)
			{
				pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public override void Reset()
	{
		foreach (ulong steamId in BuffedPlayers.ToList())
		{
			SpeedBonusManager.UnregisterBySteamId(steamId, "Karma");
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == steamId);
			if ((CEntityInstance)(object)val != (CEntityInstance)null)
			{
				CCSPlayerPawn pawn = val.PlayerPawn?.Value;
				if ((CEntityInstance)(object)pawn != (CEntityInstance)null && ((CEntityInstance)pawn).IsValid && ((CBaseEntity)pawn).LifeState == 0)
				{
					pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(val, 100f);
					Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
		_players.Clear();
		BuffedPlayers.Clear();
		_lastHealTime = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid)
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)userid).TeamNum == ((CBaseEntity)attacker).TeamNum)
		{
			return (HookResult)0;
		}
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)attacker).TeamNum && (CEntityInstance)(object)p != (CEntityInstance)(object)attacker && !BuffedPlayers.Contains(((CBasePlayerController)p).SteamID) && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p).ToList();
		if (list.Count == 0)
		{
			return (HookResult)0;
		}
		CCSPlayerController val = list[_random.Next(list.Count)];
		BuffedPlayers.Add(((CBasePlayerController)val).SteamID);
		CCSPlayerPawn value = val.PlayerPawn.Value;
		SpeedBonusManager.Register(val, "Karma", _config.Dices.Karma.SpeedMultiplier - 1f);
		value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(val, 100f);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		val.PrintToCenterAlert("☸ 因果报应！移速×1.5 + 每秒回复1HP！");
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Karma_spread"].Value.Replace("{attacker}", ((CBasePlayerController)attacker).PlayerName).Replace("{target}", ((CBasePlayerController)val).PlayerName));
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (BuffedPlayers.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num - _lastHealTime >= 1f)
		{
			_lastHealTime = num;
			int hpPerSecond = _config.Dices.Karma.HpPerSecond;
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && BuffedPlayers.Contains(((CBasePlayerController)p).SteamID) && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p)
			{
				CCSPlayerPawn value = item.PlayerPawn.Value;
				if (((CBaseEntity)value).Health < ((CBaseEntity)value).MaxHealth)
				{
					((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health + hpPerSecond, ((CBaseEntity)value).MaxHealth);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				}
			}
			foreach (ulong steamId in BuffedPlayers.Where(delegate(ulong steamId)
			{
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController pl) => ((CBasePlayerController)pl).SteamID == steamId);
				return (CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || (CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid || ((CBaseEntity)val.PlayerPawn.Value).LifeState != 0;
			}).ToList())
			{
				BuffedPlayers.Remove(steamId);
				SpeedBonusManager.UnregisterBySteamId(steamId, "Karma");
			}
		}
		foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && BuffedPlayers.Contains(((CBasePlayerController)p).SteamID) && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			CCSPlayerPawn value2 = item2.PlayerPawn.Value;
			float expected = 1f + SpeedBonusManager.GetEffective(item2, 100f);
			if (value2.VelocityModifier != expected)
			{
				value2.VelocityModifier = expected;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}
}
