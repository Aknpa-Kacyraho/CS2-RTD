using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class WheelOfFate : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "WheelOfFate";

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

	public WheelOfFate(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
			NotifyStatus(player, ClassName, new Dictionary<string, string> { 
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

	public override void Reset()
	{
		_players.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
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
				double num = _random.NextDouble();
				if (num >= (double)_config.Dices.WheelOfFate.ReviveChance)
				{
					string value2 = _localizer["dice_WheelOfFate_failed"].Value;
					if (!string.IsNullOrEmpty(value2))
					{
						victim.PrintToCenterAlert(value2.Replace("{playerName}", ((CBasePlayerController)victim).PlayerName));
					}
					return (HookResult)0;
				}
				List<string> tmpWeaponList = new List<string>();
				CCSPlayerController attacker = @event.Attacker;
				object obj2;
				if (attacker == null)
				{
					obj2 = null;
				}
				else
				{
					CHandle<CCSPlayerPawn> playerPawn2 = attacker.PlayerPawn;
					if (playerPawn2 == null)
					{
						obj2 = null;
					}
					else
					{
						CCSPlayerPawn value3 = playerPawn2.Value;
						obj2 = ((value3 != null) ? ((CBasePlayerPawn)value3).WeaponServices : null);
					}
				}
				if (obj2 != null)
				{
					foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)attacker.PlayerPawn.Value).WeaponServices.MyWeapons)
					{
						if (myWeapon != null && myWeapon.IsValid && !((CEntityInstance)(object)myWeapon.Value == (CEntityInstance)null) && ((CEntityInstance)myWeapon.Value).DesignerName != null && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)5/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_knife") && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)501/*cast due to constrained. prefix*/).ToString().ToLower()) && !(((CEntityInstance)myWeapon.Value).DesignerName == "weapon_" + ((object)(CsItem)500/*cast due to constrained. prefix*/).ToString().ToLower()))
						{
							tmpWeaponList.Add(((CEntityInstance)myWeapon.Value).DesignerName);
						}
					}
				}
				Server.NextFrame((Action)delegate
				{
					Server.NextFrame((Action)delegate
					{
						CCSPlayerController obj3 = victim;
						if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null) && _players.Contains(victim) && ((CBaseEntity)victim.PlayerPawn.Value).LifeState != 0)
						{
							victim.Respawn();
							Server.NextFrame((Action)delegate
							{
								//IL_0094: Unknown result type (might be due to invalid IL or missing references)
								//IL_009a: Invalid comparison between Unknown and I4
								//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
								//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
								//IL_00c9: Expected O, but got Unknown
								CCSPlayerController obj4 = victim;
								if (!((CEntityInstance)(object)((obj4 == null) ? null : obj4.PlayerPawn?.Value) == (CEntityInstance)null))
								{
									CCSPlayerController obj5 = victim;
									object obj6;
									if (obj5 == null)
									{
										obj6 = null;
									}
									else
									{
										CHandle<CCSPlayerPawn> playerPawn3 = obj5.PlayerPawn;
										obj6 = ((playerPawn3 != null) ? ((CBasePlayerPawn)playerPawn3.Value).ItemServices : null);
									}
									if (obj6 != null && ((CBaseEntity)victim.PlayerPawn.Value).LifeState == 0)
									{
										victim.RemoveWeapons();
										victim.GiveNamedItem("weapon_knife");
										if ((int)victim.Team == 3)
										{
											CCSPlayer_ItemServices val = new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)victim.PlayerPawn.Value).ItemServices).Handle);
											val.HasDefuser = true;
											CCSPlayer_ItemServices val2 = val;
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
										Utilities.SetStateChanged((CBaseEntity)(object)victim.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue", 0);
										NotifyStatus(victim, ClassName, new Dictionary<string, string> { 
										{
											"playerName",
											((CBasePlayerController)victim).PlayerName
										} });
										Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_WheelOfFate_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)victim).PlayerName));
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
