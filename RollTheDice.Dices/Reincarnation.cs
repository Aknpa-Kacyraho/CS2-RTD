using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Reincarnation : DiceBlueprint
{
	public static readonly Dictionary<ulong, int> PendingExtraDice = new Dictionary<ulong, int>();

	public static readonly HashSet<ulong> ComboWheelVictims = new HashSet<ulong>();

	public override string ClassName => "Reincarnation";

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

	public Reincarnation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			int num = (PendingExtraDice.TryGetValue(((CBasePlayerController)player).SteamID, out var value) ? value : 0);
			if (num > 0)
			{
				player.PrintToCenterAlert($"\ud83d\udd04 轮回！下回合获得{num}个额外骰子！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		ComboWheelVictims.Clear();
	}

	public override void Destroy()
	{
		PendingExtraDice.Clear();
		ComboWheelVictims.Clear();
		_players.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		int maxStacks = _config.Dices.Reincarnation.MaxStacks;
		int num = (PendingExtraDice.TryGetValue(((CBasePlayerController)userid).SteamID, out var value) ? value : 0);
		if (num >= maxStacks)
		{
			return (HookResult)0;
		}
		num++;
		PendingExtraDice[((CBasePlayerController)userid).SteamID] = num;
		userid.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Reincarnation_progress"].Value.Replace("{count}", num.ToString()));
		if (DiceSynergy.HasPartner(userid, "WheelOfFate"))
		{
			ComboWheelVictims.Add(((CBasePlayerController)userid).SteamID);
		}
		return (HookResult)0;
	}
}
