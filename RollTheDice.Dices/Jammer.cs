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

public class Jammer : DiceBlueprint
{
	public readonly Random _random = new Random();

	private const uint HIDE_VISUAL_ONLY = 12593u;

	private readonly Dictionary<CCSPlayerController, uint> _originalHUDs = new Dictionary<CCSPlayerController, uint>();

	private CCSPlayerController? _jammerPlayer;

	public override string ClassName => "Jammer";

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

	public Jammer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_jammerPlayer = player;
		foreach (CCSPlayerController player2 in Utilities.GetPlayers())
		{
			if (!((CEntityInstance)(object)((player2 == null) ? null : player2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player2.PlayerPawn.Value).IsValid && !((CEntityInstance)(object)player2 == (CEntityInstance)(object)player) && ((CBaseEntity)player2).TeamNum != ((CBaseEntity)player).TeamNum)
			{
				_originalHUDs[player2] = ((CBasePlayerPawn)player2.PlayerPawn.Value).HideHUD;
				((CBasePlayerPawn)player2.PlayerPawn.Value).HideHUD |= 12593u;
				Utilities.SetStateChanged((CBaseEntity)(object)player2.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD", 0);
			}
		}
		SpeedBonusManager.Register(player, "Jammer", _config.Dices.Jammer.SpeedMultiplier - 1f);
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		try
		{
			RestoreAllHUDs();
		}
		catch
		{
		}
		SpeedBonusManager.Unregister(player, "Jammer");
		_players.Remove(player);
		_jammerPlayer = null;
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
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
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				float effective = SpeedBonusManager.GetEffective(item);
				item.PlayerPawn.Value.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	private void RestoreAllHUDs()
	{
		foreach (KeyValuePair<CCSPlayerController, uint> originalHUD in _originalHUDs)
		{
			CCSPlayerController key = originalHUD.Key;
			if (!((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)key.PlayerPawn.Value).IsValid)
			{
				((CBasePlayerPawn)key.PlayerPawn.Value).HideHUD = originalHUD.Value;
				Utilities.SetStateChanged((CBaseEntity)(object)key.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD", 0);
			}
		}
		_originalHUDs.Clear();
	}
}
