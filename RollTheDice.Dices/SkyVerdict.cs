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
/// 天穹裁决 SkyVerdict（传说）。
/// 按 E：用 Trace 锁定准星命中点（撞墙即止、不穿墙），以该点为中心铺开"超位魔法"级的**多层同心地面法阵**
/// （外层带外环、内层逐层缩小、正反交替旋转，倒数期间从放大 1.4 倍向中心合拢），并在其上方竖起一座
/// **8 层水平法阵塔**（层层抬升、正反旋转、双色交替；够高不被地形遮挡、够大远处可见）。
/// 10s 后金色光柱从天而降，范围内敌人高伤 + 上抛 + 白屏，命中瞬间全服广播 C4 爆炸音效；超大半径震屏让全图有感。CD 60s。
/// </summary>
public class SkyVerdict : DiceBlueprint
{
	private static readonly Color Gold = Color.FromArgb(255, 255, 205, 40);
	private static readonly Color GoldPale = Color.FromArgb(255, 255, 240, 190);

	private readonly Dictionary<ulong, float> _cooldownEnd = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _strikeAt = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, Vector> _strikeCenter = new Dictionary<ulong, Vector>();
	private readonly Dictionary<ulong, MagicSigil> _sigils = new Dictionary<ulong, MagicSigil>();
	private readonly Dictionary<ulong, MagicTower> _towers = new Dictionary<ulong, MagicTower>();
	private readonly Dictionary<ulong, int> _nextSecond = new Dictionary<ulong, int>();
	private int _tick;

	public override string ClassName => "SkyVerdict";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public SkyVerdict(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		player.PrintToCenterAlert("⚖ 按E降下天穹裁决！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		// 故意不 ClearPending：10s 锁定一旦按下就该落下，不因施法者阵亡而取消。
		if (player != null)
		{
			_cooldownEnd.Remove(player.SteamID);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (MagicSigil sigil in _sigils.Values)
		{
			sigil.Remove();
		}
		foreach (MagicTower tower in _towers.Values)
		{
			tower.Remove();
		}
		_players.Clear();
		_cooldownEnd.Clear();
		_strikeAt.Clear();
		_strikeCenter.Clear();
		_sigils.Clear();
		_towers.Clear();
		_nextSecond.Clear();
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
		if (_cooldownEnd.TryGetValue(player.SteamID, out float cd) && now < cd)
		{
			return;
		}
		if (_strikeAt.ContainsKey(player.SteamID))
		{
			return;
		}
		SkyVerdictConfig cfg = _config.Dices.SkyVerdict;
		Vector center = ResolveAimPoint(pawn, cfg.Range);
		ulong sid = player.SteamID;
		_sigils[sid] = new MagicSigil(center, cfg.RuneRadius, cfg.RuneOuterRadius, Gold, cfg.RuneWidth, cfg.RuneSegments, cfg.RuneSpokes, cfg.SigilRings, cfg.SigilShrink, cfg.SigilSpin, 4f);
		_towers[sid] = new MagicTower(center, Gold, GoldPale, cfg.RuneWidth, cfg.TowerSegments, cfg.TowerSpokes, cfg.TowerCount, cfg.TowerHeightBase, cfg.TowerHeightStep, cfg.TowerRadius, cfg.TowerOuterRadius, cfg.TowerRadiusDecay, cfg.TowerSpin);
		Effects.Play(new Vector(center.X, center.Y, center.Z + 2f), ParticlePaths.PingGroundRings, cfg.DelaySeconds + 1.2f);
		Effects.Play(new Vector(center.X, center.Y, center.Z + 8f), ParticlePaths.DangerZoneLoop, cfg.DelaySeconds + 1.2f);
		_strikeAt[sid] = now + cfg.DelaySeconds;
		_strikeCenter[sid] = center;
		_nextSecond[sid] = (int)MathF.Ceiling(cfg.DelaySeconds);
		_cooldownEnd[sid] = now + cfg.CooldownSeconds;
		player.PrintToCenterAlert($"⚖ 天穹裁决锁定！{cfg.DelaySeconds:0}s 后降临！");
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⚖ {player.PlayerName} 降下了天穹裁决！");
	}

	public void OnTick()
	{
		if (_strikeAt.Count == 0)
		{
			return;
		}
		_tick++;
		bool visual = (_tick & 1) == 0;
		float now = Server.CurrentTime;
		SkyVerdictConfig cfg = _config.Dices.SkyVerdict;
		float delay = MathF.Max(cfg.DelaySeconds, 0.1f);
		foreach (KeyValuePair<ulong, float> kv in _strikeAt.ToList())
		{
			ulong sid = kv.Key;
			Vector center = _strikeCenter.TryGetValue(sid, out Vector c) ? c : null;
			if (now < kv.Value)
			{
				if (visual && center != null)
				{
					float frac = Math.Clamp((kv.Value - now) / delay, 0f, 1f);
					float scale = 1f + cfg.SigilContract * frac;
					if (_sigils.TryGetValue(sid, out MagicSigil sigil))
					{
						sigil.Update(center, now, scale);
					}
					if (_towers.TryGetValue(sid, out MagicTower tower))
					{
						tower.Update(center, now, 1f + cfg.TowerContract * frac);
					}
				}
				int remaining = (int)MathF.Ceiling(kv.Value - now);
				if (_nextSecond.TryGetValue(sid, out int last) && remaining < last)
				{
					_nextSecond[sid] = remaining;
					FindBySteamId(sid)?.PrintToCenterAlert($"⚖ 天穹裁决 {remaining}s");
				}
				continue;
			}
			ClearPending(sid);
			if (center != null)
			{
				Detonate(sid, center);
			}
		}
	}

	private static Vector ResolveAimPoint(CCSPlayerPawn pawn, float range)
	{
		Vector origin = pawn.AbsOrigin;
		QAngle angles = pawn.EyeAngles;
		float pitch = angles.X * MathF.PI / 180f;
		float yaw = angles.Y * MathF.PI / 180f;
		float cp = MathF.Cos(pitch);
		float sp = MathF.Sin(pitch);
		float fx = cp * MathF.Cos(yaw);
		float fy = cp * MathF.Sin(yaw);
		float fz = 0f - sp;
		Vector start = new Vector(origin.X, origin.Y, origin.Z + 64f);
		Vector end = new Vector(start.X + fx * range, start.Y + fy * range, start.Z + fz * range);
		try
		{
			TraceResult result = Trace.TraceEndShape(start, end, (CBaseEntity)pawn);
			return result.EndPos;
		}
		catch
		{
			return end;
		}
	}

	private void Detonate(ulong casterSid, Vector center)
	{
		SkyVerdictConfig cfg = _config.Dices.SkyVerdict;
		float radius = cfg.Radius;
		CCSPlayerController caster = FindBySteamId(casterSid);
		if (caster != null && DiceSynergy.HasPartner(caster, "DivinePunishment"))
		{
			radius *= 1.5f;
		}
		Effects.BeamColumn(new Vector(center.X, center.Y, center.Z), cfg.PillarHeight, Gold, cfg.PillarWidth, cfg.PillarLife);
		Effects.Play(center, ParticlePaths.ExplosionHegrenade, 1.5f);
		Effects.Play(center, ParticlePaths.ExplosionFlashbang, 1.5f);
		Effects.Play(center, ParticlePaths.ExplosionDistort, 1.5f);
		Effects.Explosion(center, 0, null);
		Effects.SoundAll(cfg.ExplosionSound, cfg.ExplosionSoundVolume);
		Effects.Shake(center, cfg.ShakeAmplitude, cfg.ShakeFrequency, cfg.ShakeDuration, cfg.ShakeRadius);

		float falloff = MathF.Max(cfg.FalloffRadius, 1f);
		foreach (CCSPlayerController victim in Utilities.GetPlayers())
		{
			if (victim == null || !victim.IsValid || victim.IsHLTV || victim.SteamID == casterSid)
			{
				continue;
			}
			if (caster != null && ((CBaseEntity)victim).TeamNum == ((CBaseEntity)caster).TeamNum)
			{
				continue;
			}
			CCSPlayerPawn vp = victim.PlayerPawn?.Value;
			Vector pos = vp?.AbsOrigin;
			if (vp == null || !vp.IsValid || ((CBaseEntity)vp).LifeState != 0 || pos == null)
			{
				continue;
			}
			float dist = Vectors.GetDistance(pos, center);
			float scale;
			if (dist <= radius)
			{
				scale = 1f;
			}
			else if (dist <= radius + falloff)
			{
				scale = 1f - (dist - radius) / falloff * (1f - cfg.MinDamageScale);
			}
			else
			{
				continue;
			}
			Effects.Whiteout(victim, cfg.WhiteoutSeconds * scale, 255f * scale);
			if (Invulnerability.IsInvulnerable(victim))
			{
				continue;
			}
			int dmg = (int)MathF.Round(cfg.Damage * scale);
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
			try
			{
				((CBaseEntity)vp).Teleport((Vector)null, (QAngle)null, new Vector(0f, 0f, cfg.LaunchForce * scale));
			}
			catch
			{
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
		_strikeAt.Remove(sid);
		_strikeCenter.Remove(sid);
		_nextSecond.Remove(sid);
		if (_sigils.TryGetValue(sid, out MagicSigil sigil))
		{
			sigil.Remove();
			_sigils.Remove(sid);
		}
		if (_towers.TryGetValue(sid, out MagicTower tower))
		{
			tower.Remove();
			_towers.Remove(sid);
		}
	}

	private static CCSPlayerController? FindBySteamId(ulong steamId)
	{
		return Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && p.SteamID == steamId);
	}
}
