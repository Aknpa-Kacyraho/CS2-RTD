using System;
using System.Collections.Generic;
using System.Drawing;
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
/// 附骨之疽 BoneMaggot：命中敌人使其被标记（隔墙可见）；被标记目标受到额外伤害；击杀标记目标回血并补一颗手雷。
/// </summary>
public class BoneMaggot : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _markedVictims = new Dictionary<ulong, float>();

	public override string ClassName => "BoneMaggot";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public BoneMaggot(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_markedVictims.Clear();
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
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		CCSPlayerController victim = ResolvePlayer(entity);
		if (attacker == null || !attacker.IsValid || !_players.Contains(attacker) || victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || ((CBaseEntity)victim).TeamNum == ((CBaseEntity)attacker).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid)
		{
			return HookResult.Continue;
		}
		BoneMaggotConfig cfg = _config.Dices.BoneMaggot;
		float now = Server.CurrentTime;
		ulong victimId = victim.SteamID;
		bool marked = _markedVictims.TryGetValue(victimId, out float until) && now < until;
		if (!marked)
		{
			_markedVictims[victimId] = now + cfg.MarkDuration;
			MarkVictim(victim, victimPawn, cfg.MarkDuration);
			return HookResult.Continue;
		}
		info.Damage *= 1f + cfg.MarkDamageBonus;
		return HookResult.Changed;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		ulong victimId = victim.SteamID;
		if (!_markedVictims.TryGetValue(victimId, out float until) || Server.CurrentTime >= until)
		{
			return HookResult.Continue;
		}
		_markedVictims.Remove(victimId);
		BoneMaggotConfig cfg = _config.Dices.BoneMaggot;
		CCSPlayerPawn pawn = attacker.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			CBaseEntity attackerEntity = pawn;
			attackerEntity.Health = Math.Min(attackerEntity.Health + cfg.KillHeal, attackerEntity.MaxHealth);
			Utilities.SetStateChanged(attackerEntity, "CBaseEntity", "m_iHealth", 0);
		}
		attacker.GiveNamedItem("weapon_hegrenade");
		attacker.PrintToCenterAlert($"🐛 击杀标记目标！+{cfg.KillHeal} HP +手雷");
		return HookResult.Continue;
	}

	private void MarkVictim(CCSPlayerController victim, CCSPlayerPawn pawn, float duration)
	{
		(CDynamicProp Proxy, CDynamicProp Glow) glow = GlowUtil.CreateGlow(pawn, Color.FromArgb(255, 50, 255, 50));
		if (glow.Glow != null && glow.Glow.IsValid)
		{
			glow.Glow.Glow.GlowType = 3;
			glow.Glow.Glow.GlowRange = 5000;
			glow.Glow.Glow.GlowRangeMin = 0;
		}
		CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		if (particle != null && particle.IsValid)
		{
			particle.EffectName = "particles/critters/chicken/chicken_impact_burst_zombie.vpcf";
			particle.Teleport(((CBaseEntity)pawn).AbsOrigin, null, null);
			particle.StartActive = true;
			particle.DispatchSpawn();
			new Timer(2f, delegate
			{
				if (particle != null && particle.IsValid)
				{
					particle.Remove();
				}
			}, (TimerFlags?)null);
		}
		victim.PrintToCenterAlert("🐛 你被标记了！");
		new Timer(duration, delegate
		{
			if (glow.Proxy != null && glow.Proxy.IsValid)
			{
				glow.Proxy.Remove();
			}
			if (glow.Glow != null && glow.Glow.IsValid)
			{
				glow.Glow.Remove();
			}
		}, (TimerFlags?)null);
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
