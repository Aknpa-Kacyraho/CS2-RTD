using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 千钧 HighGravity：超高重力；高处落地砸出震波（范围伤害+减速）；免疫摔落伤害；
/// 保留少量常驻减伤。
/// </summary>
public class HighGravity : DiceBlueprint
{
	private readonly Dictionary<ulong, bool> _wasOnGround = new Dictionary<ulong, bool>();

	private readonly Dictionary<ulong, float> _fallSpeed = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _slowUntil = new Dictionary<ulong, float>();

	public override string ClassName => "HighGravity";

	public override List<string> Listeners => new List<string> { "OnTick", "OnPlayerTakeDamagePre" };

	public HighGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
		{
			return;
		}
		_players.Add(player);
		ChangePlayerGravity(player, _config.Dices.HighGravity.GravityScale);
		DamageReductionManager.Register(player, ClassName, _config.Dices.HighGravity.DamageReduction);
		ulong id = player.SteamID;
		_wasOnGround[id] = true;
		_fallSpeed[id] = 0f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		ChangePlayerGravity(player, 1f);
		DamageReductionManager.Unregister(player, ClassName);
		ulong id = player.SteamID;
		_wasOnGround.Remove(id);
		_fallSpeed.Remove(id);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			ChangePlayerGravity(player, 1f);
			DamageReductionManager.Unregister(player, ClassName);
		}
		_players.Clear();
		_wasOnGround.Clear();
		_fallSpeed.Clear();
		_slowUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		if (((uint)info.BitsDamageType & 0x20u) == 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerController victim = ResolvePlayer(entity);
		if (victim == null || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		info.Damage = 0f;
		return HookResult.Changed;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		HighGravityConfig cfg = _config.Dices.HighGravity;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
				if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
				{
					continue;
				}
				CBaseEntity entity = pawn;
				entity.ActualGravityScale = cfg.GravityScale;
				ulong id = player.SteamID;
				bool onGround = (entity.Flags & 1u) != 0;
				Vector velocity = entity.AbsVelocity;
				float vz = (velocity == null) ? 0f : velocity.Z;
				if (!onGround)
				{
					float prev = (_fallSpeed.TryGetValue(id, out float f) ? f : 0f);
					_fallSpeed[id] = Math.Max(prev, -vz);
				}
				else
				{
					bool was = (_wasOnGround.TryGetValue(id, out bool w) ? w : true);
					float fs = (_fallSpeed.TryGetValue(id, out float f2) ? f2 : 0f);
					if (!was && fs >= cfg.MinFallSpeed)
					{
						DoShock(player, pawn);
					}
					_fallSpeed[id] = 0f;
				}
				_wasOnGround[id] = onGround;
			}
			catch
			{
				_wasOnGround.Remove(player.SteamID);
				_fallSpeed.Remove(player.SteamID);
			}
		}
	}

	private void DoShock(CCSPlayerController holder, CCSPlayerPawn pawn)
	{
		HighGravityConfig cfg = _config.Dices.HighGravity;
		Vector origin = ((CBaseEntity)pawn).AbsOrigin;
		if (origin == null)
		{
			return;
		}
		List<CCSPlayerController> targets = Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			CCSPlayerPawn targetPawn = p?.PlayerPawn?.Value;
			return p != null && p.IsValid && !((CBasePlayerController)p).IsHLTV && p != holder && targetPawn != null && targetPawn.IsValid
				&& ((CBaseEntity)targetPawn).LifeState == 0 && ((CBaseEntity)p).TeamNum != ((CBaseEntity)holder).TeamNum;
		}).ToList();
		foreach (CCSPlayerController enemy in targets)
		{
			CCSPlayerPawn enemyPawn = enemy.PlayerPawn.Value;
			Vector enemyOrigin = ((CBaseEntity)enemyPawn).AbsOrigin;
			if (enemyOrigin == null || Vectors.GetDistance(origin, enemyOrigin) > cfg.ShockRadius)
			{
				continue;
			}
			CBaseEntity enemyEntity = enemyPawn;
			enemyEntity.Health -= cfg.ShockDamage;
			Utilities.SetStateChanged(enemyEntity, "CBaseEntity", "m_iHealth", 0);
			if (enemyEntity.Health <= 0)
			{
				try
				{
					((CBasePlayerPawn)enemyPawn).CommitSuicide(false, true);
				}
				catch
				{
					enemyEntity.Health = 0;
					Utilities.SetStateChanged(enemyEntity, "CBaseEntity", "m_iHealth", 0);
				}
			}
			ulong enemyId = enemy.SteamID;
			_slowUntil[enemyId] = Server.CurrentTime + cfg.ShockSlowSeconds;
			SpeedBonusManager.Register(enemy, ClassName, -cfg.ShockSlow);
			enemyPawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(enemy, 100f);
			Utilities.SetStateChanged(enemyPawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			new Timer(cfg.ShockSlowSeconds, (Action)delegate
			{
				RestoreSlow(enemyId);
			}, (TimerFlags?)null);
		}
		holder.PrintToCenterAlert("落地震波！");
	}

	private void RestoreSlow(ulong steamId)
	{
		if (_slowUntil.TryGetValue(steamId, out float until) && Server.CurrentTime < until - 0.05f)
		{
			return;
		}
		_slowUntil.Remove(steamId);
		SpeedBonusManager.UnregisterBySteamId(steamId, ClassName);
		CCSPlayerController player = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && ((CBasePlayerController)p).SteamID == steamId);
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffectiveBySteamId(steamId, 100f);
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private static void ChangePlayerGravity(CCSPlayerController player, float gravityScale)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			((CBaseEntity)pawn).ActualGravityScale = gravityScale;
		}
	}

	private static CCSPlayerController ResolvePlayer(CBaseEntity entity)
	{
		if (entity == null)
		{
			return null;
		}
		CCSPlayerPawn pawn = entity.As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return null;
		}
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null || controller.Value == null)
		{
			return null;
		}
		return controller.Value.As<CCSPlayerController>();
	}
}
