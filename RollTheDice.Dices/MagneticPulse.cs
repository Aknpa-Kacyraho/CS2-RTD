using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class MagneticPulse : DiceBlueprint
{
	private bool _hasThorns;

	private bool _hasRepulsionField;

	private readonly Dictionary<ulong, Dictionary<ulong, float>> _windowDamage = new Dictionary<ulong, Dictionary<ulong, float>>();

	private readonly Dictionary<ulong, float> _windowStart = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _slowEnd = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> _pendingPulse = new HashSet<ulong>();

	public override string ClassName => "MagneticPulse";

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

	public MagneticPulse(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_hasThorns = DiceSynergy.HasPartner(player, "Thorns");
			_hasRepulsionField = DiceSynergy.HasPartner(player, "RepulsionField");
			if (_hasThorns)
			{
				DiceSynergy.AnnounceCombo(player, "磁力荆棘", "魔镜反弹+磁力脉冲缴械！双重重压！");
			}
			if (_hasRepulsionField)
			{
				DiceSynergy.AnnounceCombo(player, "禁区", "斥力场弹回投掷物+磁力脉冲缴械！完全封锁远程！");
			}
			_windowDamage[((CBasePlayerController)player).SteamID] = new Dictionary<ulong, float>();
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
		CleanupPlayer(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			CleanupPlayer(((CBasePlayerController)item).SteamID);
		}
		_players.Clear();
		_windowDamage.Clear();
		_windowStart.Clear();
		_cooldownEnd.Clear();
		foreach (ulong sid in _slowEnd.Keys.ToList())
		{
			SpeedBonusManager.UnregisterBySteamId(sid, "MagneticPulse");
		}
		_slowEnd.Clear();
		_pendingPulse.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void CleanupPlayer(ulong steamID)
	{
		_windowDamage.Remove(steamID);
		_windowStart.Remove(steamID);
		_cooldownEnd.Remove(steamID);
		_pendingPulse.Remove(steamID);
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj3;
		if (attacker == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value2 = attacker.Value;
			if (value2 == null)
			{
				obj3 = null;
			}
			else
			{
				CCSPlayerPawn obj4 = ((NativeObject)value2).As<CCSPlayerPawn>();
				if (obj4 == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerController value3 = controller2.Value;
						obj3 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj3;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val2 == (CEntityInstance)(object)val)
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)val2).TeamNum == ((CBaseEntity)val).TeamNum)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)val).SteamID;
		ulong steamID2 = ((CBasePlayerController)val2).SteamID;
		float num = Server.CurrentTime;
		if (_cooldownEnd.TryGetValue(steamID, out var value4) && num < value4)
		{
			return (HookResult)0;
		}
		float num2 = ((DiceSynergy.HasPartner(val, "Thorns") || DiceSynergy.HasPartner(val, "RepulsionField")) ? (_config.Dices.MagneticPulse.DamageWindow * 0.5f) : _config.Dices.MagneticPulse.DamageWindow);
		if (!_windowStart.TryGetValue(steamID, out var value5) || num - value5 > num2)
		{
			_windowDamage[steamID] = new Dictionary<ulong, float>();
			_windowStart[steamID] = num;
		}
		if (!_windowDamage.TryGetValue(steamID, out Dictionary<ulong, float> value6))
		{
			value6 = new Dictionary<ulong, float>();
			_windowDamage[steamID] = value6;
		}
		if (value6.ContainsKey(steamID2))
		{
			return (HookResult)0;
		}
		value6[steamID2] = info.Damage;
		float num3 = 0f;
		foreach (float value7 in value6.Values)
		{
			num3 += value7;
		}
		float num4 = ((DiceSynergy.HasPartner(val, "Thorns") || DiceSynergy.HasPartner(val, "RepulsionField")) ? (_config.Dices.MagneticPulse.DamageThreshold * 0.5f) : _config.Dices.MagneticPulse.DamageThreshold);
		if (num3 >= num4)
		{
			_pendingPulse.Add(steamID);
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		float num = Server.CurrentTime;
		foreach (ulong sid in _pendingPulse.ToList())
		{
			_pendingPulse.Remove(sid);
			CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == sid);
			CCSPlayerController obj = player;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				CHandle<CCSPlayerPawn> playerPawn = obj.PlayerPawn;
				if (playerPawn == null)
				{
					obj2 = null;
				}
				else
				{
					CCSPlayerPawn value = playerPawn.Value;
					obj2 = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
				}
			}
			if (obj2 == null)
			{
				continue;
			}
			float num2 = ((DiceSynergy.HasPartner(player, "Thorns") || DiceSynergy.HasPartner(player, "RepulsionField")) ? (_config.Dices.MagneticPulse.Cooldown * 0.5f) : _config.Dices.MagneticPulse.Cooldown);
			_cooldownEnd[sid] = num + num2;
			_windowDamage.Remove(sid);
			_windowStart.Remove(sid);
			Vector absOrigin = ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin;
			float radius = _config.Dices.MagneticPulse.Radius;
			float num3 = (DiceSynergy.HasPartner(player, "RepulsionField") ? (_config.Dices.MagneticPulse.SlowAmount * 1.5f) : _config.Dices.MagneticPulse.SlowAmount);
			float num4 = (DiceSynergy.HasPartner(player, "Thorns") ? (_config.Dices.MagneticPulse.SlowDuration * 1.5f) : _config.Dices.MagneticPulse.SlowDuration);
			SpawnPulseVisual(absOrigin);
			((CBaseEntity)player).EmitSound("CSGO_EMP.Impact", (RecipientFilter)null, 1f, 0f);
			player.PrintToCenterAlert("⚡ 磁力脉冲释放！");
			int num5 = 0;
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
				select p)
			{
				float num6 = absOrigin.X - ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin.X;
				float num7 = absOrigin.Y - ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin.Y;
				float num8 = absOrigin.Z - ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin.Z;
				float num9 = MathF.Sqrt(num6 * num6 + num7 * num7 + num8 * num8);
				if (num9 > radius)
				{
					continue;
				}
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
				CHandle<CBasePlayerWeapon> val = ((weaponServices != null) ? weaponServices.ActiveWeapon : null);
				if ((CEntityInstance)(object)val?.Value != (CEntityInstance)null && ((CEntityInstance)val.Value).IsValid)
				{
					string text = ((CEntityInstance)val.Value).DesignerName ?? "";
					if (!text.Contains("knife") && !text.Contains("bayonet") && !text.Contains("c4"))
					{
						item.DropActiveWeapon();
						num5++;
					}
				}
				SpeedBonusManager.Register(item, "MagneticPulse", 0f - num3);
				_slowEnd[((CBasePlayerController)item).SteamID] = num + num4;
				item.PrintToCenterAlert($"⚡ 磁力脉冲！缴械+减速{(int)(num3 * 100f)}% {num4:F0}秒！");
			}
			if (num5 > 0)
			{
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚡ {((CBasePlayerController)player).PlayerName} 释放磁力脉冲！缴械{num5}名敌人！");
			}
		}
		List<ulong> list = new List<ulong>();
		foreach (KeyValuePair<ulong, float> kv in _slowEnd.ToList())
		{
			if (num >= kv.Value)
			{
				CCSPlayerController val2 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == kv.Key);
				if ((CEntityInstance)(object)val2 != (CEntityInstance)null)
				{
					SpeedBonusManager.Unregister(val2, "MagneticPulse");
					if ((CEntityInstance)(object)val2.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)val2.PlayerPawn.Value).IsValid && ((CBaseEntity)val2.PlayerPawn.Value).LifeState == 0)
					{
						float effectiveNow = SpeedBonusManager.GetEffective(val2);
						val2.PlayerPawn.Value.VelocityModifier = 1f + effectiveNow;
						Utilities.SetStateChanged((CBaseEntity)(object)val2.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}
				else
				{
					SpeedBonusManager.UnregisterBySteamId(kv.Key, "MagneticPulse");
				}
				list.Add(kv.Key);
				continue;
			}
			CCSPlayerController val3 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == kv.Key && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0);
			if ((CEntityInstance)(object)val3 != (CEntityInstance)null)
			{
				float effective = SpeedBonusManager.GetEffective(val3);
				val3.PlayerPawn.Value.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)val3.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
		foreach (ulong item2 in list)
		{
			_slowEnd.Remove(item2);
		}
	}

	private void SpawnPulseVisual(Vector pos)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_0083: Expected O, but got Unknown
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Expected O, but got Unknown
		//IL_0201: Expected O, but got Unknown
		//IL_0201: Expected O, but got Unknown
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		CParticleSystem val = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		if ((CEntityInstance)(object)val != (CEntityInstance)null)
		{
			val.EffectName = "particles/ui/ui_experience_award_innerpoint.vpcf";
			val.StartActive = true;
			((CBaseEntity)val).Teleport(pos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			((CBaseEntity)val).DispatchSpawn();
			CParticleSystem captured = val;
			new Timer(1.5f, (Action)delegate
			{
				if ((CEntityInstance)(object)captured != (CEntityInstance)null && ((CEntityInstance)captured).IsValid)
				{
					((CEntityInstance)captured).Remove();
				}
			}, (TimerFlags?)null);
		}
		int num = 12;
		float num2 = 60f;
		for (int num3 = 0; num3 < num; num3++)
		{
			float x = (float)num3 * 2f * (float)Math.PI / (float)num;
			float x2 = (float)(num3 + 1) * 2f * (float)Math.PI / (float)num;
			float value = pos.X + num2 * MathF.Cos(x);
			float value2 = pos.Y + num2 * MathF.Sin(x);
			float num4 = pos.X + num2 * MathF.Cos(x2);
			float num5 = pos.Y + num2 * MathF.Sin(x2);
			CBeam val2 = Utilities.CreateEntityByName<CBeam>("beam");
			if ((CEntityInstance)(object)val2 == (CEntityInstance)null)
			{
				continue;
			}
			((CBaseModelEntity)val2).Render = Color.FromArgb(200, 30, 144, 255);
			val2.Width = 3f;
			((CBaseEntity)val2).Teleport(new Vector((float?)value, (float?)value2, (float?)(pos.Z + 30f)), new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			val2.EndPos.X = num4;
			val2.EndPos.Y = num5;
			val2.EndPos.Z = pos.Z + 30f;
			((CBaseEntity)val2).DispatchSpawn();
			CBeam capturedBeam = val2;
			new Timer(1f, (Action)delegate
			{
				if ((CEntityInstance)(object)capturedBeam != (CEntityInstance)null && ((CEntityInstance)capturedBeam).IsValid)
				{
					((CEntityInstance)capturedBeam).Remove();
				}
			}, (TimerFlags?)null);
		}
	}
}
