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

public class FireDragon : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalArmor = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<ulong, float> _burnEndTime = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, int> _burnDps = new Dictionary<ulong, int>();

	public override string ClassName => "FireDragon";


	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerHurt";
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

	public override List<string> Precache => new List<string>();

	public FireDragon(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			_originalArmor[player] = value.ArmorValue;
			((CBaseEntity)value).MaxHealth = _config.Dices.FireDragon.BonusHP;
			((CBaseEntity)value).Health = _config.Dices.FireDragon.BonusHP;
			value.ArmorValue = _config.Dices.FireDragon.BonusArmor;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
			if (_originalArmor.TryGetValue(player, out var value3))
			{
				value.ArmorValue = Math.Min(value.ArmorValue, value3);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				_originalArmor.Remove(player);
			}
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
		_originalArmor.Clear();
		_burnEndTime.Clear();
		_burnDps.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)((CBasePlayerController)userid).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)userid).Pawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float val = Server.CurrentTime;
		float burnDuration = _config.Dices.FireDragon.BurnDuration;
		int burnDamagePerSec = _config.Dices.FireDragon.BurnDamagePerSec;
		float valueOrDefault = _burnEndTime.GetValueOrDefault(((CBasePlayerController)userid).SteamID, 0f);
		float value = Math.Max(val, valueOrDefault) + burnDuration;
		_burnEndTime[((CBasePlayerController)userid).SteamID] = value;
		int valueOrDefault2 = _burnDps.GetValueOrDefault(((CBasePlayerController)userid).SteamID, 0);
		_burnDps[((CBasePlayerController)userid).SteamID] = valueOrDefault2 + burnDamagePerSec;
		userid.PrintToCenterAlert($"\ud83d\udd25 被火巨龙灼烧！-{_burnDps[((CBasePlayerController)userid).SteamID]}HP/s！");
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_burnEndTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<ulong, float> item in _burnEndTime.ToList())
		{
			var (steamId, num4) = item;
			if (num >= num4)
			{
				_burnEndTime.Remove(steamId);
				_burnDps.Remove(steamId);
				continue;
			}
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == steamId);
			CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
			if (val2 == null || !((CEntityInstance)val2).IsValid || ((CBaseEntity)val2).LifeState != 0 || !_burnEndTime.TryGetValue(steamId, out var value) || !(num < value))
			{
				continue;
			}
			int valueOrDefault = _burnDps.GetValueOrDefault(steamId, 20);
			if (Server.TickCount % 64 != 0)
			{
				continue;
			}
			// 灼烧是直扣血，必须显式尊重无敌窗口（如亚戈鲁/菲尼克斯/愚者）。
			if (val != null && val.IsValid && Invulnerability.IsInvulnerable(val))
			{
				continue;
			}
			((CBaseEntity)val2).Health -= valueOrDefault;
			Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)val2).Health > 0)
			{
				continue;
			}
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
	}
}
