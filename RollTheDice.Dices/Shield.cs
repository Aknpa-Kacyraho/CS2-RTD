using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Shield : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Shield";

	public Shield(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			int num = _random.Next(_config.Dices.Shield.ArmorMin, _config.Dices.Shield.ArmorMax + 1);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			value.ArmorValue = Math.Min(value.ArmorValue + num, 100);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			if (_config.Dices.Shield.Helmet)
			{
				player.GiveNamedItem("item_assaultsuit");
			}
			_players.Add(player);
			bool comboActive = DiceSynergy.HasPartner(player, "Evasion");
			DamageReductionManager.Register(player, "Shield", comboActive ? 0.75f : 0.5f);
			if (comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "钢铁壁垒", "钢铁壁垒联动生效！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"armor",
					num.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		DamageReductionManager.Unregister(player, "Shield");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players)
		{
			DamageReductionManager.Unregister(item, "Shield");
		}
		_players.Clear();
	}
}
