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

public class Satellite : DiceBlueprint
{
	private bool _comboActive;

	public override string ClassName => "Satellite";

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

	public Satellite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Drone");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "天网", "无人机伤害+50%！卫星浮空！");
			}
			float gravity = _config.Dices.Satellite.Gravity;
			((CBaseEntity)player.PlayerPawn.Value).GravityScale = gravity;
			((CBaseEntity)player.PlayerPawn.Value).ActualGravityScale = gravity;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flGravityScale", 0);
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
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseEntity)player.PlayerPawn.Value).ActualGravityScale = 1f;
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				((CBaseEntity)item.PlayerPawn.Value).ActualGravityScale = 1f;
			}
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value = item.PlayerPawn.Value;
				float num = _config.Dices.Satellite.Gravity * (DiceSynergy.HasPartner(item, "Drone") ? 0.3f : 1f);
				((CBaseEntity)value).GravityScale = num;
				((CBaseEntity)value).ActualGravityScale = num;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flGravityScale", 0);
			}
		}
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid))
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
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
				obj = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
			}
		}
		CBasePlayerWeapon val = (CBasePlayerWeapon)obj;
		if (val == null)
		{
			return (HookResult)0;
		}
		CCSWeaponBase val2 = ((NativeObject)val).As<CCSWeaponBase>();
		val2.AccuracyPenalty = 0f;
		val2.FlRecoilIndex = 0f;
		return (HookResult)0;
	}
}
