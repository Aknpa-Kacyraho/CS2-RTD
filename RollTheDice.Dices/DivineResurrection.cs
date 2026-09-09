using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class DivineResurrection : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "DivineResurrection";

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

	public DivineResurrection(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
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
		_cooldowns.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_cooldowns.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)attacker.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)attacker.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		if (_cooldowns.TryGetValue(attacker, out var value) && Server.CurrentTime < value)
		{
			return (HookResult)0;
		}
		if (_random.NextDouble() >= (double)_config.Dices.DivineResurrection.Chance)
		{
			return (HookResult)0;
		}
		CCSPlayerController val = (from _ in Utilities.GetPlayers()
			where (CEntityInstance)(object)_ != (CEntityInstance)(object)attacker && ((CEntityInstance)_).IsValid && ((CBaseEntity)_).TeamNum == ((CBaseEntity)attacker).TeamNum && (CEntityInstance)(object)((CBasePlayerController)_).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)_).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)_).Pawn.Value).LifeState != 0
			orderby _random.Next()
			select _).FirstOrDefault();
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		CCSPlayerController capturedAttacker = attacker;
		CCSPlayerController capturedDead = val;
		List<string> weaponList = new List<string>();
		CHandle<CCSPlayerPawn> playerPawn = attacker.PlayerPawn;
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
			foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)attacker.PlayerPawn.Value).WeaponServices.MyWeapons)
			{
				if ((CEntityInstance)(object)myWeapon?.Value != (CEntityInstance)null && ((CEntityInstance)myWeapon.Value).IsValid && ((CEntityInstance)myWeapon.Value).DesignerName != null)
				{
					string designerName = ((CEntityInstance)myWeapon.Value).DesignerName;
					if (!designerName.Contains("knife") && !designerName.Contains("c4"))
					{
						weaponList.Add(designerName);
					}
				}
			}
		}
		_cooldowns[attacker] = Server.CurrentTime + _config.Dices.DivineResurrection.Cooldown;
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
						//IL_0089: Unknown result type (might be due to invalid IL or missing references)
						//IL_008f: Invalid comparison between Unknown and I4
						//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
						//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
						//IL_00b0: Expected O, but got Unknown
						CCSPlayerController obj3 = capturedDead;
						if (!((CEntityInstance)(object)((obj3 == null) ? null : obj3.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState == 0)
						{
							CCSPlayerPawn value3 = capturedDead.PlayerPawn.Value;
							value3.ArmorValue = 100;
							capturedDead.RemoveWeapons();
							capturedDead.GiveNamedItem("weapon_knife");
							if ((int)capturedDead.Team == 3)
							{
								CCSPlayer_ItemServices val2 = new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value3).ItemServices).Handle);
								val2.HasDefuser = true;
								CCSPlayer_ItemServices val3 = val2;
							}
							if (weaponList.Count > 0)
							{
								foreach (string item in weaponList)
								{
									capturedDead.GiveNamedItem(item);
								}
							}
							else
							{
								capturedDead.GiveNamedItem("weapon_ak47");
								capturedDead.GiveNamedItem("weapon_deagle");
							}
							string value4 = _localizer["command.prefix"].Value;
							capturedDead.PrintToChat(value4 + _localizer["dice_DivineResurrection_revived"].Value);
							capturedAttacker.PrintToChat(value4 + _localizer["dice_DivineResurrection_reviver"].Value.Replace("{player}", ((CBasePlayerController)capturedDead).PlayerName));
						}
					});
				}
			});
		});
		return (HookResult)0;
	}
}
