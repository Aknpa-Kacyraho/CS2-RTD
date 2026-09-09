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

public class JumpHeal : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, bool> _wasOnGround = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "JumpHeal";

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

	public JumpHeal(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_wasOnGround[player] = true;
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
		_wasOnGround.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_wasOnGround.Clear();
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
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			CCSPlayerPawn value = item.PlayerPawn.Value;
			bool flag = ((CBaseEntity)value).GroundEntity != null && ((CBaseEntity)value).GroundEntity.IsValid;
			bool valueOrDefault = _wasOnGround.GetValueOrDefault(item, defaultValue: true);
			_wasOnGround[item] = flag;
			if (valueOrDefault && !flag)
			{
				int num = _random.Next(_config.Dices.JumpHeal.HealMin, _config.Dices.JumpHeal.HealMax + 1);
				if (DiceSynergy.HasPartner(item, "Regeneration"))
				{
					num *= 2;
				}
				int num2 = Math.Min(((CBaseEntity)value).Health + num, ((CBaseEntity)value).MaxHealth);
				int num3 = num2 - ((CBaseEntity)value).Health;
				if (num3 > 0)
				{
					((CBaseEntity)value).Health = num2;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
					item.PrintToCenterAlert($"\ud83c\udf6c +{num3} HP");
				}
			}
		}
	}
}
