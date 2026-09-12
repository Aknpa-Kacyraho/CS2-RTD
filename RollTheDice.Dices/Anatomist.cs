using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Anatomist : DiceBlueprint
{
	public override string ClassName => "Anatomist";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Anatomist(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		CCSPlayerController victim = ResolvePlayer(entity);
		if (attacker == null || victim == null || attacker == victim)
		{
			return HookResult.Continue;
		}
		if (!_players.Contains(attacker) || ((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		float damage = info.Damage;
		if (info.GetHitGroup() == HitGroup_t.HITGROUP_HEAD)
		{
			info.Damage = damage * (1f + _config.Dices.Anatomist.HeadshotBonus);
		}
		else
		{
			info.Damage = damage * (1f - _config.Dices.Anatomist.BodyPenalty);
		}
		return HookResult.Changed;
	}

	private static CCSPlayerController ResolvePlayer(CBaseEntity entity)
	{
		if (entity == null)
		{
			return null;
		}
		CCSPlayerPawn pawn = ((NativeObject)entity).As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return null;
		}
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null || controller.Value == null)
		{
			return null;
		}
		return ((NativeObject)controller.Value).As<CCSPlayerController>();
	}
}
