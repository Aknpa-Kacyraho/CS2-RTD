#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RollTheDice.Utils;

/// <summary>持续类特效的挂点方式（attach / orbit 共用）。</summary>
public enum FxMode
{
	Around = 0,
	Behind = 1,
	Wings = 2
}

/// <summary>抽到 / 移除 dice 时的一次性触发。</summary>
public sealed class FxTriggerSlot
{
	[JsonPropertyName("burst")] public string Burst { get; set; } = "";
	[JsonPropertyName("remove")] public string Remove { get; set; } = "";
}

/// <summary>持续附着（旧 Hold）。</summary>
public sealed class FxAttachSlot
{
	[JsonPropertyName("particle")] public string Particle { get; set; } = "";
	[JsonPropertyName("mode")] public FxMode Mode { get; set; } = FxMode.Around;
	[JsonPropertyName("z")] public float Z { get; set; } = 40f;
}

/// <summary>双翼：用 CBeam 自造的光翼（attach / orbit 的 mode == Wings 时生效，忽略 particle）。</summary>
public sealed class FxWingSlot
{
	[JsonPropertyName("color")] public int[] Color { get; set; } = new[] { 255, 215, 0 };
	/// <summary>每侧羽片数。</summary>
	[JsonPropertyName("blades")] public int Blades { get; set; } = 4;
	/// <summary>翼展基准长度。</summary>
	[JsonPropertyName("length")] public float Length { get; set; } = 90f;
	[JsonPropertyName("width")] public float Width { get; set; } = 3f;
	/// <summary>扇动速度倍率（0 = 不扇动）。</summary>
	[JsonPropertyName("flap")] public float Flap { get; set; } = 0.6f;
}

/// <summary>环绕（旧 Orbit）。</summary>
public sealed class FxOrbitSlot
{
	[JsonPropertyName("particle")] public string Particle { get; set; } = "";
	[JsonPropertyName("mode")] public FxMode Mode { get; set; } = FxMode.Behind;
	[JsonPropertyName("radius")] public float Radius { get; set; } = 70f;
	[JsonPropertyName("speed")] public float Speed { get; set; } = 2f;
	[JsonPropertyName("z")] public float Z { get; set; } = 40f;
}

/// <summary>脚底痕迹（旧 Trail）。</summary>
public sealed class FxTrailSlot
{
	[JsonPropertyName("particle")] public string Particle { get; set; } = "";
	[JsonPropertyName("interval")] public float Interval { get; set; } = 2f;
	[JsonPropertyName("z")] public float Z { get; set; } = 4f;
}

/// <summary>attach / orbit 的 behind・wings 几何（按玩家朝向实时计算）。</summary>
public sealed class FxGeom
{
	[JsonPropertyName("behind")] public float Behind { get; set; } = 26f;
	[JsonPropertyName("spread")] public float Spread { get; set; } = 34f;
	[JsonPropertyName("back")] public float Back { get; set; } = 22f;
	[JsonPropertyName("wingZ")] public float WingZ { get; set; } = 52f;
}

/// <summary>战斗 / 回合事件触发的粒子。</summary>
public sealed class FxEvents
{
	[JsonPropertyName("fire")] public string Fire { get; set; } = "";
	[JsonPropertyName("kill")] public string Kill { get; set; } = "";
	[JsonPropertyName("killSelf")] public string KillSelf { get; set; } = "";
	[JsonPropertyName("killHeadshot")] public string KillHeadshot { get; set; } = "";
	[JsonPropertyName("death")] public string Death { get; set; } = "";
	[JsonPropertyName("hurt")] public string Hurt { get; set; } = "";
	[JsonPropertyName("hit")] public string Hit { get; set; } = "";
	[JsonPropertyName("hitHeadshot")] public string HitHeadshot { get; set; } = "";
	[JsonPropertyName("roundStart")] public string RoundStart { get; set; } = "";
	[JsonPropertyName("roundEnd")] public string RoundEnd { get; set; } = "";
}

/// <summary>CBeam 光束效果的一条规格（<c>shape</c> = ring | pillar）。</summary>
public sealed class FxBeamSpec
{
	[JsonPropertyName("shape")] public string Shape { get; set; } = "ring";
	[JsonPropertyName("color")] public int[] Color { get; set; } = new[] { 255, 215, 0 };
	[JsonPropertyName("width")] public float Width { get; set; } = 3f;
	[JsonPropertyName("life")] public float Life { get; set; } = 2f;
	[JsonPropertyName("spin")] public float Spin { get; set; } = 1.5f;
	/// <summary>生命期结束时半径的额外增量（负值=收缩）。</summary>
	[JsonPropertyName("grow")] public float Grow { get; set; }
	// ring
	[JsonPropertyName("radius")] public float Radius { get; set; } = 180f;
	[JsonPropertyName("outer")] public float Outer { get; set; }
	[JsonPropertyName("segments")] public int Segments { get; set; } = 24;
	[JsonPropertyName("spokes")] public int Spokes { get; set; } = 8;
	[JsonPropertyName("z")] public float Z { get; set; } = 4f;
	// pillar
	[JsonPropertyName("height")] public float Height { get; set; } = 500f;
}

/// <summary>各事件可播放的光束列表（与粒子槽并行）。</summary>
public sealed class FxBeams
{
	[JsonPropertyName("trigger")] public List<FxBeamSpec>? Trigger { get; set; }
	[JsonPropertyName("remove")] public List<FxBeamSpec>? Remove { get; set; }
	[JsonPropertyName("kill")] public List<FxBeamSpec>? Kill { get; set; }
	[JsonPropertyName("killSelf")] public List<FxBeamSpec>? KillSelf { get; set; }
	[JsonPropertyName("death")] public List<FxBeamSpec>? Death { get; set; }
	[JsonPropertyName("hurt")] public List<FxBeamSpec>? Hurt { get; set; }
	[JsonPropertyName("hit")] public List<FxBeamSpec>? Hit { get; set; }
	[JsonPropertyName("roundStart")] public List<FxBeamSpec>? RoundStart { get; set; }
	[JsonPropertyName("roundEnd")] public List<FxBeamSpec>? RoundEnd { get; set; }
}

/// <summary>单个 dice 的特效档案（dicefx.json 的 dice.&lt;ClassName&gt;）。</summary>
public sealed class FxProfile
{
	[JsonPropertyName("trigger")] public FxTriggerSlot Trigger { get; set; } = new FxTriggerSlot();
	[JsonPropertyName("attach")] public FxAttachSlot Attach { get; set; } = new FxAttachSlot();
	[JsonPropertyName("orbit")] public FxOrbitSlot Orbit { get; set; } = new FxOrbitSlot();
	[JsonPropertyName("trail")] public FxTrailSlot Trail { get; set; } = new FxTrailSlot();
	[JsonPropertyName("geom")] public FxGeom Geom { get; set; } = new FxGeom();
	[JsonPropertyName("wing")] public FxWingSlot Wing { get; set; } = new FxWingSlot();
	[JsonPropertyName("events")] public FxEvents Events { get; set; } = new FxEvents();
	[JsonPropertyName("beams")] public FxBeams? Beams { get; set; }
}

internal sealed class DiceFxConfig
{
	[JsonPropertyName("version")] public int Version { get; set; } = DiceFxTable.SupportedVersion;
	[JsonPropertyName("dice")] public Dictionary<string, FxProfile> Dice { get; set; } = new Dictionary<string, FxProfile>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// dicefx.json 的加载入口。数据（每个 dice 的事件→粒子、挂点几何）全在本文件/该 JSON，
/// 引擎（<see cref="DiceEffects"/>）只读这里。加载为 fail-soft：解析失败保留上一份好的表，
/// 文件缺失则清空（特效关闭），都不影响 dice 本体。
/// </summary>
public static class DiceFxTable
{
	public const int SupportedVersion = 1;

	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		Converters = { new JsonStringEnumConverter() }
	};

	private static readonly FxProfile EmptyProfile = new FxProfile();

	private static Dictionary<string, FxProfile> _profiles = new Dictionary<string, FxProfile>(StringComparer.OrdinalIgnoreCase);

	public static int Count => _profiles.Count;

	public static IEnumerable<KeyValuePair<string, FxProfile>> All => _profiles;

	public static bool TryGet(string? className, out FxProfile profile)
	{
		if (className != null && _profiles.TryGetValue(className, out FxProfile? found))
		{
			profile = found;
			return true;
		}
		profile = EmptyProfile;
		return false;
	}

	public static void Clear()
	{
		_profiles = new Dictionary<string, FxProfile>(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// 从磁盘重载。返回是否替换了内存表。
	/// 文件缺失 → 清空表并返回 false；解析异常 → 保留旧表并返回 false。
	/// </summary>
	public static bool Reload(string path, ICollection<string>? knownDice = null)
	{
		try
		{
			if (!File.Exists(path))
			{
				Clear();
				Report($"[DiceFX] dicefx.json not found: {path} (effects disabled)");
				return false;
			}

			string text = File.ReadAllText(path, Encoding.UTF8);
			DiceFxConfig? config = JsonSerializer.Deserialize<DiceFxConfig>(text, Options);
			if (config?.Dice == null)
			{
				Report($"[DiceFX] dicefx.json parsed empty: {path} (previous table kept)");
				return false;
			}

			if (config.Version != SupportedVersion)
			{
				Report($"[DiceFX] dicefx.json version {config.Version} != {SupportedVersion}, loading anyway");
			}

			Normalize(config.Dice);

			if (knownDice != null && knownDice.Count > 0)
			{
				HashSet<string> known = new HashSet<string>(knownDice, StringComparer.OrdinalIgnoreCase);
				List<string> unknown = new List<string>();
				foreach (string key in config.Dice.Keys)
				{
					if (!known.Contains(key))
					{
						unknown.Add(key);
					}
				}
				if (unknown.Count > 0)
				{
					Report($"[DiceFX] dicefx.json has {unknown.Count} key(s) with no matching dice: {string.Join(", ", unknown)}");
				}
			}

			_profiles = config.Dice;
			Report($"[DiceFX] Loaded {_profiles.Count} fx profiles from {path}");
			return true;
		}
		catch (Exception ex)
		{
			Report($"[DiceFX] Failed to load dicefx.json ({path}): {ex.Message} (previous table kept)");
			return false;
		}
	}

	private static void Normalize(Dictionary<string, FxProfile> dice)
	{
		foreach (KeyValuePair<string, FxProfile> entry in dice)
		{
			FxProfile p = entry.Value;
			p.Trigger ??= new FxTriggerSlot();
			p.Attach ??= new FxAttachSlot();
			p.Orbit ??= new FxOrbitSlot();
			p.Trail ??= new FxTrailSlot();
			p.Geom ??= new FxGeom();
			p.Wing ??= new FxWingSlot();
			p.Events ??= new FxEvents();

			p.Attach.Particle ??= "";
			p.Orbit.Particle ??= "";
			p.Trail.Particle ??= "";
			p.Trigger.Burst ??= "";
			p.Trigger.Remove ??= "";
			p.Events.Fire ??= "";
			p.Events.Kill ??= "";
			p.Events.KillSelf ??= "";
			p.Events.KillHeadshot ??= "";
			p.Events.Death ??= "";
			p.Events.Hurt ??= "";
			p.Events.Hit ??= "";
			p.Events.HitHeadshot ??= "";
			p.Events.RoundStart ??= "";
			p.Events.RoundEnd ??= "";

			// guard non-sensical numbers (a 0/negative interval would proc every tick)
			if (!float.IsFinite(p.Attach.Z)) p.Attach.Z = 40f;
			if (!float.IsFinite(p.Orbit.Radius) || p.Orbit.Radius < 0f) p.Orbit.Radius = 70f;
			if (!float.IsFinite(p.Orbit.Speed)) p.Orbit.Speed = 2f;
			if (!float.IsFinite(p.Orbit.Z)) p.Orbit.Z = 40f;
			if (!float.IsFinite(p.Trail.Interval) || p.Trail.Interval <= 0f) p.Trail.Interval = 2f;
			if (!float.IsFinite(p.Trail.Z)) p.Trail.Z = 4f;
			if (!float.IsFinite(p.Geom.Behind)) p.Geom.Behind = 26f;
			if (!float.IsFinite(p.Geom.Spread)) p.Geom.Spread = 34f;
			if (!float.IsFinite(p.Geom.Back)) p.Geom.Back = 22f;
			if (!float.IsFinite(p.Geom.WingZ)) p.Geom.WingZ = 52f;

			if (p.Wing.Color == null || p.Wing.Color.Length < 3) p.Wing.Color = new[] { 255, 215, 0 };
			if (p.Wing.Blades < 1) p.Wing.Blades = 4;
			if (p.Wing.Blades > 8) p.Wing.Blades = 8;
			if (!float.IsFinite(p.Wing.Length) || p.Wing.Length < 10f) p.Wing.Length = 90f;
			if (!float.IsFinite(p.Wing.Width) || p.Wing.Width <= 0f) p.Wing.Width = 3f;
			if (!float.IsFinite(p.Wing.Flap) || p.Wing.Flap < 0f) p.Wing.Flap = 0.6f;

			NormalizeBeams(p.Beams);
		}
	}

	private static void NormalizeBeams(FxBeams? beams)
	{
		if (beams == null)
		{
			return;
		}
		NormalizeBeamList(beams.Trigger);
		NormalizeBeamList(beams.Remove);
		NormalizeBeamList(beams.Kill);
		NormalizeBeamList(beams.KillSelf);
		NormalizeBeamList(beams.Death);
		NormalizeBeamList(beams.Hurt);
		NormalizeBeamList(beams.Hit);
		NormalizeBeamList(beams.RoundStart);
		NormalizeBeamList(beams.RoundEnd);
	}

	private static void NormalizeBeamList(List<FxBeamSpec>? list)
	{
		if (list == null)
		{
			return;
		}
		for (int i = list.Count - 1; i >= 0; i--)
		{
			FxBeamSpec? s = list[i];
			if (s == null)
			{
				list.RemoveAt(i);
				continue;
			}
			s.Shape = string.IsNullOrWhiteSpace(s.Shape) ? "ring" : s.Shape.Trim().ToLowerInvariant();
			if (s.Color == null || s.Color.Length < 3)
			{
				s.Color = new[] { 255, 215, 0 };
			}
			if (!float.IsFinite(s.Width) || s.Width <= 0f) s.Width = 3f;
			if (!float.IsFinite(s.Life) || s.Life <= 0f) s.Life = 2f;
			if (!float.IsFinite(s.Spin)) s.Spin = 1.5f;
			if (!float.IsFinite(s.Grow)) s.Grow = 0f;
			if (!float.IsFinite(s.Radius) || s.Radius <= 0f) s.Radius = 180f;
			if (!float.IsFinite(s.Outer) || s.Outer < 0f) s.Outer = 0f;
			if (s.Segments < 3) s.Segments = 24;
			if (s.Segments > 64) s.Segments = 64;
			if (s.Spokes < 0) s.Spokes = 8;
			if (s.Spokes > 32) s.Spokes = 32;
			if (!float.IsFinite(s.Z)) s.Z = 4f;
			if (!float.IsFinite(s.Height) || s.Height <= 0f) s.Height = 500f;
		}
	}

	private static void Report(string message)
	{
		try
		{
			Console.WriteLine(message);
		}
		catch
		{
		}
		try
		{
			RollTheDice.LogErr(message + "\n");
		}
		catch
		{
		}
	}
}
