using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Necromancer : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<ulong, float> _invincibilityEndTime = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, Vector> _deathPositions = new Dictionary<ulong, Vector>();

	private static readonly HashSet<ulong> _revivedThisRound = new HashSet<ulong>();

	public override string ClassName => "Necromancer";

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

	public Necromancer(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "InfiniteProliferation");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "不死军团", "不死军团联动生效！");
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
		_invincibilityEndTime.Clear();
		_deathPositions.Clear();
		_revivedThisRound.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid)
		{
			return (HookResult)0;
		}
		if (_invincibilityEndTime.TryGetValue(((CBasePlayerController)val).SteamID, out var value2) && Server.CurrentTime < value2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Expected O, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || userid.IsBot || ((CBasePlayerController)userid).IsHLTV)
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value = playerPawn.Value;
			obj = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
		}
		if (obj != null)
		{
			Vector value2 = new Vector((float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Z);
			_deathPositions[((CBasePlayerController)userid).SteamID] = value2;
		}
		int reviveCost = (int)float.Round(_config.Dices.Necromancer.ReviveHPCost);
		CCSPlayerController val = null;
		foreach (CCSPlayerController player in _players)
		{
			if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player == (CEntityInstance)(object)userid || ((CBaseEntity)player).TeamNum != ((CBaseEntity)userid).TeamNum || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0 || ((CBaseEntity)player.PlayerPawn.Value).Health <= reviveCost)
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
		ulong steamID = ((CBasePlayerController)userid).SteamID;
		if (_revivedThisRound.Contains(steamID))
		{
			return (HookResult)0;
		}
		_revivedThisRound.Add(steamID);
		CCSPlayerPawn value3 = val.PlayerPawn.Value;
		((CBaseEntity)value3).Health -= reviveCost;
		Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
		Vector savedDeathPos = new Vector((float?)0f, (float?)0f, (float?)0f);
		bool hasDeathPos = _deathPositions.TryGetValue(steamID, out Vector value4);
		if (hasDeathPos)
		{
			savedDeathPos = value4;
		}
		CCSPlayerController capturedDead = userid;
		CCSPlayerController capturedNecro = val;
		Server.NextFrame((Action)delegate
		{
			Server.NextFrame((Action)delegate
			{
				CCSPlayerController obj2 = capturedDead;
				if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState != 0)
				{
					capturedDead.Respawn();
					Server.NextFrame((Action)delegate
					{
						//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
						//IL_0253: Unknown result type (might be due to invalid IL or missing references)
						CCSPlayerController obj3 = capturedDead;
						if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState == 0)
						{
							capturedDead.RemoveWeapons();
							capturedDead.GiveNamedItem("weapon_knife");
							if (hasDeathPos)
							{
								CCSPlayerPawn pawnToTP = capturedDead.PlayerPawn.Value;
								new Timer(0.1f, (Action)delegate
								{
									//IL_0082: Unknown result type (might be due to invalid IL or missing references)
									//IL_008c: Expected O, but got Unknown
									//IL_005f: Unknown result type (might be due to invalid IL or missing references)
									if (!((CEntityInstance)(object)pawnToTP == (CEntityInstance)null) && ((CEntityInstance)pawnToTP).IsValid)
									{
										((CBaseEntity)pawnToTP).Teleport(savedDeathPos, (QAngle)(((object)pawnToTP.EyeAngles) ?? ((object)new QAngle((float?)0f, (float?)0f, (float?)0f))), new Vector((float?)0f, (float?)0f, (float?)0f));
									}
								}, (TimerFlags?)null);
							}
							ulong revivedID = ((CBasePlayerController)capturedDead).SteamID;
							_invincibilityEndTime[revivedID] = Server.CurrentTime + 1f;
							CCSPlayerPawn value5 = capturedDead.PlayerPawn.Value;
							((CBaseModelEntity)value5).Render = Color.FromArgb(128, 255, 255, 255);
							Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseModelEntity", "m_clrRender", 0);
							List<string> weaponList = new List<string>();
							if (((CBasePlayerPawn)value5).WeaponServices != null)
							{
								foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)value5).WeaponServices.MyWeapons)
								{
									if ((CEntityInstance)(object)myWeapon?.Value != (CEntityInstance)null && ((CEntityInstance)myWeapon.Value).IsValid && ((CEntityInstance)myWeapon.Value).DesignerName != null && !((CEntityInstance)myWeapon.Value).DesignerName.Contains("knife"))
									{
										weaponList.Add(((CEntityInstance)myWeapon.Value).DesignerName);
									}
								}
							}
							capturedDead.RemoveWeapons();
							capturedDead.GiveNamedItem("weapon_knife");
							CCSPlayerController capDead2 = capturedDead;
							new Timer(1f, (Action)delegate
							{
								CCSPlayerController obj5 = capDead2;
								if ((CEntityInstance)(object)((obj5 == null) ? null : obj5.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)capDead2.PlayerPawn.Value).IsValid)
								{
									((CBaseModelEntity)capDead2.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
									Utilities.SetStateChanged((CBaseEntity)(object)capDead2.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
									foreach (string item in weaponList)
									{
										capDead2.GiveNamedItem(item);
									}
								}
								_invincibilityEndTime.Remove(revivedID);
							}, (TimerFlags?)null);
							capturedDead.PrintToCenterAlert("\ud83d\udc80 死灵法师在死亡地点复活了你！1秒无敌！");
							CCSPlayerController obj4 = capturedNecro;
							if (obj4 != null)
							{
								obj4.PrintToCenterAlert($"\ud83d\udc80 你复活了 {((CBasePlayerController)capturedDead).PlayerName}！-{reviveCost}HP");
							}
							Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Necromancer_revive"].Value.Replace("{necroName}", ((CBasePlayerController)capturedNecro).PlayerName).Replace("{deadName}", ((CBasePlayerController)capturedDead).PlayerName));
						}
					});
				}
			});
		});
		return (HookResult)0;
	}
}
