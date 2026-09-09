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

public class IceBeam : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, Timer> _frozenPlayers = new Dictionary<CCSPlayerController, Timer>();

	private readonly HashSet<ulong> _frozenSteamIDs = new HashSet<ulong>();

	public override string ClassName => "IceBeam";

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
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public IceBeam(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "PoisonBlade"))
			{
				DiceSynergy.AnnounceCombo(player, "霜毒双刃", "霜毒双刃联动生效！");
			}
			if (DiceSynergy.HasPartner(player, "Amber"))
			{
				DiceSynergy.AnnounceCombo(player, "极寒地狱", "冻结时间翻倍+琥珀概率翻倍！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"chance",
					(_config.Dices.IceBeam.FreezeChance * 100f).ToString("F0")
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		foreach (Timer value in _frozenPlayers.Values)
		{
			if (value != null)
			{
				value.Kill();
			}
		}
		_frozenPlayers.Clear();
		_frozenSteamIDs.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
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
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Expected O, but got Unknown
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)((CBasePlayerController)userid).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)userid).Pawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		if (_random.NextDouble() >= (double)_config.Dices.IceBeam.FreezeChance)
		{
			return (HookResult)0;
		}
		if (_frozenPlayers.ContainsKey(userid))
		{
			Timer obj = _frozenPlayers[userid];
			if (obj != null)
			{
				obj.Kill();
			}
		}
		_frozenSteamIDs.Add(((CBasePlayerController)userid).SteamID);
		CCSPlayerPawn value = userid.PlayerPawn.Value;
		value.VelocityModifier = _config.Dices.IceBeam.SlowAmount;
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		((CBaseModelEntity)value).Render = Color.FromArgb(255, 0, 255, 255);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
		MoveLockManager.Lock(userid, "IceBeam");
		userid.PrintToCenterAlert("❄ 冻结！无法移动和攻击！");
		CCSPlayerController capturedVictim = userid;
		_frozenPlayers[userid] = new Timer(DiceSynergy.HasPartner(attacker, "Amber") ? (_config.Dices.IceBeam.FreezeDuration * 2f) : (DiceSynergy.HasPartner(attacker, "PoisonBlade") || DiceSynergy.HasPartner(attacker, "Amber") ? (_config.Dices.IceBeam.FreezeDuration * 1.5f) : _config.Dices.IceBeam.FreezeDuration), (Action)delegate
		{
			MoveLockManager.Unlock(capturedVictim, "IceBeam");
			_frozenSteamIDs.Remove(((CBasePlayerController)capturedVictim).SteamID);
			CCSPlayerController obj2 = capturedVictim;
			if ((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)capturedVictim.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value2 = capturedVictim.PlayerPawn.Value;
				value2.VelocityModifier = 1f;
				((CBaseModelEntity)value2).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseModelEntity", "m_clrRender", 0);
			}
			_frozenPlayers.Remove(capturedVictim);
		}, (TimerFlags?)null);
		return (HookResult)0;
	}
}
