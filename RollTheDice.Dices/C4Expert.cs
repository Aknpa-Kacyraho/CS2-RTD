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

public class C4Expert : DiceBlueprint
{
	public override string ClassName => "C4Expert";

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

	public C4Expert(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
			if (DiceSynergy.HasPartner(player, "HotPotato"))
			{
				DiceSynergy.AnnounceCombo(player, "炸弹专家", "热土豆灼烧免疫，持包时 99% 减伤！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageReductionManager.Unregister(player, "C4Expert");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageReductionManager.Unregister(item, "C4Expert");
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Invalid comparison between Unknown and I4
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Invalid comparison between Unknown and I4
		if (_players.Count == 0)
		{
			return;
		}
		CPlantedC4 val = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
				{
					if ((int)item.Team == 3 && (CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && val.DefuseCountDown > 1f)
					{
						val.DefuseCountDown = 1f;
						Utilities.SetStateChanged((CBaseEntity)(object)val, "CPlantedC4", "m_fDefuseCountDown", 0);
					}
					if ((int)item.Team == 2 && (CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid)
					{
						val.C4Blow = ((val.C4Blow > 0f) ? Math.Min(val.C4Blow, Server.CurrentTime + 3f) : 0f);
					}
					bool carryingC4 = false;
					CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
					if (weaponServices != null && weaponServices.MyWeapons != null)
					{
						foreach (CHandle<CBasePlayerWeapon> weapon in weaponServices.MyWeapons)
						{
							CBasePlayerWeapon value = weapon?.Value;
							if (value != null && ((CEntityInstance)value).DesignerName == "weapon_c4")
							{
								carryingC4 = true;
								break;
							}
						}
					}
					if (carryingC4 && DiceSynergy.HasPartner(item, "HotPotato"))
					{
						DamageReductionManager.Register(item, "C4Expert", 0.99f);
					}
					else
					{
						DamageReductionManager.Unregister(item, "C4Expert");
					}
				}
			}
			catch
			{
			}
		}
	}
}
