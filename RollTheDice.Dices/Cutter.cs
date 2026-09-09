using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Cutter : DiceBlueprint
{
	private bool _comboActive;

	private readonly HashSet<string> _knifes;

	public override string ClassName => "Cutter";

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

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "SwordSaint");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "剑刃风暴", "刺客信条+剑仙！刀剑合璧，移速×1.5！");
			}
			SpeedBonusManager.Register(player, "Cutter", _config.Dices.Cutter.SpeedMultiplier - 1f);
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
		SpeedBonusManager.Unregister(player, "Cutter");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			SpeedBonusManager.Unregister(item, "Cutter");
			_players.Remove(item);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				float effective = SpeedBonusManager.GetEffective(item);
				float num = 1f + effective;
				if (DiceSynergy.HasPartner(item, "SwordSaint"))
				{
					num *= 1.5f;
				}
				item.PlayerPawn.Value.VelocityModifier = num;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public Cutter(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		_knifes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"knife",
			((CsItem)500).ToString(),
			((CsItem)501).ToString(),
			((CsItem)500).ToString(),
			((CsItem)501).ToString(),
			((CsItem)500).ToString()
		};
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (userid == null || !((CEntityInstance)userid).IsValid || attacker == null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)((CBasePlayerController)userid).Pawn.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		if (_knifes.Any((string item) => @event.Weapon.Contains(item, StringComparison.OrdinalIgnoreCase)))
		{
			((CBaseEntity)((CBasePlayerController)userid).Pawn.Value).Health -= 9999;
			Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)userid).Pawn.Value, "CBaseEntity", "m_iHealth", 0);
		}
		return (HookResult)0;
	}
}
