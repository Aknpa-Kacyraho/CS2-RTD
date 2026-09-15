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
		((CBaseEntity)beam).Teleport(start, new QAngle(0f, 0f, 0f), new Vector(0f, 0f, 0f));
		beam.EndPos.X = end.X;
		beam.EndPos.Y = end.Y;
		beam.EndPos.Z = end.Z;
		((CBaseEntity)beam).DispatchSpawn();
		Track(beam, lifeSeconds);
		return beam;
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
