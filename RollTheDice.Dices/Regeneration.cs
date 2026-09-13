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
/// 生命之泉 Regeneration：脱离战斗一段时间后每秒回血；受到伤害或开火会打断。
/// 与 JumpHeal 组合（生命律动）：脱战判定时间减半。
/// </summary>
public class Regeneration : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _lastCombat = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _accum = new Dictionary<CCSPlayerController, float>();

	private float _lastTick;

	public override string ClassName => "Regeneration";

	public override List<string> Events => new List<string> { "EventPlayerHurt", "EventWeaponFire" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Regeneration(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_lastCombat[player] = Server.CurrentTime;
		_accum[player] = 0f;
		if (DiceSynergy.HasPartner(player, "JumpHeal"))
		{
			DiceSynergy.AnnounceCombo(player, "生命律动", "脱战判定时间减半，回血更频繁！");
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
		_lastCombat.Remove(player);
		_accum.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_lastCombat.Clear();
		_accum.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		MarkCombat(@event.Userid);
		return HookResult.Continue;
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		MarkCombat(@event.Userid);
		return HookResult.Continue;
	}

	private void MarkCombat(CCSPlayerController player)
	{
		if (player != null && player.IsValid && _players.Contains(player))
		{
			_lastCombat[player] = Server.CurrentTime;
			_accum[player] = 0f;
		}
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
		RegenerationConfig cfg = _config.Dices.Regeneration;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
				if (player == null || !player.IsValid || pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
				{
					continue;
				}
				if (!_lastCombat.TryGetValue(player, out float lastCombat))
				{
					lastCombat = now;
					_lastCombat[player] = now;
				}
				float outOfCombat = cfg.OutOfCombatSeconds;
				if (DiceSynergy.HasPartner(player, "JumpHeal"))
				{
					outOfCombat *= 0.5f;
				}
				if (now - lastCombat < outOfCombat)
				{
					continue;
				}
				float accum = (_accum.TryGetValue(player, out float a) ? a : 0f) + cfg.HealPerSecond * dt;
				int whole = (int)accum;
				if (whole <= 0)
				{
					_accum[player] = accum;
					continue;
				}
				CBaseEntity entity = pawn;
				if (entity.Health < entity.MaxHealth)
				{
					entity.Health = Math.Min(entity.Health + whole, entity.MaxHealth);
					Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
					accum -= whole;
				}
				_accum[player] = accum;
			}
			catch
			{
				_lastCombat.Remove(player);
				_accum.Remove(player);
			}
		}
	}
}
