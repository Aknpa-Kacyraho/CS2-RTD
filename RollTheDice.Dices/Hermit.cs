using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Hermit : DiceBlueprint
{
	public override string ClassName => "Hermit";

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

	public override Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode> { 
	{
		208,
		(HookMode)0
	} };

	public Hermit(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			bool flag = DiceSynergy.HasPartner(player, "ShadowWarrior");
			if (flag)
			{
				DiceSynergy.AnnounceCombo(player, "暗影行者", "极限隐身！");
			}
			CCSPlayerPawn value = player.PlayerPawn.Value;
			int alpha = (flag ? 10 : 30);
			((CBaseModelEntity)value).Render = Color.FromArgb(alpha, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
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
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid && (CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid && (CEntityInstance)(object)item.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				((CBaseModelEntity)item.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
			}
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
				int alpha = (DiceSynergy.HasPartner(item, "ShadowWarrior") ? 10 : 30);
				((CBaseModelEntity)item.PlayerPawn.Value).Render = Color.FromArgb(alpha, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
	}

	public HookResult HookUserMessage208(UserMessage um)
	{
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		int num = um.ReadInt("source_entity_index", (int?)null);
		foreach (CCSPlayerController player in _players)
		{
			if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CEntityInstance)player.PlayerPawn.Value).Index == num)
			{
				um.Recipients.Clear();
				return (HookResult)4;
			}
		}
		return (HookResult)0;
	}
}
