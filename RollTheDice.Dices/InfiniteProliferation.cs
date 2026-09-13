using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class InfiniteProliferation : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _respawnsLeft = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "InfiniteProliferation";

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

	public InfiniteProliferation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Necromancer");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "不死军团", "不死军团联动生效！");
			}
			_respawnsLeft[player] = _config.Dices.InfiniteProliferation.MaxRespawns;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert($"\ud83d\udd04 无限增殖！可复活{_config.Dices.InfiniteProliferation.MaxRespawns}次，每次HP/甲减半！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (reason != DiceRemoveReason.Death)
		{
			_players.Remove(player);
			_respawnsLeft.Remove(player);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_respawnsLeft.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !_players.Contains(victim))
		{
			return (HookResult)0;
		}
		if (!_respawnsLeft.TryGetValue(victim, out var value) || value <= 0)
		{
			return (HookResult)0;
		}
		List<string> tmpWeaponList = new List<string>();
		CCSPlayerController attacker = @event.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CHandle<CCSPlayerPawn> playerPawn = attacker.PlayerPawn;
			if (playerPawn == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn value2 = playerPawn.Value;
				obj = ((value2 != null) ? ((CBasePlayerPawn)value2).WeaponServices : null);
			}
		}
		if (obj != null)
		{
			foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)attacker.PlayerPawn.Value).WeaponServices.MyWeapons)
			{
				object obj2;
				if (myWeapon == null)
				{
					obj2 = null;
				}
				else
				{
					CBasePlayerWeapon value3 = myWeapon.Value;
					obj2 = ((value3 != null) ? ((CEntityInstance)value3).DesignerName : null);
				}
				if (obj2 != null)
				{
					string designerName = ((CEntityInstance)myWeapon.Value).DesignerName;
					if (!designerName.Contains("knife") && !designerName.Contains("bayonet") && !designerName.Contains("c4"))
					{
						tmpWeaponList.Add(designerName);
					}
				}
			}
		}
		int remaining = value;
		int maxRespawns = _config.Dices.InfiniteProliferation.MaxRespawns;
		int deathsUsed = maxRespawns - remaining + 1;
		int newArmor = _config.Dices.InfiniteProliferation.BaseArmor;
		string victimName = ((CBasePlayerController)victim).PlayerName;
		Server.NextFrame((Action)delegate
		{
			Server.NextFrame((Action)delegate
			{
				CCSPlayerController obj3 = victim;
				if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null) && _players.Contains(victim) && ((CBaseEntity)victim.PlayerPawn.Value).LifeState != 0)
				{
					victim.Respawn();
					Invulnerability.Grant(victim, _config.Dices.InfiniteProliferation.ReviveInvulnSeconds);
					Server.NextFrame((Action)delegate
					{
						//IL_013b: Unknown result type (might be due to invalid IL or missing references)
						//IL_0141: Invalid comparison between Unknown and I4
						//IL_0155: Unknown result type (might be due to invalid IL or missing references)
						CCSPlayerController obj4 = victim;
						if (!((CEntityInstance)(object)((obj4 == null) ? null : obj4.PlayerPawn?.Value) == (CEntityInstance)null))
						{
							CCSPlayerController obj5 = victim;
							if (((obj5 != null) ? ((CBasePlayerPawn)obj5.PlayerPawn.Value).ItemServices : null) != null && ((CBaseEntity)victim.PlayerPawn.Value).LifeState == 0)
							{
								CCSPlayerPawn value4 = victim.PlayerPawn.Value;
								victim.RemoveWeapons();
								victim.GiveNamedItem("weapon_knife");
								int maxHealth = ((CBaseEntity)value4).MaxHealth;
								int num = maxHealth;
								for (int i = 0; i < deathsUsed; i++)
								{
									num /= 2;
									if (num < 1)
									{
										num = 1;
									}
								}
								((CBaseEntity)value4).Health = num;
								((CBaseEntity)value4).MaxHealth = num;
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CBaseEntity", "m_iHealth", 0);
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CBaseEntity", "m_iMaxHealth", 0);
								value4.ArmorValue = newArmor;
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CCSPlayerPawn", "m_ArmorValue", 0);
								victim.GiveNamedItem("item_assaultsuit");
								if ((int)victim.Team == 3)
								{
									new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value4).ItemServices).Handle).HasDefuser = true;
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
									victim.GiveNamedItem("weapon_ak47");
									victim.GiveNamedItem("weapon_deagle");
								}
								int num2 = remaining - 1;
								_respawnsLeft[victim] = num2;
								victim.PrintToCenterAlert($"\ud83d\udd04 无限增殖！还剩{num2}次复活 | HP:{num} 甲:{newArmor}");
								Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_InfiniteProliferation_revived"].Value.Replace("{playerName}", victimName).Replace("{hp}", num.ToString()).Replace("{armor}", newArmor.ToString())
									.Replace("{left}", num2.ToString()));
								if (num2 <= 0)
								{
									_players.Remove(victim);
									_respawnsLeft.Remove(victim);
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
