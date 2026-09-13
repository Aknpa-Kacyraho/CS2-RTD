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
/// 毁灭之力 DamageMultiplier：站定不动时伤害倍率逐秒上涨到上限；移动/离地立刻回落到基础值。
/// 与 Berserker 组合（狂暴之力）：半血以下蓄力翻倍。
/// </summary>
public class DamageMultiplier : DiceBlueprint
{
	private const float MoveSpeedThreshold = 30f;

	private readonly Dictionary<CCSPlayerController, float> _multiplier = new Dictionary<CCSPlayerController, float>();

	private float _lastTick;

	public override string ClassName => "DamageMultiplier";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public DamageMultiplier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_multiplier[player] = _config.Dices.DamageMultiplier.BaseMultiplier;
		DamageBonusManager.Register(player, ClassName, _config.Dices.DamageMultiplier.BaseMultiplier - 1f);
		if (DiceSynergy.HasPartner(player, "Berserker"))
		{
			DiceSynergy.AnnounceCombo(player, "狂暴之力", "半血以下毁灭之力蓄力翻倍！");
		}
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
		DamageBonusManager.Unregister(player, ClassName);
		_multiplier.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			DamageBonusManager.Unregister(player, ClassName);
		}
		_players.Clear();
		_multiplier.Clear();
	}

	public override void Destroy()
	{
		Reset();
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
		DamageMultiplierConfig cfg = _config.Dices.DamageMultiplier;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
				if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
				{
					continue;
				}
				if (!_multiplier.TryGetValue(player, out float current))
				{
					current = cfg.BaseMultiplier;
				}
				CBaseEntity entity = pawn;
				Vector velocity = entity.AbsVelocity;
				float horizontal = (velocity == null) ? 0f : MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
				bool onGround = (entity.Flags & 1u) != 0;
				float next;
				if (onGround && horizontal <= MoveSpeedThreshold)
				{
					float gain = cfg.GainPerSecond * dt;
					if (DiceSynergy.HasPartner(player, "Berserker") && entity.Health <= entity.MaxHealth / 2)
					{
						gain *= 2f;
					}
					next = Math.Min(current + gain, cfg.MaxMultiplier);
				}
				else
				{
					next = cfg.BaseMultiplier;
				}
				if (Math.Abs(next - current) > 0.005f)
				{
					_multiplier[player] = next;
					DamageBonusManager.Register(player, ClassName, next - 1f);
				}
			}
			catch
			{
				_multiplier.Remove(player);
			}
		}
	}
}
