using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 亚戈鲁 Yagorou：每次击杀敌人获得短暂无敌；首次受到的致命伤害被完全免疫并获得无敌。
/// 无敌统一走共享 Invulnerability（主插件 OnPlayerTakeDamagePreCentral 统一置 0），
/// 比自建字典 + 本 dice 钩子更可靠（2026-09-19 修：原实现只在本 dice 钩子置 0，容易被别处/直扣血绕过）。
/// </summary>
public class Yagorou : DiceBlueprint
{
	/// <summary>持有者 SteamID 集合：判定持有者不依赖控制器对象引用。</summary>
	private readonly HashSet<ulong> _holderIds = new HashSet<ulong>();

	private readonly Dictionary<ulong, int> _lethalSaves = new Dictionary<ulong, int>();

	public override string ClassName => "Yagorou";

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public Yagorou(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Add(CCSPlayerController player)
	{
		base.Add(player);
		if (!_players.Contains(player))
		{
			return;
		}
		_holderIds.Add(player.SteamID);
		_lethalSaves[player.SteamID] = _config.Dices.Yagorou.LethalSavesPerRound;
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_players.Remove(player);
		_holderIds.Remove(player.SteamID);
		_lethalSaves.Remove(player.SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_holderIds.Clear();
		_lethalSaves.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController killer = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (killer == null || !((CEntityInstance)killer).IsValid || victim == null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)killer == (CEntityInstance)(object)victim || !_holderIds.Contains(killer.SteamID) || ((CBaseEntity)killer).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return (HookResult)0;
		}
		float seconds = _config.Dices.Yagorou.KillInvulnSeconds;
		Invulnerability.Grant(killer, seconds);
		RollTheDice.LogDebug($"[Yagorou] kill: killer={killer.SteamID} invuln={seconds:F1}s now={Server.CurrentTime:F2}\n");
		killer.PrintToCenterAlert($"亚戈鲁：击杀无敌 {seconds:F1}s");
		return (HookResult)0;
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_lethalSaves.Count == 0 || entity == null || !entity.IsValid || info.Damage <= 0f)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn pawn = entity.As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return (HookResult)0;
		}
		CCSPlayerController player = ((CBasePlayerPawn)pawn).Controller?.Value?.As<CCSPlayerController>();
		if (player == null || !player.IsValid)
		{
			return (HookResult)0;
		}
		ulong steamID = player.SteamID;
		if (!_holderIds.Contains(steamID))
		{
			return (HookResult)0;
		}
		// 无敌窗口由主插件统一拦截；这里只处理"首次致命伤害免疫"。
		if (_lethalSaves.TryGetValue(steamID, out int saves) && saves > 0 && pawn.Health - info.Damage <= 0f)
		{
			float incoming = info.Damage;
			info.Damage = 0f;
			if (saves <= 1)
			{
				_lethalSaves.Remove(steamID);
			}
			else
			{
				_lethalSaves[steamID] = saves - 1;
			}
			float lethalUntil = _config.Dices.Yagorou.LethalInvulnSeconds;
			Invulnerability.Grant(player, lethalUntil);
			RollTheDice.LogDebug($"[Yagorou] lethal save: sid={steamID} hp={pawn.Health} dmg={incoming:F1} invuln={lethalUntil:F1}s\n");
			player.PrintToCenterAlert("亚戈鲁：致命伤害免疫！");
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
