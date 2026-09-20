using System;
using System.Collections.Generic;
using System.Drawing;
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
/// 神圣降临 DivineDescent（传说）。
/// 按 E：脚下展开并跟随的金色魔法阵（CBeam 绘制，旋转 8s）；期间自身 MaxHealth 抬到 666（破上限），
/// 无敌 + 伤害+100% + 移速+30%，每 0.5s 回 30HP。结束法阵收起、buff 对称注销、HP 上限还原。CD 90s。
/// </summary>
public class DivineDescent : DiceBlueprint
{
	private static readonly Color Gold = Color.FromArgb(255, 255, 215, 0);

	private readonly Dictionary<ulong, float> _endAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _nextHeal = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, int> _originalMaxHealth = new Dictionary<ulong, int>();
	private readonly Dictionary<ulong, MagicCircle> _circles = new Dictionary<ulong, MagicCircle>();

	public override string ClassName => "DivineDescent";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public DivineDescent(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_cooldownEnd[player.SteamID] = 0f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { { "playerName", player.PlayerName } });
		player.PrintToCenterAlert("🕊 按E降临神圣之力！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player != null)
		{
			End(player);
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			End(player);
		}
		_players.Clear();
		_cooldownEnd.Clear();
		_endAt.Clear();
		_nextHeal.Clear();
		_originalMaxHealth.Clear();
		_circles.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return 0f;
		}
		float now = Server.CurrentTime;
		if (_endAt.ContainsKey(player.SteamID))
		{
			return 0f;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return cd - now;
		}
		return 0f;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid || (pressed & PlayerButtons.Use) == 0 || !_players.Contains(player))
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		if (_endAt.ContainsKey(player.SteamID))
		{
			return;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return;
		}
		Start(player, pawn);
	}

	public void OnTick()
	{
		if (_endAt.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (KeyValuePair<ulong, float> kv in _endAt.ToList())
		{
			CCSPlayerController player = FindBySteamId(kv.Key);
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0 || now >= kv.Value)
			{
				End(player);
				continue;
			}
			DivineDescentConfig cfg = _config.Dices.DivineDescent;
			Vector origin = pawn.AbsOrigin;
			if (origin != null && _circles.TryGetValue(kv.Key, out MagicCircle circle))
			{
				circle.Update(new Vector(origin.X, origin.Y, origin.Z), now * cfg.RuneSpinSpeed);
			}
			if (_nextHeal.TryGetValue(kv.Key, out float healAt) && now >= healAt)
			{
				_nextHeal[kv.Key] = now + MathF.Max(cfg.HealInterval, 0.05f);
				pawn.Health = Math.Min(pawn.Health + cfg.HealPerTick, pawn.MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}

	private void Start(CCSPlayerController player, CCSPlayerPawn pawn)
	{
		float now = Server.CurrentTime;
		DivineDescentConfig cfg = _config.Dices.DivineDescent;
		float duration = cfg.DurationSeconds;
		if (DiceSynergy.HasPartner(player, "God"))
		{
			duration += 4f;
			DiceSynergy.AnnounceCombo(player, "神临", "神圣降临无敌延长 4s！");
		}
		ulong sid = player.SteamID;
		_originalMaxHealth[sid] = pawn.MaxHealth;
		pawn.MaxHealth = Math.Max(cfg.MaxHealth, 1);
		Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iMaxHealth", 0);

		Invulnerability.Grant(player, duration);
		DamageBonusManager.Register(player, ClassName, cfg.DamageBonus, null, duration);
		SpeedBonusManager.Register(player, ClassName, cfg.SpeedBonus, duration);

		_endAt[sid] = now + duration;
		_nextHeal[sid] = now + MathF.Max(cfg.HealInterval, 0.05f);
		_cooldownEnd[sid] = now + cfg.CooldownSeconds;

		Vector origin = pawn.AbsOrigin;
		if (origin != null)
		{
			_circles[sid] = new MagicCircle(new Vector(origin.X, origin.Y, origin.Z), cfg.RuneRadius, cfg.RuneOuterRadius, Gold, cfg.RuneWidth, cfg.RuneSegments, cfg.RuneSpokes, 4f);
		}
		player.PrintToCenterAlert($"🕊 神圣降临！无敌 + 伤害+{cfg.DamageBonus * 100f:0}% + 回血，持续 {duration:0}s");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}🕊 {player.PlayerName} 降临了神圣之力！");
	}

	private void End(CCSPlayerController? player)
	{
		if (player == null)
		{
			return;
		}
		ulong sid = player.SteamID;
		DamageBonusManager.Unregister(player, ClassName);
		SpeedBonusManager.Unregister(player, ClassName);
		Invulnerability.Clear(sid);
		if (_circles.TryGetValue(sid, out MagicCircle circle))
		{
			circle.Remove();
			_circles.Remove(sid);
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid && _originalMaxHealth.TryGetValue(sid, out int original))
		{
			pawn.MaxHealth = original;
			if (pawn.Health > original)
			{
				pawn.Health = original;
			}
			Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iHealth", 0);
		}
		_originalMaxHealth.Remove(sid);
		_endAt.Remove(sid);
		_nextHeal.Remove(sid);
	}

	private static CCSPlayerController? FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}
}
