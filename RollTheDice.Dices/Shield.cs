using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 坚盾 Shield：获得一个可吸收固定伤害的护盾（吸收期间减伤），吸收量耗尽后击穿；
/// 击穿后击杀敌人重新充能。与 Evasion 组合（钢铁壁垒）：吸收量更高。
/// </summary>
public class Shield : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<ulong, float> _pool = new Dictionary<ulong, float>();

	private readonly HashSet<ulong> _broken = new HashSet<ulong>();

	private readonly Dictionary<ulong, int> _kills = new Dictionary<ulong, int>();

	public override string ClassName => "Shield";

	public override List<string> Listeners => new List<string> { "OnPlayerTakeDamagePre" };

	public override List<string> Events => new List<string> { "EventPlayerDeath" };

	public Shield(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		ShieldConfig cfg = _config.Dices.Shield;
		int armor = _random.Next(cfg.ArmorMin, cfg.ArmorMax + 1);
		CCSPlayerPawn pawn = player.PlayerPawn.Value;
		pawn.ArmorValue = Math.Min(pawn.ArmorValue + armor, 100);
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
		if (cfg.Helmet)
		{
			player.GiveNamedItem("item_assaultsuit");
		}
		ulong id = player.SteamID;
		_pool[id] = Absorb(player);
		_broken.Remove(id);
		_kills[id] = 0;
		_players.Add(player);
		if (DiceSynergy.HasPartner(player, "Evasion"))
		{
			DiceSynergy.AnnounceCombo(player, "钢铁壁垒", "护盾吸收量提升 50%！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			},
			{
				"armor",
				armor.ToString()
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageReductionManager.Unregister(player, ClassName);
		ulong id = player.SteamID;
		_pool.Remove(id);
		_broken.Remove(id);
		_kills.Remove(id);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			DamageReductionManager.Unregister(player, ClassName);
		}
		_pool.Clear();
		_broken.Clear();
		_kills.Clear();
		_players.Clear();
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
		CCSPlayerController victim = ResolvePlayer(entity);
		if (victim == null || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		CCSPlayerController attacker = ResolvePlayer(info.Attacker?.Value);
		if (attacker == victim)
		{
			return HookResult.Continue;
		}
		if (attacker != null && ((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		ulong id = victim.SteamID;
		if (_broken.Contains(id) || !_pool.TryGetValue(id, out float pool) || pool <= 0f)
		{
			return HookResult.Continue;
		}
		float reduction = Math.Clamp(_config.Dices.Shield.Reduction, 0f, 0.9f);
		pool -= info.Damage * reduction;
		info.Damage *= 1f - reduction;
		if (pool <= 0f)
		{
			pool = 0f;
			_broken.Add(id);
			_kills[id] = 0;
			victim.PrintToCenterAlert("护盾破碎！击杀敌人可重新充能");
		}
		_pool[id] = pool;
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
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		ulong id = attacker.SteamID;
		if (!_broken.Contains(id))
		{
			return HookResult.Continue;
		}
		int kills = (_kills.TryGetValue(id, out int k) ? k : 0) + 1;
		if (kills >= Math.Max(1, _config.Dices.Shield.RefreshKills))
		{
			_broken.Remove(id);
			_kills[id] = 0;
			_pool[id] = Absorb(attacker);
			attacker.PrintToCenterAlert("护盾重新充能！");
		}
		else
		{
			_kills[id] = kills;
		}
		return HookResult.Continue;
	}

	private float Absorb(CCSPlayerController player)
	{
		float absorb = _config.Dices.Shield.AbsorbDamage;
		if (DiceSynergy.HasPartner(player, "Evasion"))
		{
			absorb *= 1.5f;
		}
		return absorb;
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
