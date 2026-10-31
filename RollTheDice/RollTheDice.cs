using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Configs;
using RollTheDice.Dices;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice;

public class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
{
	private MapConfig _currentMapConfig = new MapConfig();

	private Dictionary<ulong, PlayerConfig> _playerConfigs = new Dictionary<ulong, PlayerConfig>();

	private readonly List<string> _precacheModels;

	public const string RoundBackupPrefix = "rtd";

	private static string? _currentRoundBackupFile;

	private string _currentMap;

	private readonly Dictionary<CCSPlayerController, int> _playersThatRolledTheDice;

	private readonly Dictionary<CCSPlayerController, int> _PlayerCooldown;

	private readonly List<DiceBlueprint> _dices;

	private readonly Dictionary<CCSPlayerController, string> _originalPlayerNames;

	private bool _isDuringRound;

	private readonly Random _random;

	private const int DefaultMaxDicePerPlayer = 1;

	private readonly List<(float End, List<IBeamGroup> Groups, Vector Center)> _sigilTests = new List<(float End, List<IBeamGroup> Groups, Vector Center)>();

	private bool _sigilTimerStarted;

	public required PluginConfig Config { get; set; }

	public override string ModuleName => "Roll The Dice";

	public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

	public static RollTheDice? Instance { get; private set; }

	public override string ModuleVersion => "26.05.1";

	[ConsoleCommand("givedice", "Give Dice to player")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(1, "<player> [dice]")]
	public void CommandGiveDice(CCSPlayerController player, CommandInfo command)
	{
		string playerName = command.GetArg(1);
		string arg = command.GetArg(2);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.givedice.availabledices"]);
			{
				foreach (DiceBlueprint dix in _dices)
				{
					command.ReplyToCommand("- " + dix.ClassName);
				}
				return;
			}
		}
		bool isWildcard = string.IsNullOrWhiteSpace(playerName) || playerName == "*";
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && !p.IsBot
			where isWildcard || ((CBasePlayerController)p).PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase)
			select p).ToList();
		if (list.Count == 0)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.givedice.noplayers"]);
			return;
		}
		if (!isWildcard && list.Count > 1)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.givedice.toomanyplayers"]);
			return;
		}
		foreach (CCSPlayerController item in list)
		{
			var (text, text2) = RollTheDiceForPlayer(item, string.IsNullOrWhiteSpace(arg) ? null : arg);
			if (!string.IsNullOrEmpty(text))
			{
				IncrementDiceRollCount(item);
				PlayDiceSoundForPlayer(item, text);
			}
		}
	}

	// 粒子菜单：类别关键词 → (粒子, 默认位置)。方便按设计框架预览各类特效。
	private static readonly Dictionary<string, (string Particle, string Mode)> EffectPresets = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
	{
		{ "muzzle", (ParticlePaths.MuzzleSpark, "crosshair") },
		{ "tracer", (ParticlePaths.MuzzlePistol, "crosshair") },
		{ "kill", (ParticlePaths.FireCoverage, "near") },
		{ "killer", (ParticlePaths.ShellRifle, "self") },
		{ "headshot", (ParticlePaths.BloodHeadshot, "near") },
		{ "hurt", (ParticlePaths.Blood, "self") },
		{ "hit", (ParticlePaths.ImpactArmor, "near") },
		{ "aura", (ParticlePaths.ShieldGlow, "self") },
		{ "orbit", (ParticlePaths.GoldHaloFlare, "self") },
		{ "trail", (ParticlePaths.FireTiny, "near") },
		{ "roundstart", (ParticlePaths.ExperienceAward, "self") },
		{ "roundend", (ParticlePaths.ExperienceMax, "self") },
		{ "death", (ParticlePaths.ExplosionHegrenade, "near") },
	};

	[ConsoleCommand("rtdeffect", "Play a particle effect (debug / particle menu)")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(1, "<particles/....vpcf | preset> [self|crosshair|near] [seconds]")]
	public void CommandEffect(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		string arg = command.GetArg(1);
		if (string.IsNullOrWhiteSpace(arg) || string.Equals(arg, "menu", StringComparison.OrdinalIgnoreCase))
		{
			command.ReplyToCommand("rtdeffect <particles/....vpcf | preset> [self|crosshair|near] [seconds]");
			command.ReplyToCommand("preset: " + string.Join(", ", EffectPresets.Keys));
			return;
		}
		string mode = command.GetArg(2).ToLowerInvariant();
		string path = arg;
		if (EffectPresets.TryGetValue(arg, out var preset))
		{
			path = preset.Particle;
			if (string.IsNullOrEmpty(mode))
			{
				mode = preset.Mode;
			}
		}
		float seconds = 3f;
		if (float.TryParse(command.GetArg(3), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && parsed > 0f)
		{
			seconds = parsed;
		}
		CParticleSystem system = mode switch
		{
			"self" => Effects.PlayOnPlayer(player, path, seconds),
			"near" => Effects.Play(player.PlayerPawn?.Value?.AbsOrigin, path, seconds),
			_ => Effects.PlayAtCrosshair(player, path, 300f, seconds)
		};
		string normalized = Effects.Normalize(path);
		command.ReplyToCommand(system != null ? $"OK {normalized}" : $"FAILED {normalized}");
	}

	[ConsoleCommand("rtdsigil", "Spawn test sigils at your feet (debug): [style] [radius] [density] [seconds]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[style] [radius] [density] [seconds]")]
	public void CommandSigil(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		Vector origin = player.PlayerPawn?.Value?.AbsOrigin;
		if (origin == null)
		{
			command.ReplyToCommand("rtdsigil: no pawn origin");
			return;
		}
		Vector center = new Vector(origin.X, origin.Y, origin.Z);
		// 第一个参数不是数字就当风格名（arcane / ward / astrolabe / sun / mixed）。
		int arg = 1;
		string styleName = "arcane";
		string first = command.GetArg(1);
		if (!string.IsNullOrEmpty(first) && !float.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
		{
			styleName = first;
			arg = 2;
		}
		float radius = SigilArgFloat(command.GetArg(arg), 520f);
		float density = SigilArgFloat(command.GetArg(arg + 1), 1f);
		float seconds = SigilArgFloat(command.GetArg(arg + 2), 20f);

		SigilParams parameters = new SigilParams { Density = density };
		SigilPalette palette = new SigilPalette(Color.FromArgb(255, 255, 248, 220), Color.FromArgb(255, 255, 205, 40), Color.FromArgb(255, 255, 180, 30));
		List<(IBeamGroup Group, Vector Center, string Label, int Beams)> spawned = new List<(IBeamGroup, Vector, string, int)>();

		if (SigilStyles.IsMixed(styleName))
		{
			// 四宫格：四种风格各摆一个，方便一眼比对（半径自动缩小，免得挤在一起）。
			SigilStyle[] all = new SigilStyle[] { SigilStyle.Arcane, SigilStyle.Ward, SigilStyle.Astrolabe, SigilStyle.Sun };
			string[] names = new string[] { "arcane", "ward", "astrolabe", "sun" };
			float r = radius * 0.42f;
			float offset = r * 1.15f;
			for (int i = 0; i < all.Length; i++)
			{
				Vector c = new Vector(center.X + ((i % 2 == 0) ? -offset : offset), center.Y + ((i < 2) ? -offset : offset), center.Z);
				SigilShapeGroup group = new SigilShapeGroup();
				group.AddRange(SigilStyles.Build(all[i], c, r, parameters, palette, 2.4f, 0.9f, 1f, true));
				group.Update(c, Server.CurrentTime, 1f);
				spawned.Add((group, c, names[i], group.BeamCount));
			}
		}
		else
		{
			SigilStyle style = SigilStyles.Parse(styleName);
			if (style == SigilStyle.Arcane)
			{
				ArcaneGroundSigil sigil = new ArcaneGroundSigil(center, radius, parameters, palette, 2.4f);
				ArcaneTower tower = new ArcaneTower(center, radius * 1.1f, parameters, palette, 2.4f, 8, 300f, 240f, 0f, 0.5f);
				spawned.Add((sigil, center, "ground", sigil.BeamCount));
				spawned.Add((tower, center, "tower", tower.BeamCount));
			}
			else
			{
				SigilShapeGroup group = new SigilShapeGroup();
				group.AddRange(SigilStyles.Build(style, center, radius, parameters, palette, 2.4f, 0.9f, 1f, true));
				group.Update(center, Server.CurrentTime, 1f);
				spawned.Add((group, center, styleName, group.BeamCount));
			}
		}

		int total = 0;
		foreach ((IBeamGroup group, Vector c, string label, int beams) in spawned)
		{
			_sigilTests.Add((Server.CurrentTime + seconds, new List<IBeamGroup> { group }, c));
			total += beams;
		}
		if (!_sigilTimerStarted)
		{
			_sigilTimerStarted = true;
			AddTimer(0.05f, SigilTestTick, TimerFlags.REPEAT);
		}
		string breakdown = string.Join(", ", spawned.Select((s) => $"{s.Label} {s.Beams}"));
		command.ReplyToCommand($"[rtdsigil] {styleName} beams = {total} ({breakdown}), radius {radius:0}, density {density:0.##}, {seconds:0}s");
	}

	private static float SigilArgFloat(string arg, float fallback)
	{
		return float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && parsed > 0f ? parsed : fallback;
	}

	/// <summary>
	/// `rtddome [秒]`：在脚下直接生成「坠落天空」的**浮空装置法阵**（三层轮盘 + 外缘符文带 + 环形轨道 +
	/// 4 个倾斜侧阵门 + 中心主轴/立体核心 + 支撑柱 + 锚线 + 能量柱），最后 3 秒走一遍发动脉冲。
	/// 用于**不用等 20s 施法 / 90s 冷却**地快速调外观。
	/// </summary>
	[ConsoleCommand("rtddome", "Preview the falling-sky floating sigil at your feet (debug): [seconds]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[seconds]")]
	public void CommandDome(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		Vector origin = player.PlayerPawn?.Value?.AbsOrigin;
		if (origin == null)
		{
			command.ReplyToCommand("rtddome: no pawn origin");
			return;
		}
		float seconds = SigilArgFloat(command.GetArg(1), 12f);
		FinalJudgmentConfig cfg = Config.Dices.FinalJudgment;
		Vector center = new Vector(origin.X, origin.Y, origin.Z);
		SigilPalette palette = new SigilPalette(
			Color.FromArgb(255, 228, 242, 255),
			Color.FromArgb(255, 188, 218, 245),
			Color.FromArgb(255, 92, 140, 190));
		SigilDomeOptions options = new SigilDomeOptions
		{
			Twist = cfg.DomeTwist,
			Crown = cfg.DomeCrown,
			Rim = cfg.DomeRim,
			Discs = cfg.DomeDiscs,
			Anchors = cfg.DomeAnchors,
			Gates = cfg.DomeGates,
			GateTilt = cfg.DomeGateTilt,
			GateSize = cfg.DomeGateSize,
			Tracks = cfg.DomeTracks,
			Axis = cfg.DomeAxis,
			Style = cfg.DomeStyle,
			AnimationStride = 1
		};
		SigilDome dome = new SigilDome(center, cfg.DomeRadius, palette, cfg.SigilWidth, cfg.DomeSpin, cfg.DomeGrowSeconds, Server.CurrentTime, options);
		_sigilTests.Add((Server.CurrentTime + seconds, new List<IBeamGroup> { dome }, center));
		if (!_sigilTimerStarted)
		{
			_sigilTimerStarted = true;
			AddTimer(0.05f, SigilTestTick, TimerFlags.REPEAT);
		}
		command.ReplyToCommand($"[rtddome] {cfg.DomeDiscs} 层轮盘 / 侧阵门 {(cfg.DomeGates ? $"{cfg.DomeGateTilt:0}°/{(int)(cfg.DomeGateSize * 100)}%" : "off")} / 轨道 {(cfg.DomeTracks ? "on" : "off")} / 主轴 {(cfg.DomeAxis ? "on" : "off")} / 外缘 {(cfg.DomeRim ? "on" : "off")} / 锚线 {cfg.DomeAnchors} / style={cfg.DomeStyle} / beams = {dome.BeamCount}, radius {cfg.DomeRadius:0}, {seconds:0}s");
	}

	/// <summary>`rtdsky [秒]`：在头顶铺一遍「坠落天空」的天空法阵群（含风格轮换与预算降采样）。</summary>
	[ConsoleCommand("rtdsky", "Preview the falling-sky sky sigil field overhead (debug): [seconds] [count]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[seconds] [count]")]
	public void CommandSky(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		Vector origin = player.PlayerPawn?.Value?.AbsOrigin;
		if (origin == null)
		{
			command.ReplyToCommand("rtdsky: no pawn origin");
			return;
		}
		float seconds = SigilArgFloat(command.GetArg(1), 12f);
		FinalJudgmentConfig cfg = Config.Dices.FinalJudgment;
		int count = (int)SigilArgFloat(command.GetArg(2), cfg.SkyCount);
		Vector center = new Vector(origin.X, origin.Y, origin.Z);
		SigilPalette palette = new SigilPalette(
			Color.FromArgb(255, 245, 252, 255),
			Color.FromArgb(255, 158, 214, 255),
			Color.FromArgb(255, 66, 126, 198));
		SigilParams parameters = SigilParams.From(cfg.SigilDensity, cfg.RuneTicks, cfg.TickRingCount, cfg.StarPoints, cfg.StarSkip, cfg.PolygonSides, cfg.DoubleLine, cfg.SigilSeed);
		SkySigilOptions options = new SkySigilOptions
		{
			Style = cfg.SkyStyle,
			Detail = cfg.SkyDetail,
			AnimatedLayers = cfg.SkyAnimatedLayers,
			BeamBudget = cfg.SkyBeamBudget,
			AnimationStride = 1
		};
		SkySigilField sky = new SkySigilField(center, count, cfg.SkyHeightStart, cfg.SkyHeightEnd, cfg.SkyRadiusStart, cfg.SkyRadiusEnd, cfg.SkyRadiusAlternate, cfg.SkyStartDelay, cfg.SkyLayerDelay, cfg.SkyGrowSeconds, cfg.SkySpin, parameters, palette, cfg.SigilWidth, Server.CurrentTime, options);
		_sigilTests.Add((Server.CurrentTime + seconds, new List<IBeamGroup> { sky }, center));
		if (!_sigilTimerStarted)
		{
			_sigilTimerStarted = true;
			AddTimer(0.05f, SigilTestTick, TimerFlags.REPEAT);
		}
		// 各层是"到点才建"的，等铺完再回显真实束数（回调里别用 command，改用玩家）。
		float reportAt = MathF.Max(cfg.SkyStartDelay + count * cfg.SkyLayerDelay + 0.6f, 1f);
		AddTimer(reportAt, () =>
		{
			if (player != null && player.IsValid)
			{
				player.PrintToChat($" [RTD] sky beams = {sky.BeamCount}（{count} 层 / detail {cfg.SkyDetail:0.##} / anim {cfg.SkyAnimatedLayers} / budget {cfg.SkyBeamBudget}）");
			}
		});
		command.ReplyToCommand($"[rtdsky] style={cfg.SkyStyle} / count={count} / detail={cfg.SkyDetail:0.##} / anim={cfg.SkyAnimatedLayers} / budget={cfg.SkyBeamBudget} / {seconds:0}s 开始铺阵…");
	}

	/// <summary>
	/// `rtdex [秒]`：预览「誓约胜利之剑」——光剑由光丝拼凑成形 + 法阵收拢 + 汇聚流，到点自动朝准星倾泻光炮。
	/// 0 = 跳过蓄力按满威力立即释放。无需持有 dice、无视冷却。
	/// </summary>
	[ConsoleCommand("rtdex", "Preview Excalibur charge & blast (debug): [seconds]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[seconds]")]
	public void CommandExcalibur(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		Excalibur dice = _dices.OfType<Excalibur>().FirstOrDefault();
		if (dice == null)
		{
			command.ReplyToCommand("rtdex: Excalibur dice not loaded");
			return;
		}
		float seconds = SigilArgFloat(command.GetArg(1), 3f);
		dice.DebugCast(player, seconds);
		command.ReplyToCommand($"[rtdex] 蓄力 {seconds:0.##}s → 朝准星倾泻（0 = 立即）");
	}

	/// <summary>
	/// `rtdpillar [秒] [半径]`：在脚下前方直接竖一遍「坠落天空」的通天光柱（单根巨柱），
	/// 用于**不等 20s 施法、不看冷却**地调光柱外观；半径可临时覆盖配置。`rtdpillar off` 立刻拆。
	/// </summary>
	[ConsoleCommand("rtdpillar", "Preview the falling-sky beam column (debug): [seconds] [radius]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[seconds] [radius]")]
	public void CommandPillar(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		if (string.Equals(command.GetArg(1), "off", StringComparison.OrdinalIgnoreCase))
		{
			command.ReplyToCommand($"[rtdpillar] removed {ClearPillarTests()}");
			return;
		}
		CCSPlayerPawn? pawn = player.PlayerPawn?.Value;
		Vector? feet = pawn?.AbsOrigin;
		if (pawn == null || !pawn.IsValid || feet == null)
		{
			command.ReplyToCommand("rtdpillar: no pawn origin");
			return;
		}
		FinalJudgmentConfig cfg = Config.Dices.FinalJudgment;
		float seconds = SigilArgFloat(command.GetArg(1), 8f);
		float radius = SigilArgFloat(command.GetArg(2), cfg.PillarRadius);
		// 放在前方、离得比柱半径还远一点，免得一开始就站在柱子里看不清。
		float yawRad = pawn.EyeAngles.Y * MathF.PI / 180f;
		float dist = MathF.Max(2600f, radius * 1.6f);
		Vector center = new Vector(feet.X + MathF.Cos(yawRad) * dist, feet.Y + MathF.Sin(yawRad) * dist, feet.Z);
		Color core = Color.FromArgb(255, 245, 252, 255);
		ExpandingBeamColumn column = new ExpandingBeamColumn(center, cfg.PillarHeight, cfg.BeamRadiusStart, radius,
			seconds, cfg.BeamExpandSeconds, core, Server.CurrentTime,
			cfg.BeamHaloScale, cfg.BeamFlags, cfg.BeamScrollSpeed, 0f, cfg.BeamVisualScale);
		_sigilTests.Add((Server.CurrentTime + seconds, new List<IBeamGroup> { column }, center));
		if (!_sigilTimerStarted)
		{
			_sigilTimerStarted = true;
			AddTimer(0.05f, SigilTestTick, TimerFlags.REPEAT);
		}
		command.ReplyToCommand($"[rtdpillar] 半径 {radius:0} / 高 {cfg.PillarHeight:0} / 扩张 {cfg.BeamExpandSeconds:0.##}s / {seconds:0}s");
	}

	private int ClearPillarTests()
	{
		int removed = 0;
		for (int i = _sigilTests.Count - 1; i >= 0; i--)
		{
			bool any = false;
			foreach (IBeamGroup group in _sigilTests[i].Groups)
			{
				if (group is ExpandingBeamColumn)
				{
					group.Remove();
					removed++;
					any = true;
				}
			}
			if (any)
			{
				_sigilTests.RemoveAt(i);
			}
		}
		return removed;
	}

	/// <summary>
	/// `rtdinfo [玩家名]`：打印某玩家（默认自己）的 dice 内部状态——本轮已抽次数/上限及**来源拆解**
	/// （转世 `PendingExtraDice` / 世界 `PendingExtraRolls` / 女神祝福 / 黑骑禁抽）、当前生效 dice 列表、
	/// TAB 名字前缀缓存。专治"TAB 上 dice 数量对不上 / 额外 dice 不生效"。
	/// </summary>
	[ConsoleCommand("rtdinfo", "Print a player's internal dice state (debug): [name]")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	[CommandHelper(0, "[player name]")]
	public void CommandInfo(CCSPlayerController player, CommandInfo command)
	{
		CCSPlayerController? target = player;
		string wanted = command.GetArg(1);
		if (!string.IsNullOrWhiteSpace(wanted))
		{
			target = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.PlayerName.Contains(wanted, StringComparison.OrdinalIgnoreCase));
		}
		if (target == null || !target.IsValid)
		{
			command.ReplyToCommand("rtdinfo: player not found");
			return;
		}
		ulong sid = target.SteamID;
		int rolled = GetDiceRollCount(target);
		int max = GetMaxDiceCount(target);
		Reincarnation.PendingExtraDice.TryGetValue(sid, out int pendingReincarnation);
		World.PendingExtraRolls.TryGetValue(sid, out int pendingWorld);
		bool blessed = Goddess.ActiveThisRound && Goddess.BlessedPlayers.Contains(sid);
		bool denied = DeathKnightComplete.IsDeniedThisRound(sid);
		List<string> active = GetAllDiceForPlayer(target);
		string shown = target.PlayerName;
		string trueName = _originalPlayerNames.TryGetValue(target, out string cached) ? cached : StripDiceNameDecoration(shown);
		command.ReplyToCommand($"[rtdinfo] {shown} (sid {sid})");
		command.ReplyToCommand($"[rtdinfo] 本轮已抽 {rolled}/上限 {max} ← 基础1｜转世+{pendingReincarnation}｜世界+{pendingWorld}｜女神{(blessed ? "+1" : "+0")}｜黑骑禁抽{(denied ? "是" : "否")}");
		command.ReplyToCommand($"[rtdinfo] 生效 dice({active.Count}): {(active.Count == 0 ? "-" : string.Join(", ", active))}");
		command.ReplyToCommand($"[rtdinfo] 名字：真名缓存={trueName}｜缓存中{(_originalPlayerNames.ContainsKey(target) ? "有" : "无")}（TAB 前缀是显示用，含队友 combo 的 dice 名）");
	}

	private void SigilTestTick()
	{
		if (_sigilTests.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		for (int i = _sigilTests.Count - 1; i >= 0; i--)
		{
			(float end, List<IBeamGroup> groups, Vector center) = _sigilTests[i];
			if (now >= end)
			{
				foreach (IBeamGroup group in groups)
				{
					group.Remove();
				}
				_sigilTests.RemoveAt(i);
				continue;
			}
			foreach (IBeamGroup group in groups)
			{
				if (group is SigilShapeGroup shapes)
				{
					shapes.Update(center, now, 1f);
				}
				else if (group is ArcaneGroundSigil ground)
				{
					ground.Update(center, now, 1f);
				}
				else if (group is ArcaneTower tower)
				{
					tower.Update(center, now, 1f);
				}
				else if (group is SigilDome dome)
				{
					dome.Update(center, now, 1f);
					// 最后 3 秒走一遍发动脉冲，方便看"发动信号"。
					float remain = end - now;
					dome.SetPulse(remain < 3f ? Math.Clamp(1f - remain / 3f, 0f, 1f) : 0f);
				}
				else if (group is SkySigilField sky)
				{
					sky.Update(center, now, 1f);
				}
				else if (group is ExpandingBeamColumn column)
				{
					// 到期由上面的 End 统一 Remove（构造/摆放已在 Update 里做）。
					column.Update(now);
				}
			}
		}
	}

	[ConsoleCommand("rtd", "Roll the Dice")]
	[ConsoleCommand("dice", "Roll the Dice")]
	[CommandHelper(0, "")]
	public void CommandRollTheDice(CCSPlayerController player, CommandInfo command)
	{
		if (player == null || !((CEntityInstance)player).IsValid)
		{
			command.ReplyToCommand("This command must be run by a player.");
			return;
		}
		if (!Config.Enabled || !_currentMapConfig.Enabled || _dices.Count == 0)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["core.disabled"]);
			return;
		}
		if (command.GetArg(1) == "auto")
		{
			if (!_playerConfigs.TryGetValue(((CBasePlayerController)player).SteamID, out PlayerConfig value))
			{
				value = new PlayerConfig();
				_playerConfigs.Add(((CBasePlayerController)player).SteamID, value);
			}
			value.RtdOnSpawn = !value.RtdOnSpawn;
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer[value.RtdOnSpawn ? "command.rollthedice.rtdonspawn.enabled" : "command.rollthedice.rtdonspawn.disabled"]);
			return;
		}
		if (!Config.AllowRtdDuringWarmup && GameRules.Get("WarmupPeriod") is bool warmup && warmup)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.rollthedice.iswarmup"]);
			return;
		}
		if (!_isDuringRound)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.rollthedice.noactiveround"]);
			return;
		}
		if (GetDiceRollCount(player) >= GetMaxDiceCount(player))
		{
			string text = ((BasePlugin)this).Localizer["command.rollthedice.alreadyrolled"].Value.Replace("{dice}", GetLastDiceDescription(player));
			command.ReplyToCommand(text);
			return;
		}
		if (_PlayerCooldown.TryGetValue(player, out var value2))
		{
			if (Config.CooldownRounds > 0 && value2 > 0)
			{
				command.ReplyToCommand(((BasePlugin)this).Localizer["command.rollthedice.cooldown.rounds"].Value.Replace("{rounds}", value2.ToString()));
				return;
			}
			int num2 = value2 - (int)Server.CurrentTime;
			if (Config.CooldownSeconds > 0 && num2 > 0)
			{
				command.ReplyToCommand(((BasePlugin)this).Localizer["command.rollthedice.cooldown.seconds"].Value.Replace("{seconds}", num2.ToString()));
				return;
			}
		}
		CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
		byte? obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value3 = playerPawn.Value;
			obj = ((value3 != null) ? new byte?(((CBaseEntity)value3).LifeState) : ((byte?)null));
		}
		if (obj != 0)
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.rollthedice.notalive"]);
			return;
		}
		if (Config.PriceToDice > 0)
		{
			if (player.InGameMoneyServices == null || player.InGameMoneyServices.Account < Config.PriceToDice)
			{
				command.ReplyToCommand(((BasePlugin)this).Localizer["command.rollthedice.notenoughmoney"].Value.Replace("{money}", Config.PriceToDice.ToString()));
				return;
			}
			player.InGameMoneyServices.Account -= Config.PriceToDice;
			Utilities.SetStateChanged((CBaseEntity)(object)player, "CCSPlayerController", "m_pInGameMoneyServices", 0);
		}
		var (text2, text3) = RollTheDiceForPlayer(player);
		if (string.IsNullOrEmpty(text2))
		{
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["command.rollthedice.unlucky"]);
			return;
		}
		IncrementDiceRollCount(player);
		if (Config.CooldownRounds > 0)
		{
			_PlayerCooldown[player] = Config.CooldownRounds;
		}
		else if (Config.CooldownSeconds > 0)
		{
			_PlayerCooldown[player] = (int)Server.CurrentTime + Config.CooldownSeconds;
		}
		PlayDiceSoundForPlayer(player, text2, command: true);
	}

	[ConsoleCommand("rollthedice", "RollTheDice admin commands")]
	[CommandHelper(1, "<reload|disable|enable|createmapconfig|deletemapconfig>")]
	public void CommandAdmin(CCSPlayerController player, CommandInfo command)
	{
		string arg = command.GetArg(1);
		switch (arg.ToLower(CultureInfo.CurrentCulture))
		{
		case "reload":
			PluginConfigExtensions.Reload<PluginConfig>(Config);
			LoadMapConfig(_currentMap);
			ReloadFxTable();
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["admin.reload"]);
			break;
		case "disable":
			Config.Enabled = false;
			PluginConfigExtensions.Update<PluginConfig>(Config);
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["admin.disable"]);
			break;
		case "enable":
			Config.Enabled = true;
			PluginConfigExtensions.Update<PluginConfig>(Config);
			command.ReplyToCommand((string?)((BasePlugin)this).Localizer["admin.enable"]);
			break;
		case "createmapconfig":
			Config.MapConfigs[_currentMap] = _currentMapConfig;
			PluginConfigExtensions.Update<PluginConfig>(Config);
			command.ReplyToCommand(((BasePlugin)this).Localizer["admin.mapconfig.created"].Value.Replace("{mapName}", _currentMap));
			break;
		case "deletemapconfig":
			Config.MapConfigs.Remove(_currentMap);
			PluginConfigExtensions.Update<PluginConfig>(Config);
			command.ReplyToCommand(((BasePlugin)this).Localizer["admin.mapconfig.deleted"].Value.Replace("{mapName}", _currentMap));
			break;
		default:
			command.ReplyToCommand(((BasePlugin)this).Localizer["admin.unknown_command"].Value.Replace("{command}", arg));
			break;
		}
	}

	[ConsoleCommand("rtdfx", "RollTheDice fx table admin commands")]
	[CommandHelper(1, "<reload>")]
	[RequiresPermissions(new string[] { "@rollthedice/admin" })]
	public void CommandFxTable(CCSPlayerController player, CommandInfo command)
	{
		string arg = command.GetArg(1);
		if (string.Equals(arg, "reload", StringComparison.OrdinalIgnoreCase))
		{
			ReloadFxTable();
			command.ReplyToCommand("[DiceFX] dicefx.json reloaded (see server console/log for details)");
			return;
		}
		command.ReplyToCommand("rtdfx <reload>");
	}

	private void ReloadConfigFromDisk()
	{
		try
		{
			PluginConfigExtensions.Reload<PluginConfig>(Config);
			UpdatePlayerConfig();
			PluginConfigExtensions.Update<PluginConfig>(Config);
		}
		catch (Exception ex)
		{
			string text = ((BasePlugin)this).Localizer["core.error"].Value.Replace("{error}", ex.Message);
			Console.WriteLine(text);
			Server.PrintToChatAll(text);
		}
	}

	/// <summary>特效表路径：&lt;css&gt;/configs/plugins/RollTheDice/dicefx.json。</summary>
	private string FxDiceFxPath()
	{
		DirectoryInfo pluginDir = new DirectoryInfo(ModuleDirectory);
		string cssRoot = pluginDir.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
		return Path.Combine(cssRoot, "configs", "plugins", Path.GetFileName(ModuleDirectory), "dicefx.json");
	}

	/// <summary>重载 dicefx.json（fail-soft：失败保留上一份内存表）。</summary>
	private void ReloadFxTable()
	{
		try
		{
			List<string> known = new List<string>(_dices.Count);
			foreach (DiceBlueprint dice in _dices)
			{
				known.Add(dice.ClassName);
			}
			if (DiceFxTable.Reload(FxDiceFxPath(), known))
			{
				DiceEffects.ResetPersistent();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("[DiceFX] reload failed: " + ex.Message);
		}
	}

	private void LoadMapConfig(string mapName)
	{
		_currentMapConfig = (from mapConfig in Config.MapConfigs
			where FileSystemName.MatchesSimpleExpression(mapConfig.Key.AsSpan(), mapName.AsSpan())
			select mapConfig.Value).FirstOrDefault() ?? new MapConfig
		{
			Dices = Config.Dices
		};
		Console.WriteLine(((BasePlugin)this).Localizer["core.mapconfig"].Value.Replace("{mapName}", mapName));
	}

	public void OnConfigParsed(PluginConfig config)
	{
		Config = config;
		DiceEffects.Enabled = config.Effects?.Enabled ?? true;
		DiceEffects.TrailsEnabled = config.Effects?.Trails ?? true;
		Console.WriteLine(((BasePlugin)this).Localizer["core.config"]);
	}

	private void UpdatePlayerConfig()
	{
		if (_playerConfigs.Count == 0)
		{
			_playerConfigs = Config.PlayerConfigs;
		}
		else
		{
			Config.PlayerConfigs = _playerConfigs;
		}
	}

	private static object ConvertJsonElement(object element)
	{
		if (element is JsonElement { ValueKind: var valueKind } jsonElement)
		{
			if (1 == 0)
			{
			}
			object result = valueKind switch
			{
				JsonValueKind.String => jsonElement.GetString() ?? string.Empty, 
				JsonValueKind.Number => jsonElement.TryGetSingle(out var value) ? value : 0f, 
				JsonValueKind.True => jsonElement.GetBoolean(), 
				JsonValueKind.False => jsonElement.GetBoolean(), 
				JsonValueKind.Object => jsonElement.EnumerateObject().ToDictionary((JsonProperty property) => property.Name, (JsonProperty property) => ConvertJsonElement(property.Value)), 
				JsonValueKind.Array => (from jsonElement2 in jsonElement.EnumerateArray()
					select ConvertJsonElement(jsonElement2)).ToList(), 
				JsonValueKind.Undefined => string.Empty, 
				_ => string.Empty, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
		return element;
	}

	private void OnServerPrecacheResources(ResourceManifest manifest)
	{
		foreach (string randomModel in _currentMapConfig.Dices.NoExplosives.RandomModels)
		{
			if (!string.IsNullOrEmpty(randomModel) && !_precacheModels.Contains(randomModel))
			{
				manifest.AddResource(randomModel);
			}
		}
		if (!string.IsNullOrEmpty(Config.Precache.SoundEventFile))
		{
			manifest.AddResource(Config.Precache.SoundEventFile);
		}
		if (Config.Sounds?.LegendarySounds != null)
		{
			foreach (string sound in Config.Sounds.LegendarySounds.Values)
			{
				PrecacheSound(manifest, sound);
			}
		}
		PrecacheSound(manifest, Config.Sounds?.DiceRollSound);
		PrecacheSound(manifest, Config.Dices?.Supernova?.BlastSound);
		PrecacheSound(manifest, Config.Dices?.FinalJudgment?.ExplosionSound);
		PrecacheSound(manifest, Config.Dices?.Excalibur?.ChargeSound);
		PrecacheSound(manifest, Config.Dices?.Excalibur?.ChargeLoopSound);
		PrecacheSound(manifest, Config.Dices?.Excalibur?.ReadySound);
		PrecacheSound(manifest, Config.Dices?.Excalibur?.BurstSound);
		PrecacheSound(manifest, Config.Dices?.Phoenix?.TriggerSound);
		PrecacheSound(manifest, Config.Dices?.Phoenix?.FinaleSound);
		PrecacheSound(manifest, Config.Dices?.WheelOfFate?.ReviveSound);
		manifest.AddResource("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
		// 誓约胜利之剑的实体剑模型（+ 冰剑材质；换模型/新装素材后要换图才生效）。
		if (!string.IsNullOrWhiteSpace(Config.Dices?.Excalibur?.SwordModel))
		{
			manifest.AddResource(Config.Dices.Excalibur.SwordModel.Trim());
		}
		manifest.AddResource("models/bullet/ice/tex/icicle2.vmat");
		manifest.AddResource("models/bullet/ice/tex/snow.vmat");
		Effects.PrecacheAll(manifest);
		int fxParticles = DiceFxTable.Precache(manifest);
		if (fxParticles > 0)
		{
			Console.WriteLine($"[DiceFX] Precache: {fxParticles} particle path(s) referenced by dicefx.json");
		}
		foreach (string precacheModel in _precacheModels)
		{
			manifest.AddResource(precacheModel);
		}
	}

	/// <summary>把运行期音效文件路径加进预缓存（soundevent 名如 <c>c4.explode</c> 不是文件，跳过）。</summary>
	private static void PrecacheSound(ResourceManifest manifest, string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		string sound = path.Trim().Replace('\\', '/');
		if (sound.EndsWith(".vsnd", StringComparison.OrdinalIgnoreCase) || sound.EndsWith(".vsndevts", StringComparison.OrdinalIgnoreCase))
		{
			manifest.AddResource(sound);
		}
	}

	public static string? GetRoundBackupFile()
	{
		return _currentRoundBackupFile;
	}

	public override void Load(bool hotReload)
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		Instance = this;
		ReloadConfigFromDisk();
		((BasePlugin)this).RegisterEventHandler<EventRoundStart>((GameEventHandler<EventRoundStart>)OnRoundStart, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventRoundFreezeEnd>((GameEventHandler<EventRoundFreezeEnd>)OnRoundFreezeEnd, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventRoundEnd>((GameEventHandler<EventRoundEnd>)OnRoundEnd, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventPlayerDeath>((GameEventHandler<EventPlayerDeath>)OnPlayerDeath, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventPlayerHurt>((GameEventHandler<EventPlayerHurt>)OnPlayerHurtReveal, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventWeaponFire>((GameEventHandler<EventWeaponFire>)OnWeaponFireCentral, (HookMode)1);
		((BasePlugin)this).RegisterEventHandler<EventPlayerDisconnect>((GameEventHandler<EventPlayerDisconnect>)OnPlayerDisconnect, (HookMode)1);
		((BasePlugin)this).RegisterListener<Listeners.OnMapStart>(new Listeners.OnMapStart(OnMapStart));
		((BasePlugin)this).RegisterListener<Listeners.OnMapEnd>(new Listeners.OnMapEnd(OnMapEnd));
		((BasePlugin)this).RegisterListener<Listeners.OnServerPrecacheResources>(new Listeners.OnServerPrecacheResources(OnServerPrecacheResources));
		((BasePlugin)this).RegisterListener<Listeners.OnPlayerButtonsChanged>(new Listeners.OnPlayerButtonsChanged(OnPlayerButtonsChanged));
		((BasePlugin)this).RegisterListener<Listeners.OnPlayerTakeDamagePre>(new Listeners.OnPlayerTakeDamagePre(OnPlayerTakeDamagePreCentral));
		((BasePlugin)this).RegisterListener<Listeners.OnTick>(new Listeners.OnTick(DiceEffects.OnTick));
		// 带父实体的子实体看门狗（FollowEntity / SetParent 造出来的 prop/粒子）：父失效即拆，避免悬垂父链崩溃。
		((BasePlugin)this).RegisterListener<Listeners.OnTick>(new Listeners.OnTick(ChildGuard.OnTick));
		RegisterCheatGuard();
		if (hotReload)
		{
			Console.WriteLine(((BasePlugin)this).Localizer["core.hotreload"]);
			_currentMap = Server.MapName;
			LoadMapConfig(_currentMap);
			_isDuringRound = true;
			InitializeModules();
		}
	}

	public override void Unload(bool hotReload)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		DestroyModules();
		DeregisterCheatGuard();
		ReloadConfigFromDisk();
		((BasePlugin)this).DeregisterEventHandler<EventRoundStart>((GameEventHandler<EventRoundStart>)OnRoundStart, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventRoundFreezeEnd>((GameEventHandler<EventRoundFreezeEnd>)OnRoundFreezeEnd, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventRoundEnd>((GameEventHandler<EventRoundEnd>)OnRoundEnd, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventPlayerDeath>((GameEventHandler<EventPlayerDeath>)OnPlayerDeath, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventPlayerHurt>((GameEventHandler<EventPlayerHurt>)OnPlayerHurtReveal, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventWeaponFire>((GameEventHandler<EventWeaponFire>)OnWeaponFireCentral, (HookMode)1);
		((BasePlugin)this).DeregisterEventHandler<EventPlayerDisconnect>((GameEventHandler<EventPlayerDisconnect>)OnPlayerDisconnect, (HookMode)1);
		((BasePlugin)this).RemoveListener<Listeners.OnMapStart>(new Listeners.OnMapStart(OnMapStart));
		((BasePlugin)this).RemoveListener<Listeners.OnMapEnd>(new Listeners.OnMapEnd(OnMapEnd));
		((BasePlugin)this).RemoveListener<Listeners.OnServerPrecacheResources>(new Listeners.OnServerPrecacheResources(OnServerPrecacheResources));
		((BasePlugin)this).RemoveListener<Listeners.OnPlayerButtonsChanged>(new Listeners.OnPlayerButtonsChanged(OnPlayerButtonsChanged));
		((BasePlugin)this).RemoveListener<Listeners.OnPlayerTakeDamagePre>(new Listeners.OnPlayerTakeDamagePre(OnPlayerTakeDamagePreCentral));
		((BasePlugin)this).RemoveListener<Listeners.OnTick>(new Listeners.OnTick(DiceEffects.OnTick));
		((BasePlugin)this).RemoveListener<Listeners.OnTick>(new Listeners.OnTick(ChildGuard.OnTick));
		ChildGuard.ClearAll();
		Effects.ClearAll();
		DiceEffects.ClearAll();
		// 卸载时彻底清掉静态/实例状态，避免 css_reload 后残留（旧实例的 Instance、玩家键集合、buff 域）。
		Instance = null;
		_playersThatRolledTheDice.Clear();
		_PlayerCooldown.Clear();
		_originalPlayerNames.Clear();
		Invulnerability.ClearAll();
		MoveLockManager.ClearAll();
		StackingHealth.ClearAll();
		DamageBonusManager.ClearAll();
		DamageReductionManager.ClearAll();
		SpeedBonusManager.ClearAll();
		Console.WriteLine(((BasePlugin)this).Localizer["core.unload"]);
	}

	/// <summary>
	/// CheatGuard 的每条被拦指令需要<em>各自独立</em>的委托实例。
	/// 原因：<c>BasePlugin.AddCommandListener</c> 内部用 <c>Dictionary&lt;Delegate,...&gt;</c> 以委托为键，
	/// 若所有被拦指令共用同一个 <c>OnCheatGuardCommand</c> 方法组委托，键会互相覆盖 →
	/// <c>RemoveCommandListener</c> 只能摘掉最后一个，其余原生钩子在卸载/热重载后泄漏并叠加。
	/// 每条指令包一个 <see cref="CheatGuardHook"/> 实例（Target 不同 → 委托不等）即可对称摘除。
	/// </summary>
	private sealed class CheatGuardHook
	{
		private readonly RollTheDice _plugin;

		public CheatGuardHook(RollTheDice plugin)
		{
			_plugin = plugin;
		}

		public HookResult Handle(CCSPlayerController? player, CommandInfo info)
		{
			return _plugin.OnCheatGuardCommand(player, info);
		}
	}

	private readonly List<(string Command, CommandInfo.CommandListenerCallback Handler)> _cheatGuardHooks = new List<(string, CommandInfo.CommandListenerCallback)>();

	private void RegisterCheatGuard()
	{
		CheatGuardConfig guard = Config?.CheatGuard;
		if (guard == null || !guard.Enabled || guard.BlockedCommands == null)
		{
			return;
		}
		foreach (string command in guard.BlockedCommands)
		{
			if (!string.IsNullOrWhiteSpace(command))
			{
				string name = command.Trim().ToLowerInvariant();
				CommandInfo.CommandListenerCallback handler = new CheatGuardHook(this).Handle;
				_cheatGuardHooks.Add((name, handler));
				((BasePlugin)this).AddCommandListener(name, handler, HookMode.Pre);
			}
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} CheatGuard: blocked {guard.BlockedCommands.Count} commands (bypass={guard.BypassPermission})\n");
	}

	private void DeregisterCheatGuard()
	{
		foreach ((string command, CommandInfo.CommandListenerCallback handler) in _cheatGuardHooks)
		{
			((BasePlugin)this).RemoveCommandListener(command, handler, HookMode.Pre);
		}
		_cheatGuardHooks.Clear();
	}

	private HookResult OnCheatGuardCommand(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || !((CEntityInstance)player).IsValid || player.IsBot || ((CBasePlayerController)player).IsHLTV)
		{
			return (HookResult)0;
		}
		CheatGuardConfig guard = Config?.CheatGuard;
		if (guard == null || !guard.Enabled)
		{
			return (HookResult)0;
		}
		if (!string.IsNullOrWhiteSpace(guard.BypassPermission) && AdminManager.PlayerHasPermissions(player, guard.BypassPermission))
		{
			return (HookResult)0;
		}
		int humanCount = Utilities.GetPlayers().Count((CCSPlayerController p) => p != null && ((CEntityInstance)p).IsValid && !p.IsBot && !((CBasePlayerController)p).IsHLTV);
		if (humanCount <= 1)
		{
			return (HookResult)0;
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} CheatGuard blocked '{info.GetCommandString}' from {((CBasePlayerController)player).PlayerName}\n");
		try
		{
			player.PrintToChat($" {((BasePlugin)this).Localizer["command.prefix"].Value}⛔ 该作弊指令已被禁用（仅房主可用）。");
		}
		catch
		{
		}
		return (HookResult)3;
	}

	private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		Server.ExecuteCommand("host_timescale 1.0");
		_playersThatRolledTheDice.Clear();
		Goddess.ActiveThisRound = false;
		Goddess.BlessedPlayers.Clear();
		World.PendingExtraRolls.Clear();
		Mimic.PendingCopy.Clear();
		Trickster.PendingFakeNames.Clear();
		Karma.BuffedPlayers.Clear();
		Plague.InfectedPlayers.Clear();
		GravityWell.ActiveWells.Clear();
		DeathKnightComplete.PromoteDenied();
		StackingHealth.ClearAll();
		MoveLockManager.ClearAll();
		_isDuringRound = true;
		try
		{
			RemoveDicesForPlayers();
		}
		catch (Exception ex)
		{
			LogErr($"{DateTime.Now:HH:mm:ss} OnRoundStart: RemoveDicesForPlayers error (non-fatal): {ex.Message}\n");
		}
		DamageBonusManager.ClearAll();
		DamageReductionManager.ClearAll();
		SpeedBonusManager.ClearAll();
		Effects.ClearAll();
		DiceEffects.ClearAll();
		GameRules.Refresh();
		object obj = GameRules.Get("WarmupPeriod");
		bool flag = default(bool);
		int num;
		if (!Config.AllowRtdDuringWarmup)
		{
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			_isDuringRound = false;
			return (HookResult)0;
		}
		try
		{
			CreateRoundBackup();
			Server.PrintToChatAll((string?)((BasePlugin)this).Localizer["core.announcement"]);
			LogDebug($"{DateTime.Now:HH:mm:ss} OnRoundStart: TriggerEvent={Config.DiceTrigger.TriggerEvent}, Force={Config.DiceTrigger.ForceAllPlayers}, dices={_dices.Count}\n");
			if (Config.DiceTrigger.TriggerEvent == DiceTriggerEvent.RoundStart)
			{
				RollTheDiceOnRoundStart(Config.DiceTrigger.ForceAllPlayers);
				if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
				{
					RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
				}
			}
		}
		catch (Exception ex2)
		{
			LogErr($"{DateTime.Now:HH:mm:ss} OnRoundStart dice roll error: {ex2.Message}\n");
		}
		return (HookResult)0;
	}

	private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		object obj = GameRules.Get("WarmupPeriod");
		bool flag = default(bool);
		int num;
		if (!Config.AllowRtdDuringWarmup)
		{
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			return (HookResult)0;
		}
		if (Config.DiceTrigger.TriggerEvent != DiceTriggerEvent.RoundFreezeEnd)
		{
			return (HookResult)0;
		}
		RollTheDiceOnRoundStart(Config.DiceTrigger.ForceAllPlayers);
		if (Config.DiceTrigger.RollTheDiceEveryXSeconds > 0)
		{
			RollTheDiceEveryXSeconds(Config.DiceTrigger.RollTheDiceEveryXSeconds);
		}
		return (HookResult)0;
	}

	private void RollTheDiceOnRoundStart(bool force = false)
	{
		List<CCSPlayerController> list = Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			int result;
			if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
			{
				CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
				byte? obj;
				if (pawn == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerPawn value2 = pawn.Value;
					obj = ((value2 != null) ? new byte?(((CBaseEntity)value2).LifeState) : ((byte?)null));
				}
				if (obj == 0)
				{
					result = ((force || (Config.DiceTrigger.AllowPlayerAutoRtd && _playerConfigs.ContainsKey(((CBasePlayerController)p).SteamID) && _playerConfigs[((CBasePlayerController)p).SteamID].RtdOnSpawn)) ? 1 : 0);
					goto IL_00e1;
				}
			}
			result = 0;
			goto IL_00e1;
			IL_00e1:
			return (byte)result != 0;
		}).ToList();
		if (list.Count == 0)
		{
			return;
		}
		int value = Utilities.GetPlayers().Count(delegate(CCSPlayerController p)
		{
			int result;
			if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
			{
				CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
				byte? obj;
				if (pawn == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerPawn value2 = pawn.Value;
					obj = ((value2 != null) ? new byte?(((CBaseEntity)value2).LifeState) : ((byte?)null));
				}
				result = ((obj == 0) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		});
		int count = list.Count;
		List<DiceBlueprint> drawablePool = GetDrawablePool();
		if (drawablePool.Count == 0)
		{
			return;
		}
		List<DiceBlueprint> list2 = GetNormalPool();
		if (list2.Count == 0)
		{
			list2 = drawablePool;
		}
		Dictionary<CCSPlayerController, DiceBlueprint> dictionary = new Dictionary<CCSPlayerController, DiceBlueprint>();
		List<DiceBlueprint> list3 = new List<DiceBlueprint>();
		foreach (CCSPlayerController item3 in list)
		{
			if (GetDiceRollCount(item3) >= GetMaxDiceCount(item3))
			{
				continue;
			}
			DiceBlueprint drawn = WeightedRandomDraw(drawablePool);
			if (drawn != null)
			{
				dictionary[item3] = drawn;
				if (drawn.IsSpecial && !list3.Any((DiceBlueprint s) => s.ClassName == drawn.ClassName))
				{
					list3.Add(drawn);
				}
			}
		}
		Dictionary<CCSPlayerController, DiceBlueprint> specialWinners = new Dictionary<CCSPlayerController, DiceBlueprint>();
		Dictionary<CCSPlayerController, DiceBlueprint> remaining = new Dictionary<CCSPlayerController, DiceBlueprint>();
		bool flag = list3.Count > 0;
		if (flag)
		{
			foreach (KeyValuePair<CCSPlayerController, DiceBlueprint> item4 in dictionary)
			{
				if (item4.Value.IsSpecial)
				{
					specialWinners[item4.Key] = item4.Value;
				}
				else
				{
					remaining[item4.Key] = item4.Value;
				}
			}
			foreach (CCSPlayerController item5 in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
			{
				int result;
				if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
				{
					CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
					byte? obj;
					if (pawn == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerPawn value2 = pawn.Value;
						obj = ((value2 != null) ? new byte?(((CBaseEntity)value2).LifeState) : ((byte?)null));
					}
					if (obj == 0 && !specialWinners.ContainsKey(p))
					{
						result = ((!remaining.ContainsKey(p)) ? 1 : 0);
						goto IL_00a4;
					}
				}
				result = 0;
				goto IL_00a4;
				IL_00a4:
				return (byte)result != 0;
			}))
			{
				remaining[item5] = WeightedRandomDraw(list2) ?? WeightedRandomDraw(drawablePool);
			}
			double num = 1.0;
			bool wolfCombo = dictionary.Values.Any((DiceBlueprint d) => d.ClassName == "Wolf");
			Func<DiceBlueprint, double> effectiveSecondRound = (DiceBlueprint d) => (double)d.SecondRoundProbability * ((wolfCombo && d.ClassName == "WolfKing") ? 2.0 : 1.0);
			foreach (DiceBlueprint item6 in list3)
			{
				num *= 1.0 - effectiveSecondRound(item6);
			}
			double num2 = 1.0 - num;
			double num3 = ((IEnumerable<DiceBlueprint>)list3).Sum((Func<DiceBlueprint, double>)((DiceBlueprint st) => effectiveSecondRound(st)));
			foreach (CCSPlayerController item7 in remaining.Keys.ToList())
			{
				double num4 = _random.NextDouble();
				DiceBlueprint diceBlueprint2;
				if (num4 < num2 && num3 > 0.0)
				{
					double num5 = _random.NextDouble() * num3;
					double num6 = 0.0;
					DiceBlueprint selectedType = list3[0];
					foreach (DiceBlueprint item8 in list3)
					{
						num6 += effectiveSecondRound(item8);
						if (num5 < num6)
						{
							selectedType = item8;
							break;
						}
					}
					if (!string.IsNullOrEmpty(selectedType.SecondRoundRewardId))
					{
						DiceBlueprint diceBlueprint = _dices.FirstOrDefault((DiceBlueprint d) => d.ClassName.Equals(selectedType.SecondRoundRewardId, StringComparison.OrdinalIgnoreCase));
						diceBlueprint2 = diceBlueprint ?? selectedType;
					}
					else
					{
						diceBlueprint2 = selectedType;
					}
				}
				else
				{
					diceBlueprint2 = WeightedRandomDraw(list2);
					if (diceBlueprint2 == null)
					{
						diceBlueprint2 = WeightedRandomDraw(drawablePool);
					}
				}
				if (diceBlueprint2 != null)
				{
					remaining[item7] = diceBlueprint2;
				}
			}
		}
		else
		{
			remaining = dictionary;
		}
		Dictionary<CCSPlayerController, DiceBlueprint> dictionary2 = (flag ? specialWinners.Concat(remaining).ToDictionary<KeyValuePair<CCSPlayerController, DiceBlueprint>, CCSPlayerController, DiceBlueprint>((KeyValuePair<CCSPlayerController, DiceBlueprint> kv) => kv.Key, (KeyValuePair<CCSPlayerController, DiceBlueprint> kv) => kv.Value) : remaining);
		foreach (KeyValuePair<CCSPlayerController, DiceBlueprint> item9 in dictionary2)
		{
			CCSPlayerController key = item9.Key;
			string className = item9.Value.ClassName;
			if ((CEntityInstance)(object)key == (CEntityInstance)null || !((CEntityInstance)key).IsValid || GetDiceRollCount(key) >= GetMaxDiceCount(key))
			{
				continue;
			}
			CCSPlayerController capturedEntry = key;
			string capturedDice = className;
			((BasePlugin)this).AddTimer(1f, (Action)delegate
			{
				if (!((CEntityInstance)(object)capturedEntry == (CEntityInstance)null) && ((CEntityInstance)capturedEntry).IsValid && GetDiceRollCount(capturedEntry) < GetMaxDiceCount(capturedEntry))
				{
					var (text, text2) = RollTheDiceForPlayer(capturedEntry, capturedDice);
					if ((text != null && !(text == "")) || 1 == 0)
					{
						IncrementDiceRollCount(capturedEntry);
						PlayDiceSoundForPlayer(capturedEntry, text);
					}
				}
			}, (TimerFlags?)null);
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} RoundStart: {value} alive, {count} rolling, pool={drawablePool.Count}d, special=[{string.Join(",", list3.Select((DiceBlueprint d) => d.ClassName))}], P_total={1.0 - list3.Aggregate(1.0, (double m, DiceBlueprint st) => m * (1.0 - (double)st.SecondRoundProbability)):F3}\n");
		((BasePlugin)this).AddTimer(3f, (Action)delegate
		{
			// 此时本回合的 dice 已发放完毕 → 播放"回合开始"特效。
			DiceEffects.OnRoundStart();
			Dictionary<CCSPlayerController, int> dictionary3 = new Dictionary<CCSPlayerController, int>();
			foreach (CCSPlayerController item10 in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
			{
				int result;
				if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
				{
					CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
					byte? obj;
					if (pawn == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerPawn value4 = pawn.Value;
						obj = ((value4 != null) ? new byte?(((CBaseEntity)value4).LifeState) : ((byte?)null));
					}
					result = ((obj == 0) ? 1 : 0);
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}))
			{
				dictionary3[item10] = GetMaxDiceCount(item10);
			}
			foreach (CCSPlayerController item11 in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
			{
				int result;
				if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
				{
					CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
					byte? obj;
					if (pawn == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerPawn value4 = pawn.Value;
						obj = ((value4 != null) ? new byte?(((CBaseEntity)value4).LifeState) : ((byte?)null));
					}
					result = ((obj == 0) ? 1 : 0);
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}))
			{
				int diceRollCount = GetDiceRollCount(item11);
				// 每次迭代重算上限：若补骰过程中抽到 World（+3），本轮会顺带把额外骰一起发出。
				for (int num8 = diceRollCount; num8 < GetMaxDiceCount(item11); num8++)
				{
					string item = RollTheDiceForPlayer(item11).Item1;
					if (item != null && !(item == ""))
					{
						IncrementDiceRollCount(item11);
					}
				}
				World.PendingExtraRolls.Remove(((CBasePlayerController)item11).SteamID);
				Reincarnation.PendingExtraDice.Remove(((CBasePlayerController)item11).SteamID);
			}
			foreach (KeyValuePair<ulong, string> item12 in Mimic.PendingCopy.ToList())
			{
				ulong steamID = item12.Key;
				string value3 = item12.Value;
				if (!string.IsNullOrEmpty(value3))
				{
					CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && !p.IsBot && ((CBasePlayerController)p).SteamID == steamID);
					if ((CEntityInstance)(object)val == (CEntityInstance)null)
					{
						Mimic.PendingCopy.Remove(steamID);
					}
					else if (GetDiceRollCount(val) >= GetMaxDiceCount(val))
					{
						Mimic.PendingCopy.Remove(steamID);
					}
					else
					{
						string item2 = RollTheDiceForPlayer(val, value3).Item1;
						if (item2 != null && !(item2 == ""))
						{
							IncrementDiceRollCount(val);
						}
						Mimic.PendingCopy.Remove(steamID);
					}
				}
			}
		}, (TimerFlags?)null);
	}

	private void RollTheDiceEveryXSeconds(int seconds)
	{
		((BasePlugin)this).AddTimer((float)seconds, (Action)delegate
		{
			if (_isDuringRound)
			{
				foreach (CCSPlayerController item in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
				{
					int result;
					if (!((CBasePlayerController)p).IsHLTV && !p.IsBot)
					{
						CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)p).Pawn;
						byte? obj;
						if (pawn == null)
						{
							obj = null;
						}
						else
						{
							CBasePlayerPawn value = pawn.Value;
							obj = ((value != null) ? new byte?(((CBaseEntity)value).LifeState) : ((byte?)null));
						}
						result = ((obj == 0) ? 1 : 0);
					}
					else
					{
						result = 0;
					}
					return (byte)result != 0;
				}))
				{
					RemoveDiceForPlayer(item, DiceRemoveReason.GameLogic);
					RollTheDiceForPlayer(item);
				}
				RollTheDiceEveryXSeconds(seconds);
			}
		}, (TimerFlags?)null);
	}

	private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		Server.ExecuteCommand("host_timescale 1.0");
		_isDuringRound = false;
		// 移除 dice 之前播放"回合结束"特效。
		DiceEffects.OnRoundEnd();
		try
		{
			RemoveDicesForPlayers();
		}
		catch (Exception ex)
		{
			Console.WriteLine("[RollTheDice] OnRoundEnd RemoveDicesForPlayers error: " + ex.Message);
		}
		// 本回合的"禁骰"已随本回合滚骰生效，回合结束即失效。
		DeathKnightComplete.ClearDeniedThisRound();
		if (Config.CooldownRounds > 0)
		{
			foreach (KeyValuePair<CCSPlayerController, int> item in _PlayerCooldown)
			{
				if (_PlayerCooldown[item.Key] > 0)
				{
					_PlayerCooldown[item.Key]--;
				}
			}
		}
		return (HookResult)0;
	}

	private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)userid != (CEntityInstance)null && ((CEntityInstance)userid).IsValid && (CEntityInstance)(object)attacker != (CEntityInstance)null && ((CEntityInstance)attacker).IsValid && Mimic.PendingCopy.TryGetValue(((CBasePlayerController)attacker).SteamID, out string value) && string.IsNullOrEmpty(value))
		{
			foreach (DiceBlueprint dix in _dices)
			{
				if (dix._players.Contains(userid) && dix.ClassName != "Mimic")
				{
					Mimic.PendingCopy[((CBasePlayerController)attacker).SteamID] = dix.ClassName;
					break;
				}
			}
		}
		DiceEffects.OnPlayerDeath(userid);
		DiceEffects.OnPlayerKill(attacker, userid, @event.Headshot);
		RemoveDiceForPlayer(userid, DiceRemoveReason.Death);
		return (HookResult)0;
	}

	/// <summary>枪口 / 弹道特效：EventWeaponFire 由 DiceEffects 按玩家节流。</summary>
	private HookResult OnWeaponFireCentral(EventWeaponFire @event, GameEventInfo info)
	{
		DiceEffects.OnWeaponFire(@event.Userid);
		return (HookResult)0;
	}

	private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController disconnected = @event.Userid;
		RemoveDiceForPlayer(disconnected, DiceRemoveReason.Disconnect);
		// 控制器可能已失效（RemoveDiceForPlayer 会 early-return），用 xuid 兜底清理特效状态。
		if (@event.Xuid != 0UL)
		{
			DiceEffects.OnPlayerLeft(@event.Xuid);
		}
		if (disconnected != null)
		{
			// 控制器句柄可能被新加入的玩家复用，必须清掉所有以 controller 为键的状态，否则新玩家会继承
			// "已掷骰 / 冷却中" 的旧记录。
			_originalPlayerNames.Remove(disconnected);
			_playersThatRolledTheDice.Remove(disconnected);
			_PlayerCooldown.Remove(disconnected);
		}
		return (HookResult)0;
	}

	private HookResult OnPlayerHurtReveal(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)userid).TeamNum)
		{
			return (HookResult)0;
		}
		List<string> allDiceForPlayer = GetAllDiceForPlayer(userid);
		if (allDiceForPlayer.Count == 0)
		{
			return (HookResult)0;
		}
		List<string> list = new List<string>();
		foreach (string item in allDiceForPlayer)
		{
			if (item == "Trickster" && Trickster.PendingFakeNames.TryGetValue(((CBasePlayerController)userid).SteamID, out string value))
			{
				list.Add(((BasePlugin)this).Localizer["dice_" + value + "_name"].Value);
				continue;
			}
			string text = "dice_" + item + "_name";
			string text2 = ((BasePlugin)this).Localizer[text];
			list.Add((text2 == text) ? item : text2);
		}
		attacker.PrintToChat($" {((BasePlugin)this).Localizer["command.prefix"].Value}\ud83c\udfaf {(_originalPlayerNames.TryGetValue(userid, out string value2) ? value2 : ((CBasePlayerController)userid).PlayerName)}的骰子：{string.Join(" + ", list)}");
		return (HookResult)0;
	}

	private static CCSPlayerController? ResolvePlayerController(CBaseEntity? entity)
	{
		if ((CEntityInstance)(object)entity == (CEntityInstance)null)
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

	/// <summary>
	/// 统一的伤害加成/减免应用点：attacker 的总伤害加成（跨 dice 求和）与 victim 的总减伤（跨 dice 求和）在此一次性结算。
	/// 各 dice 只负责注册/注销，不再各自乘 info.Damage，避免被重复放大。
	/// </summary>
	public HookResult OnPlayerTakeDamagePreCentral(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (info.Damage <= 0f)
		{
			return HookResult.Continue;
		}
		CCSPlayerController invulnVictim = ResolvePlayerController(entity);
		if (invulnVictim != null && invulnVictim.IsValid && Invulnerability.IsInvulnerable(invulnVictim))
		{
			info.Damage = 0f;
			return HookResult.Changed;
		}
		bool changed = false;
		CCSPlayerController attacker = ResolvePlayerController(info.Attacker?.Value);
		if ((CEntityInstance)(object)attacker != (CEntityInstance)null && ((CEntityInstance)attacker).IsValid && !attacker.IsBot && !((CBasePlayerController)attacker).IsHLTV)
		{
			float bonus = DamageBonusManager.GetTotal(attacker);
			if (bonus != 0f)
			{
				info.Damage *= 1f + bonus;
				changed = true;
			}
		}
		CCSPlayerController victim = ResolvePlayerController(entity);
		if ((CEntityInstance)(object)victim != (CEntityInstance)null && ((CEntityInstance)victim).IsValid)
		{
			float reduction = DamageReductionManager.GetTotal(victim);
			if (reduction > 0f)
			{
				if (reduction > 0.99f)
				{
					reduction = 0.99f;
				}
				info.Damage *= 1f - reduction;
				changed = true;
			}
		}
		DiceEffects.OnPlayerDamaged(victim, attacker, info.GetHitGroup() == HitGroup_t.HITGROUP_HEAD);
		return changed ? HookResult.Changed : HookResult.Continue;
	}

	private void OnMapStart(string mapName)
	{
		ReloadConfigFromDisk();
		LoadMapConfig(mapName);
		InitializeModules();
		_currentMap = mapName;
	}

	private void OnMapEnd()
	{
		DestroyModules();
		Effects.ClearAll();
		DiceEffects.ClearAll();
		_isDuringRound = false;
		_playersThatRolledTheDice.Clear();
		_PlayerCooldown.Clear();
		_originalPlayerNames.Clear();
		// 地图结束做一次完整清理，避免状态带到下一张图。
		Invulnerability.ClearAll();
		MoveLockManager.ClearAll();
		StackingHealth.ClearAll();
		DamageBonusManager.ClearAll();
		DamageReductionManager.ClearAll();
		SpeedBonusManager.ClearAll();
	}

	private (string?, string?) RollTheDiceForPlayer(CCSPlayerController? player, string? diceName = null)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return (null, null);
		}
		if (_dices.Count > 0)
		{
			if (diceName == null)
			{
				DiceBlueprint diceBlueprint = WeightedRandomDraw(GetDrawablePool());
				if (diceBlueprint == null)
				{
					return (null, null);
				}
				try
				{
					LogDebug($"{DateTime.Now:HH:mm:ss} ➜ Adding {diceBlueprint.ClassName} to {((CBasePlayerController)player).PlayerName}\n");
					if (diceBlueprint._players.Contains(player))
					{
						diceBlueprint.Remove(player, DiceRemoveReason.NewDice);
					}
					diceBlueprint.Add(player);
					DiceEffects.OnDiceAdded(player, diceBlueprint.ClassName);
					RefreshPlayerDiceName(player);
					LogDebug($"{DateTime.Now:HH:mm:ss} ✓ {((CBasePlayerController)player).PlayerName} ← {diceBlueprint.ClassName}\n");
					RefreshCombos(player);
					AnnounceDiceRarity(player, diceBlueprint);
					return (diceBlueprint.ClassName, diceBlueprint.Description);
				}
				catch (Exception value)
				{
					LogErr($"{DateTime.Now:HH:mm:ss} Error adding random dice {diceBlueprint.ClassName}: {value}\n");
					return (null, null);
				}
			}
			List<DiceBlueprint> list = _dices.Where((DiceBlueprint d) => d.ClassName.Contains(diceName, StringComparison.OrdinalIgnoreCase)).ToList();
			DiceBlueprint diceBlueprint2 = ((list.Count == 1) ? list.First() : _dices.FirstOrDefault((DiceBlueprint d) => string.Equals(d.ClassName, diceName, StringComparison.OrdinalIgnoreCase)));
			if (diceBlueprint2 != null)
			{
				try
				{
					LogDebug($"{DateTime.Now:HH:mm:ss} ➜ Adding {diceName} to {((CBasePlayerController)player).PlayerName}\n");
					if (diceBlueprint2._players.Contains(player))
					{
						diceBlueprint2.Remove(player, DiceRemoveReason.NewDice);
					}
					diceBlueprint2.Add(player);
					DiceEffects.OnDiceAdded(player, diceBlueprint2.ClassName);
					RefreshPlayerDiceName(player);
					LogDebug($"{DateTime.Now:HH:mm:ss} ✓ {((CBasePlayerController)player).PlayerName} ← {diceName}\n");
					RefreshCombos(player);
					AnnounceDiceRarity(player, diceBlueprint2);
					return (diceBlueprint2.ClassName, diceBlueprint2.Description);
				}
				catch (Exception value2)
				{
					LogErr($"{DateTime.Now:HH:mm:ss} Error adding dice {diceName}: {value2}\n");
					return (null, null);
				}
			}
		}
		return (null, null);
	}

	private DiceBlueprint? WeightedRandomDraw(List<DiceBlueprint> pool)
	{
		if (pool.Count == 0)
		{
			return null;
		}
		RarityConfig? rarity = CurrentRarity;
		if (rarity != null && rarity.TierWeights != null && rarity.TierWeights.Count > 0)
		{
			Dictionary<string, List<DiceBlueprint>> byTier = new Dictionary<string, List<DiceBlueprint>>();
			foreach (DiceBlueprint dice in pool)
			{
				string tier = GetDiceTier(dice);
				if (!byTier.TryGetValue(tier, out List<DiceBlueprint> bucket))
				{
					bucket = new List<DiceBlueprint>();
					byTier[tier] = bucket;
				}
				bucket.Add(dice);
			}
			List<(List<DiceBlueprint> Bucket, float Weight)> tiers = new List<(List<DiceBlueprint>, float)>();
			float totalWeight = 0f;
			foreach (KeyValuePair<string, List<DiceBlueprint>> entry in byTier)
			{
				if (rarity.TierWeights.TryGetValue(entry.Key, out float tierWeight) && tierWeight > 0f)
				{
					tiers.Add((entry.Value, tierWeight));
					totalWeight += tierWeight;
				}
			}
			if (tiers.Count > 0 && totalWeight > 0f)
			{
				float roll = (float)_random.NextDouble() * totalWeight;
				float accumulated = 0f;
				foreach (var tier in tiers)
				{
					accumulated += tier.Weight;
					if (roll < accumulated)
					{
						return tier.Bucket[_random.Next(tier.Bucket.Count)];
					}
				}
				var last = tiers[tiers.Count - 1];
				return last.Bucket[_random.Next(last.Bucket.Count)];
			}
		}
		float num = 0f;
		foreach (DiceBlueprint item in pool)
		{
			num += item.Weight;
		}
		if (num <= 0f)
		{
			return pool[_random.Next(pool.Count)];
		}
		float num2 = (float)_random.NextDouble() * num;
		float num3 = 0f;
		foreach (DiceBlueprint item2 in pool)
		{
			num3 += item2.Weight;
			if (num2 < num3)
			{
				return item2;
			}
		}
		return pool[pool.Count - 1];
	}

	/// <summary>
	/// 当前生效的稀有度配置：地图配置（若命中）优先，否则回退全局。
	/// 注意 <c>LoadMapConfig</c> 在无匹配地图时会把 <c>Dices</c> 直接指向全局 <c>Config.Dices</c>，
	/// 所以"无地图配置"时这里就是全局值。
	/// </summary>
	private RarityConfig? CurrentRarity => _currentMapConfig?.Dices?.Rarity ?? Config?.Dices?.Rarity;

	private string GetDiceTier(DiceBlueprint dice)
	{
		RarityConfig? rarity = CurrentRarity;
		if (rarity != null && rarity.DiceTier != null && rarity.DiceTier.TryGetValue(dice.ClassName, out string tier) && !string.IsNullOrEmpty(tier))
		{
			return tier;
		}
		return "common";
	}

	private void AnnounceDiceRarity(CCSPlayerController player, DiceBlueprint dice)
	{
		try
		{
			string tier = GetDiceTier(dice);
			string nameKey = "dice_" + dice.ClassName + "_name";
			string localized = ((BasePlugin)this).Localizer[nameKey];
			if (localized == nameKey)
			{
				localized = dice.ClassName;
			}
			string label;
			string color;
			switch (tier)
			{
			case "rare":
				label = "稀有";
				color = "\u0004";
				break;
			case "epic":
				label = "史诗";
				color = "\u0003";
				break;
			case "legendary":
				label = "传说";
				color = "\u0009";
				break;
			case "combo":
				label = "传说";
				color = "\u0002";
				break;
			default:
				label = "普通";
				color = "\u0001";
				break;
			}
			string prefix = ((BasePlugin)this).Localizer["command.prefix"].Value;
			player.PrintToChat($" {prefix}{color}🎲 你抽到了 [{label}] {localized}");
			if (Config?.Effects?.Hud ?? true)
			{
				string effects = DiceEffects.DescribeEffects(dice.ClassName);
				string effectLine = string.IsNullOrEmpty(effects) ? "" : $"<br/><span style=\"font-size:16px;color:#9ad\">特效：{effects}</span>";
				player.PrintToCenterHtml($"<div style=\"font-size:22px\"><b>🎲 {localized}</b><br/><span style=\"font-size:16px;color:#fd6\">[{label}]</span>{effectLine}</div>", 4);
			}
			if ((tier == "legendary" || tier == "combo") && (CurrentRarity?.BroadcastLegendary ?? true))
			{
				Server.PrintToChatAll($" {prefix}{color}🌟 传说骰子降临！{((CBasePlayerController)player).PlayerName} 抽到了【{localized}】！");
			}
			if ((tier == "legendary" || tier == "combo") && Config?.Sounds?.LegendarySounds != null && Config.Sounds.LegendarySounds.TryGetValue(dice.ClassName, out string legendarySound) && !string.IsNullOrEmpty(legendarySound))
			{
				BroadcastDiceSound(legendarySound);
			}
		}
		catch
		{
		}
	}

	/// <summary>把音效广播给所有在线真人（从各自身上发声 = 无距离衰减，人人听清）。</summary>
	private void BroadcastDiceSound(string sound)
	{
		try
		{
			if (string.IsNullOrEmpty(sound))
			{
				return;
			}
			float volume = Math.Clamp(Config.Sounds.Volume, 0f, 1f);
			foreach (CCSPlayerController target in Utilities.GetPlayers())
			{
				if (target == null || !((CEntityInstance)target).IsValid || target.IsBot || target.IsHLTV)
				{
					continue;
				}
				RecipientFilter filter = new RecipientFilter();
				filter.Add(target);
				((CBaseEntity)target).EmitSound(sound, filter, volume, 0f);
			}
		}
		catch
		{
		}
	}

	private List<DiceBlueprint> GetDrawablePool()
	{
		return _dices.Where((DiceBlueprint d) => d.CanBeDrawn).ToList();
	}

	private List<DiceBlueprint> GetNormalPool()
	{
		return _dices.Where((DiceBlueprint d) => d.CanBeDrawn && !d.IsSpecial).ToList();
	}

	private void RemoveDiceForPlayer(CCSPlayerController? player, DiceRemoveReason reason)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || reason == DiceRemoveReason.NewDice)
		{
			return;
		}
		bool flag = false;
		foreach (DiceBlueprint dix in _dices)
		{
			if (dix._players.Contains(player))
			{
				try
				{
					dix.Remove(player, reason);
				}
				catch
				{
				}
				DiceEffects.OnDiceRemoved(player, dix.ClassName);
				flag = true;
			}
		}
		if (flag)
		{
			RefreshPlayerDiceName(player);
		}
		if (Config.AllowDiceAfterRespawn)
		{
			_playersThatRolledTheDice.Remove(player);
		}
	}

	private void RefreshCombos(CCSPlayerController trigger)
	{
		try
		{
			List<CCSPlayerController> affected = Utilities.GetPlayers()
				.Where((CCSPlayerController p) => p != null && p.IsValid && !p.IsHLTV && (trigger == null || (CEntityInstance)p == (CEntityInstance)trigger || p.TeamNum == trigger.TeamNum))
				.ToList();
			foreach (DiceBlueprint dice in _dices)
			{
				foreach (CCSPlayerController p in affected)
				{
					try
					{
						dice.OnDiceSetChanged(p);
					}
					catch
					{
					}
				}
			}
			foreach (CCSPlayerController p in affected)
			{
				// combo 伙伴的 dice 变了 → 队里其他人的 TAB 名字前缀也要重算（否则会残留上一轮的搭档 dice 名）。
				RefreshPlayerDiceName(p, false);
			}
		}
		catch
		{
		}
	}

	private void RemoveDicesForPlayers()
	{
		foreach (DiceBlueprint dix in _dices)
		{
			try
			{
				dix.Reset();
			}
			catch
			{
			}
		}
		foreach (KeyValuePair<CCSPlayerController, string> item in _originalPlayerNames.ToList())
		{
			if ((CEntityInstance)(object)item.Key != (CEntityInstance)null && ((CEntityInstance)item.Key).IsValid)
			{
				// ★ 直接用缓存里的"真名"（item.Value），别再解析当前名：
				// `[A]+2 名字` 这种格式解析不出来，会把装饰名原样写回去 → 跨回合残留。
				((CBasePlayerController)item.Key).PlayerName = item.Value;
				Utilities.SetStateChanged((CBaseEntity)(object)item.Key, "CBasePlayerController", "m_iszPlayerName", 0);
			}
		}
		_originalPlayerNames.Clear();
		Invulnerability.ClearAll();
		// 注意：这里不能 Effects.ClearAll()——回合结束的 RoundEnd 特效与本函数里 dice 移除时的
		// Remove 特效都是刚生成的实体，会被立刻清掉导致完全不显示。遗留粒子靠自身生命周期到期，
		// 以及 OnRoundStart / OnMapEnd / Unload 的 Effects.ClearAll() 清理。
		DiceEffects.ClearAll();
		RefreshCombos(null);
	}

	private void RefreshPlayerDiceName(CCSPlayerController player, bool scheduleReapply = true)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		try
		{
			string text = ((CBasePlayerController)player).PlayerName;
			if (!_originalPlayerNames.TryGetValue(player, out string value))
			{
				// ★ 用正则剥掉**插件自己加的** dice 前缀（`[A] [B] 名字` / `[A]+2 名字`）。
				// 旧写法只认 "] "：遇到 `[A]+2 名字` 剥不掉 → 把装饰名当成"真名"缓存，
				// 于是 TAB 上的 dice 前缀跨回合残留、还越滚越长（就是"下回合还显示那么多 dice"）。
				text = StripDiceNameDecoration(text);
				_originalPlayerNames[player] = text;
			}
			else
			{
				text = value;
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (DiceBlueprint dix in _dices)
			{
				if (dix._players.Contains(player))
				{
					string item = ((dix.ClassName == "Trickster" && Trickster.PendingFakeNames.TryGetValue(((CBasePlayerController)player).SteamID, out string value2)) ? value2 : dix.ClassName);
					hashSet.Add(item);
				}
			}
			foreach (var combo in DiceSynergy._combos)
			{
				var (diceA, diceB, _) = combo;
				if (hashSet.Contains(diceA) && !hashSet.Contains(diceB) && _dices.Any((DiceBlueprint d) => d.ClassName == diceB && d._players.Any((CCSPlayerController p) => (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV)))
				{
					hashSet.Add(diceB);
				}
				if (hashSet.Contains(diceB) && !hashSet.Contains(diceA) && _dices.Any((DiceBlueprint d) => d.ClassName == diceA && d._players.Any((CCSPlayerController p) => (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum && ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV)))
				{
					hashSet.Add(diceA);
				}
			}
			List<string> list = DiceSynergy.ResolveComboNames(hashSet).Select(delegate(string cn)
			{
				string text4 = "dice_" + cn + "_name";
				string text5 = ((BasePlugin)this).Localizer[text4];
				return (text5 == text4) ? cn : text5;
			}).ToList();
			string text3 = ((list.Count > 2) ? ($"[{list[0]}]+{list.Count - 1} " + text) : ((list.Count <= 0) ? text : (string.Join(" ", list.Select((string n) => "[" + n + "]")) + " " + text)));
			((CBasePlayerController)player).PlayerName = text3;
			Utilities.SetStateChanged((CBaseEntity)(object)player, "CBasePlayerController", "m_iszPlayerName", 0);
			if (scheduleReapply)
			{
				CCSPlayerController captured = player;
				((BasePlugin)this).AddTimer(1f, (Action)delegate
				{
					if ((CEntityInstance)(object)captured != (CEntityInstance)null && ((CEntityInstance)captured).IsValid)
					{
						// ★ 重算而不是回写 1 秒前那个字符串：跨回合时旧字符串会把上回合的 dice 前缀重新贴回 TAB。
						RefreshPlayerDiceName(captured, false);
					}
				}, (TimerFlags?)null);
			}
		}
		catch (Exception ex)
		{
			LogErr($"{DateTime.Now:HH:mm:ss} RefreshPlayerDiceName error for {((CBasePlayerController)player).PlayerName}: {ex.Message}\n");
		}
	}

	/// <summary>剥掉插件自己加的 dice 前缀（`[A] [B] 名字` / `[A]+2 名字`）；剥完为空则保留原名。</summary>
	private static string StripDiceNameDecoration(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name ?? string.Empty;
		}
		string stripped = Regex.Replace(name, "^(?:\\[[^\\[\\]]{1,48}\\](?:\\+\\d+)?[ \\u3000]+)+", string.Empty).Trim();
		return stripped.Length > 0 ? stripped : name.Trim();
	}

	private void InitializeModules()
	{
		if (_dices.Count > 0 || !_currentMapConfig.Enabled)
		{
			return;
		}
		Dictionary<string, Type> dictionary = (from t in typeof(DiceBlueprint).Assembly.GetTypes()
			where t.IsSubclassOf(typeof(DiceBlueprint)) && !t.IsAbstract
			select t).ToDictionary((Type t) => t.Name, (Type t) => t);
		PropertyInfo[] properties = typeof(DicesConfig).GetProperties();
		bool flag = default(bool);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!dictionary.TryGetValue(propertyInfo.Name, out var value))
			{
				continue;
			}
			object value2 = propertyInfo.GetValue(_currentMapConfig.Dices);
			if (value2 == null)
			{
				continue;
			}
			PropertyInfo property = value2.GetType().GetProperty("Enabled");
			int num2;
			if (property != null)
			{
				object value3 = property.GetValue(value2);
				if (value3 is bool)
				{
					flag = (bool)value3;
					num2 = 1;
				}
				else
				{
					num2 = 0;
				}
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag ? 1u : 0u)) != 0)
			{
				DiceBlueprint item = (DiceBlueprint)Activator.CreateInstance(value, Config, _currentMapConfig, ((BasePlugin)this).Localizer);
				_dices.Add(item);
			}
		}
		ReloadFxTable();
		RegisterListeners();
		RegisterEventHandlers();
		RegisterUserMessageHooks();
	}

	private void DestroyModules()
	{
		try
		{
			DeregisterListeners();
		}
		catch
		{
		}
		try
		{
			DeregisterEventHandlers();
		}
		catch
		{
		}
		try
		{
			DeregisterUserMessageHooks();
		}
		catch
		{
		}
		foreach (DiceBlueprint dix in _dices)
		{
			try
			{
				dix.Destroy();
			}
			catch (Exception ex)
			{
				Console.WriteLine("[RollTheDice] DestroyModules: error destroying " + dix.ClassName + ": " + ex.Message);
			}
		}
		_dices.Clear();
	}

	private void RegisterListeners()
	{
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (string listener in dix.Listeners)
			{
				DynamicHandlers.RegisterModuleListener((BasePlugin)(object)this, listener, dix);
			}
		}
	}

	private void DeregisterListeners()
	{
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (string listener in dix.Listeners)
			{
				DynamicHandlers.DeregisterModuleListener((BasePlugin)(object)this, listener, dix);
			}
		}
	}

	private void RegisterEventHandlers()
	{
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (string @event in dix.Events)
			{
				DynamicHandlers.RegisterModuleEventHandler((BasePlugin)(object)this, @event, dix);
			}
		}
	}

	private void DeregisterEventHandlers()
	{
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (string @event in dix.Events)
			{
				DynamicHandlers.DeregisterModuleEventHandler((BasePlugin)(object)this, @event, dix);
			}
		}
	}

	private void RegisterUserMessageHooks()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (var (messageId, hookMode) in dix.UserMessages)
			{
				DynamicHandlers.RegisterUserMessageHook((BasePlugin)(object)this, messageId, dix, hookMode);
			}
		}
	}

	private void DeregisterUserMessageHooks()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		foreach (DiceBlueprint dix in _dices)
		{
			foreach (var (messageId, hookMode) in dix.UserMessages)
			{
				DynamicHandlers.DeregisterUserMessageHook((BasePlugin)(object)this, messageId, dix, hookMode);
			}
		}
	}

	private void PlayDiceSoundForPlayer(CCSPlayerController? player, string rolledDice, bool command = false)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && (command || !Config.Sounds.PlayOnCommandOnly) && !string.IsNullOrEmpty(Config.Sounds.DiceRollSound))
		{
			string text = Config.Sounds.DiceRollSound.Replace("{dice}", rolledDice);
			RecipientFilter val = new RecipientFilter();
			val.Add(player);
			RecipientFilter val2 = val;
			((CBaseEntity)player).EmitSound(text, val2, Math.Clamp(Config.Sounds.Volume, 0f, 1f), 0f);
		}
	}

	private int GetDiceRollCount(CCSPlayerController player)
	{
		int value;
		return _playersThatRolledTheDice.TryGetValue(player, out value) ? value : 0;
	}

	private void IncrementDiceRollCount(CCSPlayerController player)
	{
		if (_playersThatRolledTheDice.ContainsKey(player))
		{
			_playersThatRolledTheDice[player]++;
		}
		else
		{
			_playersThatRolledTheDice[player] = 1;
		}
	}

	public bool HasDiceActive(CCSPlayerController player, string diceClassName)
	{
		ulong steamId = ((CBasePlayerController)player).SteamID;
		return _dices.Any((DiceBlueprint d) => string.Equals(d.ClassName, diceClassName, StringComparison.OrdinalIgnoreCase) && d._players.Any((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == steamId));
	}

	public bool ForceDiceForPlayer(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return false;
		}
		if (GetDiceRollCount(player) >= GetMaxDiceCount(player))
		{
			return false;
		}
		string item = RollTheDiceForPlayer(player).Item1;
		if ((item == null || item == "") ? true : false)
		{
			return false;
		}
		IncrementDiceRollCount(player);
		PlayDiceSoundForPlayer(player, item);
		return true;
	}

	public bool ForceExtraDiceForPlayer(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return false;
		}
		string item = RollTheDiceForPlayer(player).Item1;
		if ((item == null || item == "") ? true : false)
		{
			return false;
		}
		IncrementDiceRollCount(player);
		PlayDiceSoundForPlayer(player, item);
		return true;
	}

	public bool ForceDiceForPlayer(CCSPlayerController player, string diceClassName)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return false;
		}
		if (GetDiceRollCount(player) >= GetMaxDiceCount(player))
		{
			return false;
		}
		string item = RollTheDiceForPlayer(player, diceClassName).Item1;
		if ((item == null || item == "") ? true : false)
		{
			return false;
		}
		IncrementDiceRollCount(player);
		PlayDiceSoundForPlayer(player, item);
		return true;
	}

	public bool GrantComboDice(CCSPlayerController player, string diceClassName)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return false;
		}
		var (text, _) = RollTheDiceForPlayer(player, diceClassName);
		LogDebug($"{DateTime.Now:HH:mm:ss} GrantComboDice {diceClassName} -> {text ?? "null"}\n");
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		IncrementDiceRollCount(player);
		PlayDiceSoundForPlayer(player, text);
		return true;
	}

	public bool RemoveDiceFromPlayer(CCSPlayerController player, string diceClassName)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return false;
		}
		DiceBlueprint diceBlueprint = _dices.FirstOrDefault((DiceBlueprint d) => d.ClassName == diceClassName);
		if (diceBlueprint == null)
		{
			return false;
		}
		if (!diceBlueprint._players.Contains(player))
		{
			return false;
		}
		diceBlueprint.Remove(player);
		DiceEffects.OnDiceRemoved(player, diceClassName);
		_playersThatRolledTheDice.Remove(player);
		RefreshPlayerDiceName(player);
		RefreshCombos(player);
		return true;
	}

	public List<string> GetAllDiceForPlayer(CCSPlayerController player)
	{
		List<string> list = new List<string>();
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return list;
		}
		foreach (DiceBlueprint dix in _dices)
		{
			if (dix._players.Contains(player))
			{
				list.Add(dix.ClassName);
			}
		}
		return list;
	}

	private int GetMaxDiceCount(CCSPlayerController player)
	{
		if (DeathKnightComplete.IsDeniedThisRound(((CBasePlayerController)player).SteamID))
		{
			return 0;
		}
		int num = 1;
		if (Reincarnation.PendingExtraDice.TryGetValue(((CBasePlayerController)player).SteamID, out var value2) && value2 > 0)
		{
			num += value2;
		}
		if (World.PendingExtraRolls.TryGetValue(((CBasePlayerController)player).SteamID, out var value3) && value3 > 0)
		{
			num += value3;
		}
		if (Goddess.ActiveThisRound && Goddess.BlessedPlayers.Contains(((CBasePlayerController)player).SteamID))
		{
			num++;
		}
		return num;
	}

	private string GetLastDiceDescription(CCSPlayerController player)
	{
		foreach (DiceBlueprint dix in _dices)
		{
			if (dix._players.Contains(player))
			{
				return dix.Description;
			}
		}
		return "Unknown";
	}

	private void CreateRoundBackup()
	{
		try
		{
			int num = (GameRules.Get("TotalRoundsPlayed", forceRefresh: true) as int?) ?? 1;
			string text = $"{"rtd"}_round{num:D2}.txt";
			string path = Path.Combine(Server.GameDirectory, $"{"rtd"}_round{num - 1:D2}.txt");
			try
			{
				File.Delete(path);
			}
			catch
			{
			}
			_currentRoundBackupFile = text;
			Server.ExecuteCommand("mp_backup_round_file rtd");
			Console.WriteLine("[RollTheDice] Round backup: " + text);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[RollTheDice] Failed to create round backup: " + ex.Message);
		}
	}

	private void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if (!((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || ((CBasePlayerController)player).IsHLTV || player.IsBot)
		{
			return;
		}
		LogDebug($"{DateTime.Now:HH:mm:ss} [E] {((CBasePlayerController)player).PlayerName} pressed E\n");
		List<string> list = new List<string>();
		foreach (DiceBlueprint dix in _dices)
		{
			if (dix._players.Contains(player))
			{
				float cooldownRemaining = dix.GetCooldownRemaining(player);
				if (cooldownRemaining > 0f)
				{
					string value = ((BasePlugin)this).Localizer["dice_" + dix.ClassName + "_name"].Value;
					list.Add($"[{value}] {cooldownRemaining:F1}s");
				}
			}
		}
		if (list.Count > 0)
		{
			player.PrintToCenterAlert("⏳ " + string.Join("  ", list));
		}
	}

	public RollTheDice()
	{
		int num = 19;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "models/chicken/chicken.vmdl";
		num2++;
		span[num2] = "particles/burning_fx/env_fire_tiny.vpcf";
		num2++;
		span[num2] = "models/props/cs_office/plant01.vmdl";
		num2++;
		span[num2] = "models/props/de_vertigo/trafficcone_clean.vmdl";
		num2++;
		span[num2] = "models/generic/barstool_01/barstool_01.vmdl";
		num2++;
		span[num2] = "models/generic/fire_extinguisher_01/fire_extinguisher_01.vmdl";
		num2++;
		span[num2] = "models/hostage/hostage.vmdl";
		num2++;
		span[num2] = "models/ar_shoots/shoots_pottery_02.vmdl";
		num2++;
		span[num2] = "models/anubis/signs/anubis_info_panel_01.vmdl";
		num2++;
		span[num2] = "models/cs_italy/seating/chair/wood_chair_1.vmdl";
		num2++;
		span[num2] = "models/props_office/file_cabinet_03.vmdl";
		num2++;
		span[num2] = "models/props_plants/plantairport01.vmdl";
		num2++;
		span[num2] = "models/props_street/mail_dropbox.vmdl";
		num2++;
		span[num2] = "models/props_c17/furnituretable001a_static.vmdl";
		num2++;
		span[num2] = "models/props_fairgrounds/fairgrounds_flagpole01.vmdl";
		num2++;
		span[num2] = "models/props_foliage/mall_small_palm01.vmdl";
		num2++;
		span[num2] = "models/props_foliage/urban_pot_fancy01.vmdl";
		num2++;
		span[num2] = "models/props_interiors/copymachine01.vmdl";
		num2++;
		span[num2] = "models/props_interiors/trashcan01.vmdl";
		_precacheModels = list;
		_currentMap = "";
		_playersThatRolledTheDice = new Dictionary<CCSPlayerController, int>();
		_PlayerCooldown = new Dictionary<CCSPlayerController, int>();
		_dices = new List<DiceBlueprint>();
		_originalPlayerNames = new Dictionary<CCSPlayerController, string>();
		_random = new Random(Guid.NewGuid().GetHashCode());
	}

	private static string RtdLogPath => Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/logs/rtd_debug.txt");

	private static bool RtdDebugEnabled => Instance != null && Instance.Config != null && Instance.Config.Debug;

	internal static void LogDebug(string message)
	{
		try
		{
			if (RtdDebugEnabled)
			{
				File.AppendAllText(RtdLogPath, message);
			}
		}
		catch
		{
		}
	}

	internal static void LogErr(string message)
	{
		try
		{
			File.AppendAllText(RtdLogPath, message);
		}
		catch
		{
		}
	}
}
