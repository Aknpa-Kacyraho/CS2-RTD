using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Phoenix : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _phoenixEndTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _phoenixGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();

	private readonly Dictionary<CCSPlayerController, bool> _phoenixExploded = new Dictionary<CCSPlayerController, bool>();

	private readonly Dictionary<CCSPlayerController, float> _cooldownEnd = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Phoenix";


	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerTakeDamagePre";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public Phoenix(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_phoenixExploded[player] = false;
			_cooldownEnd[player] = 0f;
			RollTheDice.LogDebug($"[Phoenix] Add: steamid={((CBasePlayerController)player).SteamID} players={_players.Count}\n");
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DeactivatePhoenix(player);
		_players.Remove(player);
		_phoenixEndTime.Remove(player);
		_phoenixGlows.Remove(player);
		_phoenixExploded.Remove(player);
		_cooldownEnd.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DeactivatePhoenix(item);
		}
		_players.Clear();
		_phoenixEndTime.Clear();
		_phoenixGlows.Clear();
		_phoenixExploded.Clear();
		_cooldownEnd.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void DeactivatePhoenix(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		CCSPlayerPawn val = player.PlayerPawn?.Value;
		if (val != null && ((CEntityInstance)val).IsValid && ((CBaseEntity)val).LifeState == 0)
		{
			((CBaseEntity)val).MoveType = (MoveType_t)2;
			Schema.SetSchemaValue<int>(((NativeEntity)val).Handle, "CBaseEntity", "m_nActualMoveType", 2);
			((CBaseEntity)val).ActualGravityScale = 1f;
			if (_phoenixGlows.TryGetValue(player, out (CDynamicProp, CDynamicProp) value))
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)value.Item1, (CBaseEntity?)(object)value.Item2);
				_phoenixGlows.Remove(player);
			}
		}
	}

	public void TriggerPhoenixRevive(CCSPlayerController player)
	{
		CCSPlayerPawn val = ((player == null) ? null : player.PlayerPawn?.Value);
		RollTheDice.LogDebug($"[Phoenix] TriggerPhoenixRevive: pawnValid={(val != null && ((CEntityInstance)val).IsValid)}\n");
		if (val != null && ((CEntityInstance)val).IsValid)
		{
			float num = Server.CurrentTime;
			float invulDuration = _config.Dices.Phoenix.InvulDuration;
			_phoenixEndTime[player] = num + invulDuration;
			_cooldownEnd[player] = num + 60f;
			((CBaseEntity)val).MoveType = (MoveType_t)0;
			Schema.SetSchemaValue<int>(((NativeEntity)val).Handle, "CBaseEntity", "m_nActualMoveType", 0);
			((CBaseEntity)val).ActualGravityScale = 0.1f;
			_phoenixGlows[player] = GlowUtil.CreateGlow((CBaseEntity)(object)val, Color.Gold);
			player.PrintToCenterAlert($"\ud83d\udd25 菲尼克斯涅槃！{invulDuration}s无敌！");
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udd25 {((CBasePlayerController)player).PlayerName} 触发菲尼克斯！涅槃重生！");
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
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
		if (_players.Count > 0)
		{
			bool valOk = val != null && ((CEntityInstance)val).IsValid;
			bool contains = valOk && _players.Contains(val);
			string cool = (valOk && _cooldownEnd.TryGetValue(val, out var _c)) ? _c.ToString("F2") : "?";
			RollTheDice.LogDebug($"[Phoenix] dmg: players={_players.Count} valid={valOk} contains={contains} hp={(valOk ? entity.Health : -1)} dmg={info.Damage} coolEnd={cool}\n");
		}
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (_phoenixEndTime.TryGetValue(val, out var value2) && num < value2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		if (_cooldownEnd.TryGetValue(val, out var value3) && num < value3)
		{
			return (HookResult)0;
		}
		int num2 = entity.Health - (int)float.Round(info.Damage);
		if (num2 > 0)
		{
			RollTheDice.LogDebug($"[Phoenix] notLethal: hp={entity.Health} dmg={info.Damage} -> continue\n");
			return (HookResult)0;
		}
		RollTheDice.LogDebug($"[Phoenix] LETHAL -> revive: hp={entity.Health} dmg={info.Damage}\n");
		info.Damage = 0f;
		_phoenixExploded[val] = false;
		CCSPlayerPawn val2 = ((NativeObject)entity).As<CCSPlayerPawn>();
		if ((CEntityInstance)(object)val2 != (CEntityInstance)null)
		{
			((CBaseEntity)val2).Health = Math.Max(((CBaseEntity)val2).Health, 1);
			Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
		}
		CCSPlayerController capturedVictim = val;
		Server.NextFrame((Action)delegate
		{
			TriggerPhoenixRevive(capturedVictim);
		});
		return (HookResult)1;
	}

	public void OnTick()
	{
		//IL_0567: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Expected O, but got Unknown
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Expected O, but got Unknown
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Expected O, but got Unknown
		//IL_0249: Expected O, but got Unknown
		float num = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, float> item in _phoenixEndTime.ToList())
		{
			CCSPlayerController player = item.Key;
			float value = item.Value;
			if (num >= value)
			{
				DeactivatePhoenix(player);
				_phoenixEndTime.Remove(player);
				if (!_phoenixExploded.TryGetValue(player, out var value2) || value2)
				{
					continue;
				}
				_phoenixExploded[player] = true;
				CCSPlayerController obj = player;
				CCSPlayerPawn val = ((obj == null) ? null : obj.PlayerPawn?.Value);
				if (val == null || !((CEntityInstance)val).IsValid || ((CBaseEntity)val).LifeState != 0)
				{
					continue;
				}
				((CBaseEntity)val).Health = _config.Dices.Phoenix.HealHP;
				((CBaseEntity)val).MaxHealth = _config.Dices.Phoenix.HealHP;
				val.ArmorValue = _config.Dices.Phoenix.HealArmor;
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseEntity", "m_iHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)val, "CCSPlayerPawn", "m_ArmorValue", 0);
				player.GiveNamedItem("weapon_ak47");
				Vector absOrigin = ((CBaseEntity)val).AbsOrigin;
				if (absOrigin != null)
				{
					float explosionRadius = _config.Dices.Phoenix.ExplosionRadius;
					int explosionDamage = _config.Dices.Phoenix.ExplosionDamage;
					Vector val2 = absOrigin;
					CBaseEntity val3 = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
					if ((CEntityInstance)(object)val3 != (CEntityInstance)null)
					{
						val3.Teleport(val2, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
						val3.DispatchSpawn();
						((CEntityInstance)val3).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
					}
					foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
						where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0 && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).AbsOrigin != null
						select p)
					{
						float distance = Vectors.GetDistance(val2, ((CBaseEntity)((CBasePlayerController)item2).Pawn.Value).AbsOrigin);
						if (!(distance <= explosionRadius))
						{
							continue;
						}
						float num2 = 1f - distance / explosionRadius;
						int num3 = (int)float.Round((float)explosionDamage * num2);
						((CBaseEntity)((CBasePlayerController)item2).Pawn.Value).Health -= num3;
						Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)item2).Pawn.Value, "CBaseEntity", "m_iHealth", 0);
						if (((CBaseEntity)((CBasePlayerController)item2).Pawn.Value).Health > 0)
						{
							continue;
						}
						if (!item2.IsBot && !((CBasePlayerController)item2).IsHLTV)
						{
							((CBasePlayerController)item2).Pawn.Value.CommitSuicide(false, true);
							continue;
						}
						try
						{
							((CBasePlayerController)item2).Pawn.Value.CommitSuicide(false, true);
						}
						catch
						{
							((CBaseEntity)((CBasePlayerController)item2).Pawn.Value).Health = 0;
							Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)item2).Pawn.Value, "CBaseEntity", "m_iHealth", 0);
						}
					}
				}
				player.PrintToCenterAlert("\ud83d\udd25 菲尼克斯涅槃完成！444HP/444甲/AK！");
				Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udd25 {((CBasePlayerController)player).PlayerName} 菲尼克斯涅槃重生！");
				continue;
			}
			CCSPlayerController obj3 = player;
			CCSPlayerPawn val4 = ((obj3 == null) ? null : obj3.PlayerPawn?.Value);
			if (val4 != null && ((CEntityInstance)val4).IsValid)
			{
				if ((int)((CBaseEntity)val4).MoveType != 0)
				{
					((CBaseEntity)val4).MoveType = (MoveType_t)0;
					Schema.SetSchemaValue<int>(((NativeEntity)val4).Handle, "CBaseEntity", "m_nActualMoveType", 0);
				}
				if (((CBaseEntity)val4).ActualGravityScale > 0.15f)
				{
					((CBaseEntity)val4).ActualGravityScale = 0.1f;
				}
				if (((CBaseEntity)val4).AbsOrigin != null)
				{
					float num4 = _config.Dices.Phoenix.FloatSpeed * Server.TickInterval;
					Vector val5 = new Vector((float?)((CBaseEntity)val4).AbsOrigin.X, (float?)((CBaseEntity)val4).AbsOrigin.Y, (float?)(((CBaseEntity)val4).AbsOrigin.Z + num4));
					((CBaseEntity)val4).Teleport(val5, ((CBaseEntity)val4).AbsRotation, new Vector((float?)0f, (float?)0f, (float?)(num4 / Server.TickInterval)));
				}
			}
		}
	}
}
