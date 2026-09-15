using System;
using System.Collections.Generic;
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

/// <summary>
/// 亚戈鲁 Yagorou：每次击杀敌人获得 1s 无敌；首次受到的致命伤害被完全免疫，并获得 0.5s 无敌。
/// 无敌通过 OnPlayerTakeDamagePre 将伤害归零实现（TakesDamage=false 在本版本不可靠）。
/// </summary>
public class Yagorou : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _invulnUntil = new Dictionary<ulong, float>();

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
		_lethalSaves[player.SteamID] = _config.Dices.Yagorou.LethalSavesPerRound;
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_players.Remove(player);
		_invulnUntil.Remove(player.SteamID);
		_lethalSaves.Remove(player.SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_invulnUntil.Clear();
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
		if ((CEntityInstance)(object)killer == (CEntityInstance)null || !((CEntityInstance)killer).IsValid || (CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)killer == (CEntityInstance)(object)victim || !_players.Contains(killer) || ((CBaseEntity)killer).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)killer).SteamID;
		float until = Server.CurrentTime + _config.Dices.Yagorou.KillInvulnSeconds;
		if (!_invulnUntil.TryGetValue(steamID, out float existing) || existing < until)
		{
			_invulnUntil[steamID] = until;
		}
		killer.PrintToCenterAlert($"亚戈鲁：击杀无敌 {_config.Dices.Yagorou.KillInvulnSeconds:F1}s");
		return (HookResult)0;
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (_invulnUntil.Count == 0 && _lethalSaves.Count == 0)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid || info.Damage <= 0f)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn pawn = ((NativeObject)entity).As<CCSPlayerPawn>();
		if (pawn == null)
		{
			return (HookResult)0;
		}
		object obj;
		CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)pawn).Controller;
		if (controller == null)
		{
			obj = null;
		}
		else
		{
			CBasePlayerController value = controller.Value;
			obj = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
		}
		CCSPlayerController player = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)player).SteamID;
		if (_invulnUntil.TryGetValue(steamID, out float until))
		{
			if (Server.CurrentTime < until)
			{
				info.Damage = 0f;
				return (HookResult)1;
			}
			_invulnUntil.Remove(steamID);
		}
		if (_lethalSaves.TryGetValue(steamID, out int saves) && saves > 0 && ((CBaseEntity)pawn).Health - info.Damage <= 0f)
		{
			info.Damage = 0f;
			if (saves <= 1)
			{
				_lethalSaves.Remove(steamID);
			}
			else
			{
				_lethalSaves[steamID] = saves - 1;
			}
			float lethalUntil = Server.CurrentTime + _config.Dices.Yagorou.LethalInvulnSeconds;
			if (!_invulnUntil.TryGetValue(steamID, out float existing) || existing < lethalUntil)
			{
				_invulnUntil[steamID] = lethalUntil;
			}
			player.PrintToCenterAlert("亚戈鲁：致命伤害免疫！");
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
