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
/// 疾风步 IncreaseSpeed：持续移动时移速递增到上限；受到伤害立即清零。
/// </summary>
public class IncreaseSpeed : DiceBlueprint
{
	private const float MoveSpeedThreshold = 30f;

	private readonly Dictionary<CCSPlayerController, float> _bonus = new Dictionary<CCSPlayerController, float>();

	private float _lastTick;

	public override string ClassName => "IncreaseSpeed";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public IncreaseSpeed(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_bonus[player] = 0f;
		SpeedBonusManager.Register(player, ClassName, 0f);
		WriteSpeed(player);
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		SpeedBonusManager.Unregister(player, ClassName);
		_bonus.Remove(player);
		_players.Remove(player);
		WriteSpeed(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			SpeedBonusManager.Unregister(player, ClassName);
			WriteSpeed(player);
		}
		_players.Clear();
		_bonus.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		CCSPlayerController player = @event.Userid;
		if (player == null || !player.IsValid || !_players.Contains(player))
		{
			return HookResult.Continue;
		}
		if (_bonus.TryGetValue(player, out float current) && current != 0f)
		{
			_bonus[player] = 0f;
			SpeedBonusManager.Register(player, ClassName, 0f);
			WriteSpeed(player);
		}
		return HookResult.Continue;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		float dt = (_lastTick <= 0f) ? 0f : Math.Min(now - _lastTick, 0.25f);
		_lastTick = now;
		if (dt <= 0f)
		{
			return;
		}
		IncreaseSpeedConfig cfg = _config.Dices.IncreaseSpeed;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
				if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
				{
					continue;
				}
				if (!_bonus.TryGetValue(player, out float current))
				{
					current = 0f;
				}
				CBaseEntity entity = pawn;
				Vector velocity = entity.AbsVelocity;
				float horizontal = (velocity == null) ? 0f : MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
				if (horizontal <= MoveSpeedThreshold)
				{
					continue;
				}
				float next = Math.Min(current + cfg.GainPerSecond * dt, cfg.MaxBonus);
				if (Math.Abs(next - current) > 0.005f)
				{
					_bonus[player] = next;
					SpeedBonusManager.Register(player, ClassName, next);
					WriteSpeed(player);
				}
			}
			catch
			{
				_bonus.Remove(player);
			}
		}
	}

	private static void WriteSpeed(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		pawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
		Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
	}
}
