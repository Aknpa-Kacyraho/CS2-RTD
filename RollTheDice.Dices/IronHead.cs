using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class IronHead : DiceBlueprint
{
	public override string ClassName => "IronHead";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public IronHead(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_players.Count == 0 || info == null || info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CCSPlayerController victim = ResolvePlayer(entity);
		if (victim == null || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		if (info.GetHitGroup() != HitGroup_t.HITGROUP_HEAD)
		{
			return HookResult.Continue;
		}
		info.Damage = info.Damage * (1f - _config.Dices.IronHead.HeadshotReduction);
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
