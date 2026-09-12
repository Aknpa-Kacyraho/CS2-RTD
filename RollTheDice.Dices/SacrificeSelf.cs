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

public class SacrificeSelf : DiceBlueprint
{
	private readonly HashSet<CCSPlayerController> _boostedTeammates = new HashSet<CCSPlayerController>();

	public override string ClassName => "SacrificeSelf";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerButtonsChanged";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public SacrificeSelf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
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
			player.PrintToCenterAlert("\ud83d\udc9d 按E键牺牲自己！全队获得20%伤害+50%速度加成！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _boostedTeammates.ToList())
		{
			SpeedBonusManager.Unregister(item, "SacrificeSelf");
			DamageBonusManager.Unregister(item, "SacrificeSelf");
		}
		_players.Clear();
		_boostedTeammates.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float speedMultiplier = _config.Dices.SacrificeSelf.SpeedMultiplier;
		string playerName = ((CBasePlayerController)player).PlayerName;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			SpeedBonusManager.Register(item, "SacrificeSelf", _config.Dices.SacrificeSelf.SpeedMultiplier - 1f);
			DamageBonusManager.Register(item, "SacrificeSelf", _config.Dices.SacrificeSelf.DamageMultiplier - 1f);
			_boostedTeammates.Add(item);
			item.PrintToCenterAlert("\ud83d\udc9d 队友牺牲了！获得+20%伤害+50%速度加成！");
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_SacrificeSelf_broadcast"].Value.Replace("{playerName}", playerName));
		if (!player.IsBot && !((CBasePlayerController)player).IsHLTV)
		{
			((CBasePlayerPawn)player.PlayerPawn.Value).CommitSuicide(false, true);
		}
	}

	public void OnTick()
	{
		if (_boostedTeammates.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _boostedTeammates.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				float effective = SpeedBonusManager.GetEffective(item);
				if (effective > 0f)
				{
					item.PlayerPawn.Value.VelocityModifier = 1f + effective;
					Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}
}
