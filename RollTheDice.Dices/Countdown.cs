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

public class Countdown : DiceBlueprint
{
	private class CountdownState
	{
		public float EndTime;

		public Vector SpawnPosition = null;

		public QAngle SpawnAngles = null;

		public List<string> OriginalWeapons = new List<string>();

		public int OriginalHP;

		public int OriginalArmor;

		public bool OriginalHelmet;

		public bool Triggered;

		public bool ComboActive;
	}

	private readonly Dictionary<CCSPlayerController, CountdownState> _states = new Dictionary<CCSPlayerController, CountdownState>();

	public override string ClassName => "Countdown";

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

	public Countdown(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected O, but got Unknown
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		CCSPlayerPawn value = player.PlayerPawn.Value;
		List<string> list = new List<string>();
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
		if (((weaponServices != null) ? weaponServices.MyWeapons : null) != null)
		{
			foreach (CHandle<CBasePlayerWeapon> myWeapon in ((CBasePlayerPawn)value).WeaponServices.MyWeapons)
			{
				object obj;
				if (myWeapon == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value2 = myWeapon.Value;
					obj = ((value2 != null) ? ((CEntityInstance)value2).DesignerName : null);
				}
				if (obj != null)
				{
					string designerName = ((CEntityInstance)myWeapon.Value).DesignerName;
					if (!designerName.Contains("knife") && !designerName.Contains("bayonet"))
					{
						list.Add(designerName);
					}
				}
			}
		}
		int armorValue = value.ArmorValue;
		bool originalHelmet = ((CBasePlayerPawn)value).ItemServices != null && new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value).ItemServices).Handle).HasHelmet;
		CountdownState countdownState = new CountdownState
		{
			EndTime = Server.CurrentTime + _config.Dices.Countdown.Countdown,
			SpawnPosition = new Vector((float?)((CBaseEntity)value).AbsOrigin.X, (float?)((CBaseEntity)value).AbsOrigin.Y, (float?)((CBaseEntity)value).AbsOrigin.Z),
			SpawnAngles = new QAngle((float?)((CBaseEntity)value).AbsRotation.X, (float?)((CBaseEntity)value).AbsRotation.Y, (float?)((CBaseEntity)value).AbsRotation.Z),
			OriginalWeapons = list,
			OriginalHP = ((CBaseEntity)value).Health,
			OriginalArmor = armorValue,
			OriginalHelmet = originalHelmet,
			Triggered = false
		};
		_players.Add(player);
		if (countdownState.ComboActive = DiceSynergy.HasPartner(player, "Rewind"))
		{
			DiceSynergy.AnnounceCombo(player, "时空主宰", "时空主宰联动生效！");
		}
		_states[player] = countdownState;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_states.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_states.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected O, but got Unknown
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Invalid comparison between Unknown and I4
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		if (_states.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, CountdownState> item in _states.ToList())
		{
			CCSPlayerController key = item.Key;
			CountdownState value = item.Value;
			if (value.Triggered)
			{
				continue;
			}
			float num2 = value.EndTime - num;
			if (num2 > 0f)
			{
				if (Server.TickCount % 64 == 0)
				{
					int value2 = (int)Math.Ceiling(num2);
					if (key != null)
					{
						key.PrintToCenterAlert($"倒计时: {value2}秒");
					}
				}
				continue;
			}
			value.Triggered = true;
			if ((CEntityInstance)(object)key == (CEntityInstance)null || !((CEntityInstance)key).IsValid || (CEntityInstance)(object)key.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)key.PlayerPawn.Value).IsValid)
			{
				_states.Remove(key);
				continue;
			}
			if (((CBaseEntity)key.PlayerPawn.Value).LifeState != 0)
			{
				_states.Remove(key);
				continue;
			}
			CCSPlayerPawn value3 = key.PlayerPawn.Value;
			int num3 = (value.ComboActive ? (Math.Max(value.OriginalHP, 100) + 50) : Math.Max(value.OriginalHP, 100));
			((CBaseEntity)value3).Teleport(value.SpawnPosition, value.SpawnAngles, new Vector((float?)0f, (float?)0f, (float?)0f));
			((CBaseEntity)value3).Health = num3;
			((CBaseEntity)value3).MaxHealth = Math.Max(((CBaseEntity)value3).MaxHealth, num3);
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iMaxHealth", 0);
			key.RemoveWeapons();
			key.GiveNamedItem("weapon_knife");
			if (value.OriginalWeapons.Count > 0)
			{
				foreach (string originalWeapon in value.OriginalWeapons)
				{
					key.GiveNamedItem(originalWeapon);
				}
			}
			value3.ArmorValue = ((value.OriginalArmor > 0) ? value.OriginalArmor : 100);
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CCSPlayerPawn", "m_ArmorValue", 0);
			if (value.OriginalHelmet && ((CBasePlayerPawn)value3).ItemServices != null)
			{
				new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value3).ItemServices).Handle).HasHelmet = true;
			}
			if ((int)key.Team == 3 && ((CBasePlayerPawn)value3).ItemServices != null)
			{
				new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value3).ItemServices).Handle).HasDefuser = true;
			}
			key.PrintToCenterAlert("倒计时结束！已回溯至出生点！满血满甲！");
			_states.Remove(key);
			_players.Remove(key);
		}
	}
}
