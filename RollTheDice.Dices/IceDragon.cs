using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class IceDragon : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalArmor = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<ulong, float> _frozenUntil = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> _frozenSteamIDs = new HashSet<ulong>();

	public override string ClassName => "IceDragon";


	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerHurt";
			return list;
		}
	}

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public IceDragon(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			_originalArmor[player] = value.ArmorValue;
			((CBaseEntity)value).MaxHealth = _config.Dices.IceDragon.BonusHP;
			((CBaseEntity)value).Health = _config.Dices.IceDragon.BonusHP;
			value.ArmorValue = _config.Dices.IceDragon.BonusArmor;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
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
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
			if (_originalArmor.TryGetValue(player, out var value3))
			{
				value.ArmorValue = Math.Min(value.ArmorValue, value3);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				_originalArmor.Remove(player);
			}
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
		_originalArmor.Clear();
		_frozenUntil.Clear();
		_frozenSteamIDs.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
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
		if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && _frozenSteamIDs.Contains(((CBasePlayerController)val).SteamID))
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)((CBasePlayerController)userid).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)userid).Pawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float val = Server.CurrentTime;
		float freezeDuration = _config.Dices.IceDragon.FreezeDuration;
		float valueOrDefault = _frozenUntil.GetValueOrDefault(((CBasePlayerController)userid).SteamID, 0f);
		float value = Math.Max(val, valueOrDefault) + freezeDuration;
		_frozenUntil[((CBasePlayerController)userid).SteamID] = value;
		_frozenSteamIDs.Add(((CBasePlayerController)userid).SteamID);
		CCSPlayerPawn value2 = userid.PlayerPawn.Value;
		((CBaseModelEntity)value2).Render = Color.FromArgb(255, 100, 200, 255);
		Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseModelEntity", "m_clrRender", 0);
		MoveLockManager.Lock(userid, "IceDragon");
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_frozenUntil.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<ulong, float> item in _frozenUntil.ToList())
		{
			var (steamId, num4) = item;
			if (num >= num4)
			{
				_frozenUntil.Remove(steamId);
				_frozenSteamIDs.Remove(steamId);
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == steamId);
				CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
				if (val2 != null && ((CEntityInstance)val2).IsValid)
				{
					MoveLockManager.Unlock(val, "IceDragon");
					((CBaseModelEntity)val2).Render = Color.FromArgb(255, 255, 255, 255);
					Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseModelEntity", "m_clrRender", 0);
				}
			}
			else
			{
				CCSPlayerController val3 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == steamId);
				CCSPlayerPawn val4 = ((val3 == null) ? null : val3.PlayerPawn?.Value);
				if (val4 != null && ((CEntityInstance)val4).IsValid)
				{
					((CBaseModelEntity)val4).Render = Color.FromArgb(255, 100, 200, 255);
					Utilities.SetStateChanged((CBaseEntity)(object)val4, "CBaseModelEntity", "m_clrRender", 0);
					MoveLockManager.Lock(val3, "IceDragon");
				}
			}
		}
	}
}
