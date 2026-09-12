using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DeagleKing : DiceBlueprint
{
	public override string ClassName => "DeagleKing";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public DeagleKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "SniperElite"))
			{
				DiceSynergy.AnnounceCombo(player, "精准猎杀", "精准猎杀联动生效！");
			}
			if (DiceSynergy.HasPartner(player, "DeadHand"))
			{
				DiceSynergy.AnnounceCombo(player, "致命一击", "开枪自伤减半+命中回血翻倍！");
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
	}

	public override void Reset()
	{
		_players.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		var attacker = info.Attacker;
		CCSPlayerController val = null;
		if (attacker != null)
		{
			CBaseEntity value = attacker.Value;
			if (value != null)
			{
				CCSPlayerPawn pawn = ((NativeObject)value).As<CCSPlayerPawn>();
				if (pawn != null)
				{
					var controller = ((CBasePlayerPawn)pawn).Controller;
					if (controller != null && controller.Value != null)
					{
						val = ((NativeObject)controller.Value).As<CCSPlayerController>();
					}
				}
			}
		}
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		CCSPlayerPawn val2 = val.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid)
		{
			return (HookResult)0;
		}
		var activeWeapon = val2.WeaponServices?.ActiveWeapon;
		CBasePlayerWeapon weapon = ((activeWeapon == null) ? null : activeWeapon.Value);
		if ((CEntityInstance)(object)weapon == (CEntityInstance)null || !((CEntityInstance)weapon).IsValid)
		{
			return (HookResult)0;
		}
		string designerName = ((CEntityInstance)weapon).DesignerName;
		if (designerName == null || !designerName.Contains("deagle"))
		{
			return (HookResult)0;
		}
		info.Damage *= (DiceSynergy.HasPartner(val, "SniperElite") || DiceSynergy.HasPartner(val, "DeadHand")) ? 5f : 3f;
		return (HookResult)1;
	}
}
