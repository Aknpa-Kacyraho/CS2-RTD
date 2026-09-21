#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public static class Effects
{
	private static readonly List<CEntityInstance> Managed = new List<CEntityInstance>();

	public static string Normalize(string? particle)
	{
		if (string.IsNullOrWhiteSpace(particle))
		{
			return string.Empty;
		}
		string text = particle.Trim().Replace('\\', '/');
		if (text.EndsWith(".vpcf_c", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - 2);
		}
		return text;
	}

	public static CParticleSystem? Play(Vector? position, string particle, float? lifeSeconds = null, QAngle? angles = null)
	{
		if (position == null)
		{
			return null;
		}
		CParticleSystem system = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		return Init(system, particle, position, angles, null, lifeSeconds) ? system : null;
	}

	/// <summary>
	/// 播放一个按 <paramref name="radiusScale"/> 放大的粒子。
	/// 用 <c>env_particle_glow</c> 的 <see cref="CEnvParticleGlow.RadiusScale"/> 缩放（这是 CS2 里真正能放大粒子的方式；
	/// <c>info_particle_system</c> 没有 <c>CBodyComponent</c>，设 <c>SceneNode.Scale</c> 会静默失败）。
	/// <paramref name="radiusScale"/> 是倍率，1=原始大小，几十~几百=巨大。
	/// </summary>
	[Obsolete("RadiusScale 不是通用倍率（见 docs/research/2026-09-20-cs2-particle-system.md）。改用 CBeam 原语或自制 vpcf+CP。")]
	public static CEnvParticleGlow? PlayScaled(Vector? position, string particle, float radiusScale, float? lifeSeconds = null, QAngle? angles = null)
	{
		if (position == null)
		{
			return null;
		}
		CEnvParticleGlow glow = Utilities.CreateEntityByName<CEnvParticleGlow>("env_particle_glow");
		if (!Init(glow, particle, position, angles, null, lifeSeconds))
		{
			return null;
		}
		ApplyGlowScale(glow, radiusScale);
		return glow;
	}

	/// <summary>在目标身上附着放大的粒子（用于光翼等跟随效果）。返回的实体可用 <see cref="SetScale"/> 再改大小。</summary>
	[Obsolete("RadiusScale 不是通用倍率（见 docs/research/2026-09-20-cs2-particle-system.md）。改用 CBeam 原语或自制 vpcf+CP。")]
	public static CEnvParticleGlow? AttachScaled(CBaseEntity? target, string particle, float radiusScale, float? lifeSeconds = null, float zOffset = 0f)
	{
		if (target == null || !target.IsValid)
		{
			return null;
		}
		Vector? origin = target.AbsOrigin;
		if (origin == null)
		{
			return null;
		}
		Vector position = new Vector(origin.X, origin.Y, origin.Z + zOffset);
		CEnvParticleGlow glow = Utilities.CreateEntityByName<CEnvParticleGlow>("env_particle_glow");
		if (!Init(glow, particle, position, null, target, lifeSeconds))
		{
			return null;
		}
		ApplyGlowScale(glow, radiusScale);
		return glow;
	}

	/// <summary>改已存在放大粒子的倍率（实验性参照 <see cref="PlayScaled"/>）。</summary>
	[Obsolete("RadiusScale 不是通用倍率（见 docs/research/2026-09-20-cs2-particle-system.md）。改用 CBeam 原语或自制 vpcf+CP。")]
	public static void SetScale(CEnvParticleGlow? glow, float radiusScale)
	{
		ApplyGlowScale(glow, radiusScale);
	}

	[Obsolete("RadiusScale 不是通用倍率（见 docs/research/2026-09-20-cs2-particle-system.md）。")]
	private static void ApplyGlowScale(CEnvParticleGlow? glow, float radiusScale)
	{
		if (glow == null || !glow.IsValid)
		{
			return;
		}
		try
		{
			glow.RadiusScale = MathF.Max(radiusScale, 0.01f);
			glow.AlphaScale = 1f;
			glow.ColorTint = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)glow, "CEnvParticleGlow", "m_flRadiusScale", 0);
			Utilities.SetStateChanged((CBaseEntity)glow, "CEnvParticleGlow", "m_flAlphaScale", 0);
		}
		catch
		{
		}
	}

	/// <summary>给单个玩家叠加白色闪光遮罩（用于"越近越白"的震撼表现）。alpha 0~255，越大越白。</summary>
	public static void Whiteout(CCSPlayerController? player, float seconds, float alpha = 255f)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || seconds <= 0f)
		{
			return;
		}
		try
		{
			pawn.FlashDuration = seconds;
			pawn.FlashMaxAlpha = Math.Clamp(alpha, 0f, 255f);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawnBase", "m_flFlashDuration", 0);
			Utilities.SetStateChanged((CBaseEntity)pawn, "CCSPlayerPawnBase", "m_flFlashMaxAlpha", 0);
		}
		catch
		{
		}
	}

	public static CParticleSystem? Attach(CBaseEntity? target, string particle, float? lifeSeconds = null, float zOffset = 0f)
	{
		if (target == null || !target.IsValid)
		{
			return null;
		}
		Vector? origin = target.AbsOrigin;
		if (origin == null)
		{
			return null;
		}
		Vector position = new Vector(origin.X, origin.Y, origin.Z + zOffset);
		CParticleSystem system = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		return Init(system, particle, position, null, target, lifeSeconds) ? system : null;
	}

	public static CEnvParticleGlow? Glow(Vector? position, string particle, Color tint, float radiusScale = 1f, float alphaScale = 1f, float? lifeSeconds = null)
	{
		if (position == null)
		{
			return null;
		}
		CEnvParticleGlow glow = Utilities.CreateEntityByName<CEnvParticleGlow>("env_particle_glow");
		if (!Init(glow, particle, position, null, null, lifeSeconds))
		{
			return null;
		}
		glow.ColorTint = tint;
		glow.RadiusScale = radiusScale;
		glow.AlphaScale = alphaScale;
		return glow;
	}

	public static CParticleSystem? PlayOnPlayer(CCSPlayerController? player, string particle, float? lifeSeconds = null)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		return Attach(pawn, particle, lifeSeconds);
	}

	public static CParticleSystem? PlayAtCrosshair(CCSPlayerController? player, string particle, float distance = 200f, float? lifeSeconds = null)
	{
		CCSPlayerPawn? pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		Vector? origin = pawn.AbsOrigin;
		if (origin == null)
		{
			return null;
		}
		QAngle angles = pawn.EyeAngles;
		float pitch = angles.X * MathF.PI / 180f;
		float yaw = angles.Y * MathF.PI / 180f;
		Vector forward = new Vector(MathF.Cos(pitch) * MathF.Cos(yaw), MathF.Cos(pitch) * MathF.Sin(yaw), 0f - MathF.Sin(pitch));
		Vector eye = new Vector(origin.X, origin.Y, origin.Z + 64f);
		return Play(eye + forward * distance, particle, lifeSeconds, angles);
	}

	public static CBeam? Beam(Vector? start, Vector? end, Color color, float width = 1f, float? lifeSeconds = null)
	{
		if (start == null || end == null)
		{
			return null;
		}
		CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
		if (beam == null || !beam.IsValid)
		{
			return null;
		}
		((CBaseModelEntity)beam).Render = color;
		beam.Width = width;
		beam.EndWidth = width;
		((CBaseEntity)beam).Teleport(start, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		beam.EndPos.X = end.X;
		beam.EndPos.Y = end.Y;
		beam.EndPos.Z = end.Z;
		((CBaseEntity)beam).DispatchSpawn();
		Track(beam, lifeSeconds);
		return beam;
	}

	/// <summary>从地面点向上竖起一根光束柱（外粗内白），用于"光柱砸落"这类表现。</summary>
	public static void BeamColumn(Vector? ground, float height, Color color, float width, float? lifeSeconds = null)
	{
		if (ground == null)
		{
			return;
		}
		Vector top = new Vector(ground.X, ground.Y, ground.Z + height);
		Vector bottom = new Vector(ground.X, ground.Y, ground.Z);
		Beam(top, bottom, color, width, lifeSeconds);
		Beam(top, bottom, Color.FromArgb(255, 255, 255, 255), MathF.Max(width * 0.35f, 1f), lifeSeconds);
	}

	/// <summary>
	/// "坠落天空"的通天光柱：多层同心 CBeam 叠加模拟「炽白核心 → 蓝白边缘」的径向渐变，
	/// 再在柱内加几根偏移细亮束做"能量翻腾"。
	/// <paramref name="radius"/> 是光柱半径（外层最粗 = 直径 2×radius）。
	/// </summary>
	public static void SkyPillar(Vector? ground, float height, Color core, Color mid, Color halo, float radius, float lifeSeconds)
	{
		if (ground == null)
		{
			return;
		}
		float r = MathF.Max(radius, 8f);
		Vector top = new Vector(ground.X, ground.Y, ground.Z + height);
		Vector bottom = new Vector(ground.X, ground.Y, ground.Z);
		Beam(top, bottom, halo, r * 2f, lifeSeconds);
		Beam(top, bottom, mid, r * 1.15f, lifeSeconds);
		Beam(top, bottom, core, r * 0.5f, lifeSeconds);
		for (int i = 0; i < 4; i++)
		{
			float angle = i * MathF.PI * 0.5f;
			float ox = MathF.Cos(angle) * r * 0.42f;
			float oy = MathF.Sin(angle) * r * 0.42f;
			Vector o = new Vector(ground.X + ox, ground.Y + oy, ground.Z);
			Vector t = new Vector(ground.X + ox, ground.Y + oy, ground.Z + height);
			Beam(t, o, core, r * 0.18f, lifeSeconds);
		}
	}

	public static void Shake(Vector? position, float amplitude, float frequency, float duration, float radius = 0f)
	{
		if (position == null || duration <= 0f)
		{
			return;
		}
		CEnvShake shake = Utilities.CreateEntityByName<CEnvShake>("env_shake");
		if (shake == null || !shake.IsValid)
		{
			return;
		}
		shake.Amplitude = amplitude;
		shake.Frequency = frequency;
		shake.Duration = duration;
		shake.Radius = radius;
		((CBaseEntity)shake).Teleport(position, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		((CBaseEntity)shake).DispatchSpawn();
		shake.AcceptInput("StartShake");
		Track(shake, duration + 1f);
	}

	/// <summary>
	/// 把一个音效事件广播给所有在线真人（从各自身上发声 = 无距离衰减，全图都听得到、听得响）。
	/// 用于爆炸 / 终极技能那一瞬间的"全服有感"音效；传的是 soundevent 名（如 <c>c4.explode</c>）或 <c>.vsnd</c> 路径。
	/// </summary>
	public static void SoundAll(string? soundEvent, float volume = 1f)
	{
		if (string.IsNullOrWhiteSpace(soundEvent))
		{
			return;
		}
		float vol = Math.Clamp(volume, 0f, 1f);
		foreach (CCSPlayerController target in Utilities.GetPlayers())
		{
			if (target == null || !((CEntityInstance)target).IsValid || target.IsBot || target.IsHLTV)
			{
				continue;
			}
			try
			{
				RecipientFilter filter = new RecipientFilter();
				filter.Add(target);
				((CBaseEntity)target).EmitSound(soundEvent, filter, vol, 1f);
			}
			catch
			{
			}
		}
	}

	public static void Explosion(Vector? position, int magnitude = 0, string? effectName = null)
	{
		if (position == null)
		{
			return;
		}
		CEnvExplosion explosion = Utilities.CreateEntityByName<CEnvExplosion>("env_explosion");
		if (explosion == null || !explosion.IsValid)
		{
			return;
		}
		explosion.Magnitude = magnitude;
		explosion.PlayerDamage = 0f;
		if (!string.IsNullOrEmpty(effectName))
		{
			explosion.CustomEffectName = effectName;
		}
		((CBaseEntity)explosion).Teleport(position, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		((CBaseEntity)explosion).DispatchSpawn();
		explosion.AcceptInput("Explode");
		Track(explosion, 1f);
	}

	public static void Precache(ResourceManifest? manifest, params string[] particles)
	{
		if (manifest == null || particles == null)
		{
			return;
		}
		foreach (string particle in particles)
		{
			string effect = Normalize(particle);
			if (!string.IsNullOrEmpty(effect))
			{
				manifest.AddResource(effect);
			}
		}
	}

	public static void PrecacheAll(ResourceManifest? manifest)
	{
		if (manifest == null)
		{
			return;
		}
		foreach (FieldInfo field in typeof(ParticlePaths).GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			if (field.FieldType == typeof(string) && field.GetValue(null) is string path)
			{
				Precache(manifest, path);
			}
		}
	}

	/// <summary>把已存在的粒子实体移动到新位置（用于"持续附着 / 环绕"这类不挂 Parent、每 tick 跟随的特效）。</summary>
	public static void MoveTo(CParticleSystem? system, Vector? position)
	{
		if (system == null || position == null || !system.IsValid)
		{
			return;
		}
		try
		{
			((CBaseEntity)system).Teleport(position, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		}
		catch
		{
		}
	}

	public static void Remove(CEntityInstance? entity)
	{
		if (entity == null)
		{
			return;
		}
		Managed.Remove(entity);
		try
		{
			if (entity.IsValid)
			{
				entity.Remove();
			}
		}
		catch
		{
		}
	}

	public static void ClearAll()
	{
		foreach (CEntityInstance entity in Managed.ToList())
		{
			try
			{
				if (entity != null && entity.IsValid)
				{
					entity.Remove();
				}
			}
			catch
			{
			}
		}
		Managed.Clear();
	}

	private static bool Init(CParticleSystem? system, string particle, Vector position, QAngle? angles, CBaseEntity? parent, float? lifeSeconds)
	{
		if (system == null || !system.IsValid)
		{
			return false;
		}
		string effect = Normalize(particle);
		if (string.IsNullOrEmpty(effect))
		{
			Remove(system);
			return false;
		}
		system.EffectName = effect;
		system.StartActive = true;
		((CBaseEntity)system).Teleport(position, angles ?? new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		((CBaseEntity)system).DispatchSpawn();
		if (parent != null && parent.IsValid)
		{
			system.AcceptInput("SetParent", parent, parent, "!activator");
		}
		Track(system, lifeSeconds);
		return true;
	}

	private static void Track(CEntityInstance entity, float? lifeSeconds)
	{
		Managed.Add(entity);
		if (!lifeSeconds.HasValue || lifeSeconds.Value <= 0f)
		{
			return;
		}
		CEntityInstance captured = entity;
		Action cleanup = delegate
		{
			Remove(captured);
		};
		// 使用插件作用域的定时器：全局 new Timer 不会被 Unload 取消，插件卸载后回调仍会触发并持有已失效实体引用。
		RollTheDice? plugin = RollTheDice.Instance;
		if (plugin != null)
		{
			plugin.AddTimer(lifeSeconds.Value, cleanup, (TimerFlags?)null);
		}
		else
		{
			new Timer(lifeSeconds.Value, cleanup, null);
		}
	}
}
