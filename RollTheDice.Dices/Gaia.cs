using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 大地母亲 Gaia：站在地面时最大生命持续增长（可突破原上限，最多 +max_bonus），移动或离地停止。
/// 与 HangedMan 组合（生死天平）：上限提高到 500。
/// </summary>
public class Gaia : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMax = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _accum = new Dictionary<CCSPlayerController, float>();

	private float _lastTick;

	public override string ClassName => "Gaia";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Gaia(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_originalMax[player] = ((CBaseEntity)player.PlayerPawn.Value).MaxHealth;
		_accum[player] = 0f;
		if (DiceSynergy.HasPartner(player, "HangedMan"))
		{
			DiceSynergy.AnnounceCombo(player, "生死天平", "生命上限提高到 500！");
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
		if (_originalMax.TryGetValue(player, out int original) && player != null && player.IsValid)
		{
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if (pawn != null && pawn.IsValid)
			{
				CBaseEntity entity = pawn;
				entity.MaxHealth = original;
				if (entity.Health > original)
				{
					entity.Health = original;
				}
				Utilities.SetStateChanged(entity, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
			}
		}
		_originalMax.Remove(player);
		_accum.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			Remove(player);
		}
		_originalMax.Clear();
		_accum.Clear();
		_players.Clear();
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
		GaiaConfig cfg = _config.Dices.Gaia;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			CBaseEntity entity = pawn;
			if ((entity.Flags & 1u) == 0)
			{
				continue;
			}
			if (!_originalMax.TryGetValue(player, out int original))
			{
				original = entity.MaxHealth;
				_originalMax[player] = original;
			}
			int cap = DiceSynergy.HasPartner(player, "HangedMan") ? 500 : original + cfg.MaxBonus;
			float accum = (_accum.TryGetValue(player, out float a) ? a : 0f) + cfg.GainPerSecond * dt;
			int whole = (int)accum;
			if (whole <= 0 || entity.MaxHealth >= cap)
			{
				_accum[player] = accum;
				continue;
			}
			int newMax = Math.Min(entity.MaxHealth + whole, cap);
			entity.MaxHealth = newMax;
			entity.Health = Math.Min(entity.Health + whole, newMax);
			Utilities.SetStateChanged(entity, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
			_accum[player] = accum - whole;
		}
	}
}
