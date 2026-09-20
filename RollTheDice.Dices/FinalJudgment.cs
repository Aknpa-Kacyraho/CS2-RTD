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
/// 终焉审判 FinalJudgment（传说）。
/// 按 E：脚下铺开多层同心超位法阵（倒数收缩），头顶再竖起一座**8 层水平法阵塔**（层层抬升、正反旋转、双色交替）。
/// 20s 后引爆：全图所有人（除施法者，含队友）按距离衰减受 2000→250 伤害，越近白屏越强，
/// 命中瞬间全服广播 C4 爆炸音效 + 最强震屏 + 扭曲。CD 90s。法阵敌人可见，可跑位降低伤害。
/// 注：施法后即使施法者阵亡也会照常引爆（20s 太久，不能被死亡打断）。
/// </summary>
public class FinalJudgment : DiceBlueprint
{
	private static readonly Color Gold = Color.FromArgb(255, 255, 205, 40);
	private static readonly Color GoldPale = Color.FromArgb(255, 255, 240, 190);

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _detonateAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, Vector> _center = new Dictionary<ulong, Vector>();
	private readonly Dictionary<ulong, MagicSigil> _ground = new Dictionary<ulong, MagicSigil>();
	private readonly Dictionary<ulong, MagicTower> _tower = new Dictionary<ulong, MagicTower>();
	private readonly Dictionary<ulong, int> _nextRingSecond = new Dictionary<ulong, int>();
	private int _tick;

	public override string ClassName => "FinalJudgment";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public FinalJudgment(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		player.PrintToCenterAlert("☄ 按E发动终焉审判！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		// 故意不 ClearPending：20s 太长，施法者阵亡也必须照常引爆（否则就等于没伤害）。
		if (player != null)
		{
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (MagicSigil sigil in _ground.Values)
		{
			sigil.Remove();
		}
		foreach (MagicTower tower in _tower.Values)
		{
			tower.Remove();
		}
		_players.Clear();
		_cooldownEnd.Clear();
		_detonateAt.Clear();
		_center.Clear();
		_ground.Clear();
		_tower.Clear();
		_nextRingSecond.Clear();
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
		if (_detonateAt.ContainsKey(player.SteamID))
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
		if (_detonateAt.ContainsKey(player.SteamID))
		{
			return;
		}
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return;
		}
		Vector origin = pawn.AbsOrigin;
		if (origin == null)
		{
			return;
		}
		Vector center = new Vector(origin.X, origin.Y, origin.Z);
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		ulong sid = player.SteamID;

		_ground[sid] = new MagicSigil(center, cfg.GroundRadius, cfg.GroundOuterRadius, Gold, cfg.RuneWidth, cfg.RuneSegments, cfg.RuneSpokes, cfg.SigilRings, cfg.SigilShrink, cfg.SigilSpin, 4f);

		// 空中法阵塔：8 层水平法阵往上叠成"超位魔法"塔（够高/够大/够多/双色交替）。
		_tower[sid] = new MagicTower(center, Gold, GoldPale, cfg.RuneWidth, cfg.TowerSegments, cfg.TowerSpokes, cfg.TowerCount, cfg.TowerHeightBase, cfg.TowerHeightStep, cfg.TowerRadius, cfg.TowerOuterRadius, cfg.TowerRadiusDecay, cfg.TowerSpin);

		Effects.Play(new Vector(center.X, center.Y, center.Z + 2f), ParticlePaths.PingGroundRings, cfg.DelaySeconds + 1.5f);
		_detonateAt[sid] = now + cfg.DelaySeconds;
		_center[sid] = center;
		_nextRingSecond[sid] = (int)MathF.Ceiling(cfg.DelaySeconds);
		_cooldownEnd[sid] = now + cfg.CooldownSeconds;
		player.PrintToCenterAlert($"☄ 终焉审判！{cfg.DelaySeconds:0}s 后引爆，快离开中心！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☄ {player.PlayerName} 发动了终焉审判！");
	}

	public void OnTick()
	{
		if (_detonateAt.Count == 0)
		{
			return;
		}
		_tick++;
		bool visual = (_tick & 1) == 0;
		float now = Server.CurrentTime;
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		float delay = MathF.Max(cfg.DelaySeconds, 0.1f);
		foreach (KeyValuePair<ulong, float> kv in _detonateAt.ToList())
		{
			ulong sid = kv.Key;
			Vector center = _center.TryGetValue(sid, out Vector c) ? c : null;
			if (center == null)
			{
				ClearPending(sid);
				continue;
			}
			float remain = MathF.Max(kv.Value - now, 0f);
			float frac = Math.Clamp(remain / delay, 0f, 1f);
			if (visual)
			{
				float scale = 1f + cfg.SigilContract * frac;
				if (_ground.TryGetValue(sid, out MagicSigil ground))
				{
					ground.Update(center, now, scale);
				}
				if (_tower.TryGetValue(sid, out MagicTower tower))
				{
					tower.Update(center, now, 1f + cfg.TowerContract * frac);
				}
			}
			if (now >= kv.Value)
			{
				ClearPending(sid);
				Detonate(sid, center);
				continue;
			}
			int remaining = (int)MathF.Ceiling(kv.Value - now);
			if (_nextRingSecond.TryGetValue(sid, out int last) && remaining < last)
			{
				_nextRingSecond[sid] = remaining;
				FindBySteamId(sid)?.PrintToCenterAlert($"☄ 终焉审判 {remaining}s");
			}
		}
	}

	private void Detonate(ulong casterSid, Vector center)
	{
		FinalJudgmentConfig cfg = _config.Dices.FinalJudgment;
		float falloff = MathF.Max(cfg.FalloffRadius, 1f);
		CCSPlayerController caster = FindBySteamId(casterSid);
		if (caster != null && DiceSynergy.HasPartner(caster, "Ragnarok"))
		{
			falloff *= 1.3f;
		}
		Effects.BeamColumn(new Vector(center.X, center.Y, center.Z), cfg.PillarHeight, Gold, cfg.PillarWidth, cfg.PillarLife);
		Effects.Play(center, ParticlePaths.ExplosionHegrenade, 2f);
		Effects.Play(center, ParticlePaths.ExplosionDistort, 2f);
		Effects.Play(center, ParticlePaths.ExplosionFlashbang, 2f);
		Effects.Explosion(center, 0, null);
		Effects.SoundAll(cfg.ExplosionSound, cfg.ExplosionSoundVolume);
		Effects.Shake(center, cfg.ShakeAmplitude, cfg.ShakeFrequency, cfg.ShakeDuration, cfg.ShakeRadius);

		foreach (CCSPlayerController victim in Utilities.GetPlayers())
		{
			if (victim == null || !victim.IsValid || victim.IsHLTV || victim.SteamID == casterSid)
			{
				continue;
			}
			CCSPlayerPawn vp = victim.PlayerPawn?.Value;
			Vector pos = vp?.AbsOrigin;
			if (vp == null || !vp.IsValid || ((CBaseEntity)vp).LifeState != 0 || pos == null)
			{
				continue;
			}
			float dx = pos.X - center.X;
			float dy = pos.Y - center.Y;
			float dz = pos.Z - center.Z;
			float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
			float proximity = Math.Clamp(1f - dist / falloff, 0f, 1f);
			Effects.Whiteout(victim, cfg.WhiteoutSeconds * proximity, 255f * proximity);
			if (Invulnerability.IsInvulnerable(victim))
			{
				continue;
			}
			int dmg = cfg.MinDamage + (int)MathF.Round((cfg.MaxDamage - cfg.MinDamage) * proximity);
			if (dmg <= 0)
			{
				continue;
			}
			((CBaseEntity)vp).Health -= dmg;
			Utilities.SetStateChanged((CBaseEntity)vp, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)vp).Health <= 0)
			{
				Kill(vp);
			}
		}
	}

	private static void Kill(CCSPlayerPawn pawn)
	{
		try
		{
			((CBasePlayerPawn)pawn).CommitSuicide(false, true);
		}
		catch
		{
			((CBaseEntity)pawn).Health = 0;
			Utilities.SetStateChanged((CBaseEntity)pawn, "CBaseEntity", "m_iHealth", 0);
		}
	}

	private void ClearPending(ulong sid)
	{
		_detonateAt.Remove(sid);
		_center.Remove(sid);
		_nextRingSecond.Remove(sid);
		if (_ground.TryGetValue(sid, out MagicSigil ground))
		{
			ground.Remove();
			_ground.Remove(sid);
		}
		if (_tower.TryGetValue(sid, out MagicTower tower))
		{
			tower.Remove();
			_tower.Remove(sid);
		}
	}

	private static CCSPlayerController? FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}
}
