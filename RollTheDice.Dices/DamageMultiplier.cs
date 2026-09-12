using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DamageMultiplier : DiceBlueprint
{
	public readonly Random _random = new Random();

	public readonly Dictionary<uint, float> _playerMultipliers = new Dictionary<uint, float>();

	private bool _comboActive;

	public override string ClassName => "DamageMultiplier";

	public DamageMultiplier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			float minMultiplier = _config.Dices.DamageMultiplier.MinMultiplier;
			float maxMultiplier = _config.Dices.DamageMultiplier.MaxMultiplier;
			float value = (float)Math.Round(_random.NextDouble() * (double)(maxMultiplier - minMultiplier) + (double)minMultiplier, 2);
			_playerMultipliers.Add(((CEntityInstance)((CBasePlayerController)player).Pawn.Value).Index, value);
			_comboActive = DiceSynergy.HasPartner(player, "Berserker");
			DamageBonusManager.Register(player, "DamageMultiplier", (_comboActive ? (value * 1.5f) : value) - 1f);
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "狂暴之力", "毁灭之力+狂战士！极限倍率+50%");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"multiplier",
					value.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageBonusManager.Unregister(player, "DamageMultiplier");
		_players.Remove(player);
		if (!((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_playerMultipliers.Remove(((CEntityInstance)((CBasePlayerController)player).Pawn.Value).Index);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players)
		{
			DamageBonusManager.Unregister(item, "DamageMultiplier");
		}
		_players.Clear();
		_playerMultipliers.Clear();
	}
}
