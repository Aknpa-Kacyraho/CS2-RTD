using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class NoRecoil : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "NoRecoil";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventWeaponFire";
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

	public NoRecoil(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		player.ReplicateConVar("weapon_accuracy_nospread", "0");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid) || userid.PlayerPawn == null || !userid.PlayerPawn.IsValid || (CEntityInstance)(object)userid.PlayerPawn.Value == (CEntityInstance)null || ((CBasePlayerPawn)userid.PlayerPawn.Value).WeaponServices == null || ((CBasePlayerPawn)userid.PlayerPawn.Value).WeaponServices.ActiveWeapon == null || !((CBasePlayerPawn)userid.PlayerPawn.Value).WeaponServices.ActiveWeapon.IsValid || (CEntityInstance)(object)((CBasePlayerPawn)userid.PlayerPawn.Value).WeaponServices.ActiveWeapon.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		CBasePlayerWeapon value = ((CBasePlayerPawn)userid.PlayerPawn.Value).WeaponServices.ActiveWeapon.Value;
		ApplyNoRecoil(userid);
		((NativeObject)value).As<CCSWeaponBase>().FlRecoilIndex = 0f;
		((NativeObject)value).As<CCSWeaponBase>().AccuracyPenalty = 0f;
		return (HookResult)0;
	}

	private static void ApplyNoRecoil(CCSPlayerController? player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || player.PlayerPawn.Value.AimPunchServices == null)
		{
			return;
		}
		player.ReplicateConVar("weapon_accuracy_nospread", "1");
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)player.PlayerPawn.Value).WeaponServices;
		CBasePlayerWeapon val = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return;
		}
		CCSWeaponBase val2 = ((NativeObject)val).As<CCSWeaponBase>();
		if (!((CEntityInstance)(object)val2 == (CEntityInstance)null))
		{
			string text = ((CEntityInstance)val2).DesignerName.ToLower(CultureInfo.CurrentCulture);
			if (!text.Contains("mag7") && !text.Contains("nova") && !text.Contains("sawedoff") && !text.Contains("xm1014"))
			{
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.X = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Y = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Z = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.X = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Y = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Z = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.X = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Y = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Z = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.X = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Y = 0f;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Z = 0f;
				val2.AccuracyPenalty = 0f;
				player.PlayerPawn.Value.AimPunchServices.PredictableBaseTick = -1;
				player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseTick = -1;
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		info.Damage *= _config.Dices.NoRecoil.DamageMultiplier;
		return (HookResult)1;
	}
}
