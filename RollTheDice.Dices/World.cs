using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class World : DiceBlueprint
{
	private bool _comboActive;

	public static Dictionary<ulong, int> PendingExtraRolls = new Dictionary<ulong, int>();

	private static bool _granting;

	public override string ClassName => "World";

	public World(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		PendingExtraRolls[((CBasePlayerController)player).SteamID] = _config.Dices.World.ExtraDiceCount;
		GrantExtraDice(player);
		_comboActive = DiceSynergy.HasPartner(player, "Heaven");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "Heaven"))
			{
				DiceSynergy.AnnounceCombo(player, "超越天堂", "世界与天堂融合！获得超越天堂之力！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						instance.RemoveDiceFromPlayer(captured, "World");
						instance.RemoveDiceFromPlayer(captured, "Heaven");
						instance.GrantComboDice(captured, "BeyondHeaven");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！世界与天堂共鸣！");
		}
		if (DiceSynergy.HasPartner(player, "Bugle"))
		{
			DiceSynergy.AnnounceCombo(player, "天启", "团队联动！冲锋号+世界共鸣！");
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_World_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName).Replace("{count}", _config.Dices.World.ExtraDiceCount.ToString()));
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			},
			{
				"extraCount",
				_config.Dices.World.ExtraDiceCount.ToString()
			}
		});
	}

	/// <summary>抽到世界立即补发额外骰子；不再依赖回合开始的补骰窗口（在窗口内抽到会被清 pending 而丢失）。</summary>
	private void GrantExtraDice(CCSPlayerController player)
	{
		if (_granting)
		{
			return;
		}
		RollTheDice instance = RollTheDice.Instance;
		if (instance == null)
		{
			return;
		}
		_granting = true;
		try
		{
			int count = _config.Dices.World.ExtraDiceCount;
			for (int i = 0; i < count; i++)
			{
				if (!instance.ForceExtraDiceForPlayer(player))
				{
					break;
				}
			}
		}
		finally
		{
			_granting = false;
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		PendingExtraRolls.Remove(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		PendingExtraRolls.Clear();
	}
}
