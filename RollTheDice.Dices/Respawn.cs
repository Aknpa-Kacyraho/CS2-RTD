using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Respawn : DiceBlueprint
{
	private bool _comboActive;

	public readonly Random _random = new Random();

	public override string ClassName => "Respawn";

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

	public Respawn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Dragonborn");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "浴火重生", "浴火重生联动生效！");
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
		if (reason != DiceRemoveReason.Death)
		{
			_players.Remove(player);
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController victim = @event.Userid;
		if (!((CEntityInstance)(object)victim == (CEntityInstance)null) && _players.Contains(victim))
		{
			CHandle<CCSPlayerPawn> playerPawn = victim.PlayerPawn;
			object obj;
			if (playerPawn == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn value = playerPawn.Value;
				obj = ((value != null) ? ((CBasePlayerPawn)value).WeaponServices : null);
			}
			if (obj != null)
			{
				Vector val = null;
				CHandle<CCSPlayerPawn> playerPawn2 = victim.PlayerPawn;
				object obj2;
				if (playerPawn2 == null)
				{
					obj2 = null;
				}
				else
				{
					CCSPlayerPawn value2 = playerPawn2.Value;
					obj2 = ((value2 != null) ? ((CBaseEntity)value2).AbsOrigin : null);
				}
				if (obj2 != null)
				{
					val = new Vector((float?)((CBaseEntity)victim.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)victim.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)victim.PlayerPawn.Value).AbsOrigin.Z);
				}
				List<string> tmpWeaponList = new List<string>();
				Vector savedDeathPos = val;
				CCSPlayerController attacker = @event.Attacker;
				object obj3;
				if (attacker == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CCSPlayerPawn> playerPawn3 = attacker.PlayerPawn;
					if (playerPawn3 == null)
					{
						obj3 = null;
					}
					else
					{
						CCSPlayerPawn value3 = playerPawn3.Value;
						obj3 = ((value3 != null) ? ((CBasePlayerPawn)value3).WeaponServices : null);
					}
				}
				if (obj3 != null)
				{
					foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)attacker.PlayerPawn.Value).WeaponServices.MyWeapons)
					{
						if (myWeapon != null && myWeapon.IsValid && !((CEntityInstance)(object)myWeapon.Value == (CEntityInstance)null) && (!((CEntityInstance)(object)myWeapon.Value != (CEntityInstance)null) || ((CEntityInstance)myWeapon.Value).DesignerName != null) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)5/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_knife") && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower(CultureInfo.CurrentCulture)))
						{
							tmpWeaponList.Add(((CEntityInstance)myWeapon.Value).DesignerName);
						}
					}
				}
				Server.NextFrame((Action)delegate
				{
					Server.NextFrame((Action)delegate
					{
						//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
						//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
						//IL_00e0: Expected O, but got Unknown
						//IL_00e0: Expected O, but got Unknown
						CCSPlayerController obj4 = victim;
						if (!((CEntityInstance)(object)((obj4 == null) ? null : obj4.PlayerPawn?.Value) == (CEntityInstance)null) && _players.Contains(victim) && ((CBaseEntity)victim.PlayerPawn.Value).LifeState != 0)
						{
							victim.Respawn();
							if (savedDeathPos != null)
							{
								CCSPlayerPawn value4 = victim.PlayerPawn.Value;
								if (value4 != null)
								{
									((CBaseEntity)value4).Teleport(savedDeathPos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
								}
							}
							Server.NextFrame((Action)delegate
							{
								//IL_0095: Unknown result type (might be due to invalid IL or missing references)
								//IL_009b: Invalid comparison between Unknown and I4
								//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
								//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
								//IL_00ca: Expected O, but got Unknown
								CCSPlayerController obj5 = victim;
								if (!((CEntityInstance)(object)((obj5 == null) ? null : obj5.PlayerPawn?.Value) == (CEntityInstance)null))
								{
									CCSPlayerController obj6 = victim;
									object obj7;
									if (obj6 == null)
									{
										obj7 = null;
									}
									else
									{
										CHandle<CCSPlayerPawn> playerPawn4 = obj6.PlayerPawn;
										obj7 = ((playerPawn4 != null) ? ((CBasePlayerPawn)playerPawn4.Value).ItemServices : null);
									}
									if (obj7 != null && ((CBaseEntity)victim.PlayerPawn.Value).LifeState == 0)
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
									if (DiceSynergy.HasPartner(victim, "Dragonborn"))
										{
											((CBaseEntity)victim.PlayerPawn.Value).MaxHealth = 200;
											((CBaseEntity)victim.PlayerPawn.Value).Health = 200;
											victim.PlayerPawn.Value.ArmorValue = 200;
											Utilities.SetStateChanged((CBaseEntity)(object)victim.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth", 0);
											Utilities.SetStateChanged((CBaseEntity)(object)victim.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
											victim.PrintToCenterAlert("\ud83d\udd25 浴火重生！龙裔之力加持！200HP 200护甲！");
										}
										_players.Remove(victim);
									}
								}
							});
						}
					});
				});
				return (HookResult)0;
			}
		}
		return (HookResult)0;
	}
}
