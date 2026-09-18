using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Emperor : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, bool> _usedResurrection = new Dictionary<CCSPlayerController, bool>();

	private int _resurrectionsUsed;

	public override string ClassName => "Emperor";

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

	public Emperor(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Empress");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "王权永恒", "王权永恒联动生效！");
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
		_usedResurrection.Clear();
		_resurrectionsUsed = 0;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		if (_players.Contains(victim))
		{
			return (HookResult)0;
		}
		if (_resurrectionsUsed >= _config.Dices.Emperor.MaxResurrections)
		{
			return (HookResult)0;
		}
		CCSPlayerController val = null;
		foreach (CCSPlayerController player in _players)
		{
			if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || ((CBaseEntity)player).TeamNum != ((CBaseEntity)victim).TeamNum)
			{
				continue;
			}
			val = player;
			break;
		}
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		_resurrectionsUsed++;
		List<string> tmpWeaponList = new List<string>();
		CHandle<CCSPlayerPawn> playerPawn = victim.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value2 = playerPawn.Value;
			obj = ((value2 != null) ? ((CBasePlayerPawn)value2).WeaponServices : null);
		}
		if (obj != null)
		{
			foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)victim.PlayerPawn.Value).WeaponServices.MyWeapons)
			{
				if (myWeapon != null && myWeapon.IsValid && !((CEntityInstance)(object)myWeapon.Value == (CEntityInstance)null) && ((CEntityInstance)myWeapon.Value).DesignerName != null && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)5/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_knife") && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower()))
				{
					tmpWeaponList.Add(((CEntityInstance)myWeapon.Value).DesignerName);
				}
			}
		}
		// 全队共享复活次数，已在上方累加。
		Server.NextFrame((Action)delegate
		{
			Server.NextFrame((Action)delegate
			{
				CCSPlayerController obj2 = victim;
				if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)victim.PlayerPawn.Value).LifeState != 0)
				{
					victim.Respawn();
					Invulnerability.Grant(victim, _config.Dices.Emperor.ReviveInvulnSeconds);
					Server.NextFrame((Action)delegate
					{
						//IL_0094: Unknown result type (might be due to invalid IL or missing references)
						//IL_009a: Invalid comparison between Unknown and I4
						//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
						//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
						//IL_00c9: Expected O, but got Unknown
						CCSPlayerController obj3 = victim;
						if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null))
						{
							CCSPlayerController obj4 = victim;
							object obj5;
							if (obj4 == null)
							{
								obj5 = null;
							}
							else
							{
								CHandle<CCSPlayerPawn> playerPawn2 = obj4.PlayerPawn;
								obj5 = ((playerPawn2 != null) ? ((CBasePlayerPawn)playerPawn2.Value).ItemServices : null);
							}
							if (obj5 != null && ((CBaseEntity)victim.PlayerPawn.Value).LifeState == 0)
							{
								victim.RemoveWeapons();
								victim.GiveNamedItem("weapon_knife");
								if ((int)victim.Team == 3)
								{
									CCSPlayer_ItemServices val2 = new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)victim.PlayerPawn.Value).ItemServices).Handle);
									val2.HasDefuser = true;
									CCSPlayer_ItemServices val3 = val2;
								}
								if (tmpWeaponList.Count > 0)
								{
									foreach (string item in tmpWeaponList)
									{
										victim.GiveNamedItem(item);
									}
								}
								else
								{
									victim.GiveNamedItem(_config.Dices.Respawn.DefaultPrimaryWeapon);
									victim.GiveNamedItem(_config.Dices.Respawn.DefaultSecondaryWeapon);
								}
								victim.PlayerPawn.Value.ArmorValue = 100;
								if (DiceSynergy.HasPartner(val, "Empress"))
								{
									((CBaseEntity)victim.PlayerPawn.Value).MaxHealth = 150;
									((CBaseEntity)victim.PlayerPawn.Value).Health = 150;
									victim.PlayerPawn.Value.ArmorValue = 150;
									Utilities.SetStateChanged((CBaseEntity)(object)victim.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth", 0);
									Utilities.SetStateChanged((CBaseEntity)(object)victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
									victim.PrintToCenterAlert("\ud83d\udc51 王权永恒！150HP 150护甲！");
								}
							}
						}
					});
				}
			});
		});
		return (HookResult)0;
	}
}
