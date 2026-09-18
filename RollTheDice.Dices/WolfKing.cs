using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class WolfKing : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "WolfKing";

	public override bool IsSpecial => true;

	public override float SecondRoundProbability => 0.9f;

	public override string? SecondRoundRewardId => "Wolf";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			return list;
		}
	}

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public WolfKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			((CBaseEntity)value).MaxHealth = _config.Dices.WolfKing.HP;
			((CBaseEntity)value).Health = _config.Dices.WolfKing.HP;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			DamageBonusManager.Register(player, "WolfKing", _config.Dices.WolfKing.DamageBonus);
			SpeedBonusManager.Register(player, "WolfKing", _config.Dices.WolfKing.SpeedBonus);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc3a {((CBasePlayerController)player).PlayerName} 成为狼王！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageBonusManager.Unregister(player, "WolfKing");
		SpeedBonusManager.Unregister(player, "WolfKing");
		RestoreMaxHealth(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "WolfKing");
			SpeedBonusManager.Unregister(item, "WolfKing");
			RestoreMaxHealth(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RestoreMaxHealth(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || !_originalMaxHealth.TryGetValue(player, out int original))
		{
			_originalMaxHealth.Remove(player);
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			((CBaseEntity)pawn).MaxHealth = original;
			if (((CBaseEntity)pawn).Health > original)
			{
				((CBaseEntity)pawn).Health = original;
			}
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
		}
		_originalMaxHealth.Remove(player);
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid || attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		int heal = _config.Dices.WolfKing.KillHeal;
		if (heal <= 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn pawn = attacker.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.Health = Math.Min(pawn.Health + heal, pawn.MaxHealth);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			attacker.PrintToCenterAlert($"\ud83d\udc3a 狼王吞噬！+{heal}HP");
		}
		return HookResult.Continue;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				float effective = SpeedBonusManager.GetEffective(item, _config.Dices.WolfKing.SpeedBonus);
				item.PlayerPawn.Value.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}
}
