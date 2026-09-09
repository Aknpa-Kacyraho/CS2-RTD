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

public class Regeneration : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextHealTime = new Dictionary<CCSPlayerController, float>();

	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Regeneration";

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

	public Regeneration(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_nextHealTime[player] = 0f;
			_comboActive = DiceSynergy.HasPartner(player, "JumpHeal");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "生命律动", $"生命之泉每{_config.Dices.Regeneration.TickInterval:0.#}s回{_config.Dices.Regeneration.HealPerTick}HP，联动跳跳糖翻倍！");
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
		_nextHealTime.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextHealTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_nextHealTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		float num2 = _config.Dices.Regeneration.TickInterval;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0 && _nextHealTime.TryGetValue(item, out var value) && !(value > num))
				{
					CCSPlayerPawn value2 = item.PlayerPawn.Value;
					int num3 = _config.Dices.Regeneration.HealPerTick;
					if (DiceSynergy.HasPartner(item, "JumpHeal"))
					{
						num3 *= 2;
					}
					int num4 = Math.Min(((CBaseEntity)value2).Health + num3, ((CBaseEntity)value2).MaxHealth);
					if (num4 > ((CBaseEntity)value2).Health)
					{
						((CBaseEntity)value2).Health = num4;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					}
					_nextHealTime[item] = num + num2;
				}
			}
			catch
			{
				_nextHealTime.Remove(item);
			}
		}
	}
}
