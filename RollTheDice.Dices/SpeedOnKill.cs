using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

	public class SpeedOnKill : DiceBlueprint
	{
		public static SpeedOnKill? Instance;

		private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, Timer> _activeBoosts = new Dictionary<CCSPlayerController, Timer>();

	private static readonly string[] _grenadePool = new string[5] { "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_decoy" };

	public override string ClassName => "SpeedOnKill";

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

	public SpeedOnKill(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Instance = this;
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public void TriggerBoost(CCSPlayerController attacker)
	{
		if (attacker == null || !attacker.IsValid || attacker.PlayerPawn?.Value == null || !attacker.PlayerPawn.Value.IsValid)
		{
			return;
		}
		float multiplier = _config.Dices.SpeedOnKill.SpeedMultiplierMin + (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.SpeedMultiplierMax - _config.Dices.SpeedOnKill.SpeedMultiplierMin);
		float duration = _config.Dices.SpeedOnKill.DurationMin + (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.DurationMax - _config.Dices.SpeedOnKill.DurationMin);
		CCSPlayerPawn pawn = attacker.PlayerPawn.Value;
		SpeedBonusManager.Register(attacker, "SpeedOnKill", multiplier - 1f);
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		attacker.PrintToCenterAlert($"\u2694 \u8fde\u51fb\u8282\u594f {multiplier * 100f:F0}% {duration:F0}\u79d2!");
		if (_activeBoosts.Remove(attacker, out Timer previous) && previous != null)
		{
			previous.Kill();
		}
		_activeBoosts[attacker] = new Timer(duration, (Action)delegate
		{
			SpeedBonusManager.Unregister(attacker, "SpeedOnKill");
			CCSPlayerPawn value = attacker?.PlayerPawn?.Value;
			if (value != null && ((CEntityInstance)value).IsValid)
			{
				value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
				Utilities.SetStateChanged(value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
			_activeBoosts.Remove(attacker);
		}, (TimerFlags?)null);
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "FrontlineBeast");
		if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "猎杀本能", "猎杀时限翻倍");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		SpeedBonusManager.Unregister(player, "SpeedOnKill");
		if (_activeBoosts.Remove(player, out Timer value) && value != null)
		{
			value.Kill();
		}
		CCSPlayerPawn val = player.PlayerPawn?.Value;
		if (val != null && ((CEntityInstance)val).IsValid)
		{
			val.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)val, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public override void Reset()
	{
		foreach (var (val3, val4) in _activeBoosts.ToList())
		{
			if (val4 != null)
			{
				val4.Kill();
			}
			if (val3 != null)
			{
				SpeedBonusManager.Unregister(val3, "SpeedOnKill");
			}
			CCSPlayerPawn val5 = ((val3 == null) ? null : val3.PlayerPawn?.Value);
			if (val5 != null && ((CEntityInstance)val5).IsValid)
			{
				val5.VelocityModifier = 1f + SpeedBonusManager.GetEffective(val3, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)val5, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		_players.Clear();
		_activeBoosts.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Expected O, but got Unknown
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid || !_players.Contains(attacker) || (CEntityInstance)(object)attacker.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)attacker.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float num = _config.Dices.SpeedOnKill.SpeedMultiplierMin + (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.SpeedMultiplierMax - _config.Dices.SpeedOnKill.SpeedMultiplierMin);
		float num2 = _config.Dices.SpeedOnKill.DurationMin + (float)_random.NextDouble() * (_config.Dices.SpeedOnKill.DurationMax - _config.Dices.SpeedOnKill.DurationMin);
		if (DiceSynergy.HasPartner(attacker, "FrontlineBeast"))
		{
			num2 *= 2f;
		}
		CCSPlayerPawn value = attacker.PlayerPawn.Value;
		SpeedBonusManager.Register(attacker, "SpeedOnKill", num - 1f);
		value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		attacker.PrintToCenterAlert($"⚡ 击杀加速 {(num - 1f) * 100f:F0}% {num2:F0}秒!");
		if (_activeBoosts.Remove(attacker, out Timer value2) && value2 != null)
		{
			value2.Kill();
		}
		_activeBoosts[attacker] = new Timer(num2, (Action)delegate
		{
			CCSPlayerController obj = attacker;
			SpeedBonusManager.Unregister(obj, "SpeedOnKill");
			CCSPlayerPawn val = ((obj == null) ? null : obj.PlayerPawn?.Value);
			if (val != null && ((CEntityInstance)val).IsValid)
			{
				val.VelocityModifier = 1f + SpeedBonusManager.GetEffective(obj, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
			_activeBoosts.Remove(attacker);
		}, (TimerFlags?)null);
		string text = _grenadePool[_random.Next(_grenadePool.Length)];
		attacker.GiveNamedItem(text);
		string text2 = text;
		int length = "weapon_".Length;
		string text3 = text2.Substring(length, text2.Length - length);
		attacker.PrintToCenterAlert("\ud83d\udca3 +1 " + text3 + "!");
		return (HookResult)0;
	}
}
