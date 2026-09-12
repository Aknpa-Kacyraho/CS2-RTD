using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class PainConverter : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _pain = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _lastNotify = new Dictionary<ulong, float>();

	private readonly Dictionary<CCSPlayerController, float> _burstEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _regenAcc = new Dictionary<CCSPlayerController, float>();

	private float _lastTick = -1f;

	public override string ClassName => "PainConverter";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public PainConverter(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		return _cooldowns.TryGetValue(player, out float value) ? Math.Max(0f, value - Server.CurrentTime) : 0f;
	}

	public override void Add(CCSPlayerController player)
	{
		base.Add(player);
		if (!_players.Contains(player))
		{
			return;
		}
		_pain[player.SteamID] = 0f;
		_lastNotify[player.SteamID] = 0f;
		_cooldowns[player] = 0f;
		if (DiceSynergy.HasPartner(player, "Adrenaline"))
		{
			DiceSynergy.AnnounceCombo(player, "痛苦源泉", "残血蓄痛翻倍，爆发附带 20% 减伤！");
		}
		if (DiceSynergy.HasPartner(player, "DeathKnight"))
		{
			DiceSynergy.AnnounceCombo(player, "伤痛铠甲", "每 25 点痛觉转化为 5% 减伤！");
		}
		if (DiceSynergy.HasPartner(player, "Miser"))
		{
			DiceSynergy.AnnounceCombo(player, "痛苦经济", "挨打赚钱：蓄痛 +$100，爆发 +$200！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		EndBurst(player);
		DamageReductionManager.Unregister(player, "PainConverterDK");
		_cooldowns.Remove(player);
		_regenAcc.Remove(player);
		_players.Remove(player);
		_pain.Remove(player.SteamID);
		_lastNotify.Remove(player.SteamID);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			EndBurst(player);
			DamageReductionManager.Unregister(player, "PainConverterDK");
		}
		_players.Clear();
		_pain.Clear();
		_lastNotify.Clear();
		_burstEnd.Clear();
		_cooldowns.Clear();
		_regenAcc.Clear();
		_lastTick = -1f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		if (_players.Count == 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerController victim = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (victim == null || attacker == null || !victim.IsValid || !attacker.IsValid)
		{
			return HookResult.Continue;
		}
		if (victim == attacker || !_players.Contains(victim))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)victim).TeamNum == ((CBaseEntity)attacker).TeamNum)
		{
			return HookResult.Continue;
		}
		float damage = @event.DmgHealth;
		if (damage <= 0f)
		{
			return HookResult.Continue;
		}
		ulong id = victim.SteamID;
		float gain = damage;
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (DiceSynergy.HasPartner(victim, "Adrenaline") && victimPawn != null && victimPawn.IsValid && victimPawn.Health <= victimPawn.MaxHealth * 0.4f)
		{
			gain *= 2f;
		}
		float current = _pain.TryGetValue(id, out float pain) ? pain : 0f;
		_pain[id] = Math.Min(current + gain, _config.Dices.PainConverter.MaxPain);
		if (DiceSynergy.HasPartner(victim, "Miser") && victim.InGameMoneyServices != null)
		{
			int reward = (_burstEnd.ContainsKey(victim) ? 200 : 100);
			victim.InGameMoneyServices.Account += reward;
			Utilities.SetStateChanged(victim, "CCSPlayerController", "m_pInGameMoneyServices", 0);
		}
		return HookResult.Continue;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (_players.Count == 0 || player == null || !player.IsValid || !_players.Contains(player))
		{
			return;
		}
		if ((pressed & PlayerButtons.Use) == 0)
		{
			return;
		}
		if (GetCooldownRemaining(player) > 0f)
		{
			return;
		}
		ulong id = player.SteamID;
		float pain = _pain.TryGetValue(id, out float stored) ? stored : 0f;
		if (pain < _config.Dices.PainConverter.MinPainToActivate)
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || pawn.LifeState != 0)
		{
			return;
		}
		float amount = Math.Min(pain, _config.Dices.PainConverter.DamagePainCap);
		_pain[id] = 0f;
		float now = Server.CurrentTime;
		_cooldowns[player] = now + _config.Dices.PainConverter.Cooldown;
		_burstEnd[player] = now + _config.Dices.PainConverter.BurstDuration;
		_regenAcc[player] = 0f;
		float damageBonus = amount * _config.Dices.PainConverter.DamagePerPain;
		float speedBonus = amount * _config.Dices.PainConverter.SpeedPerPain;
		DamageBonusManager.Register(player, "PainConverter", damageBonus);
		SpeedBonusManager.Register(player, "PainConverter", speedBonus);
		if (DiceSynergy.HasPartner(player, "Adrenaline"))
		{
			DamageReductionManager.Register(player, "PainConverterAdrenaline", 0.2f);
		}
		SyncSpeed(player);
		NotifyBurst(player, damageBonus * 100f, speedBonus * 100f);
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		float dt = _lastTick < 0f ? 0f : Math.Min(now - _lastTick, 1f);
		_lastTick = now;

		DecayPain(dt);
		UpdateBursts(now, dt);
		SyncDeathKnightReduction();
		NotifyPain(now);
	}

	private void SyncDeathKnightReduction()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (player == null || !player.IsValid)
			{
				continue;
			}
			if (!DiceSynergy.HasPartner(player, "DeathKnight"))
			{
				DamageReductionManager.Unregister(player, "PainConverterDK");
				continue;
			}
			float pain = _pain.TryGetValue(player.SteamID, out float stored) ? stored : 0f;
			float reduction = Math.Min(pain / 25f * 0.05f, 0.5f);
			if (reduction > 0.001f)
			{
				DamageReductionManager.Register(player, "PainConverterDK", reduction);
			}
			else
			{
				DamageReductionManager.Unregister(player, "PainConverterDK");
			}
		}
	}

	private void DecayPain(float dt)
	{
		if (dt <= 0f || _config.Dices.PainConverter.DecayPerSecond <= 0f)
		{
			return;
		}
		foreach (CCSPlayerController player in _players.ToList())
		{
			ulong id = player.SteamID;
			if (!_pain.TryGetValue(id, out float pain) || pain <= 0f)
			{
				continue;
			}
			float next = Math.Max(0f, pain - _config.Dices.PainConverter.DecayPerSecond * dt);
			if (next <= 0f)
			{
				_pain.Remove(id);
			}
			else
			{
				_pain[id] = next;
			}
		}
	}

	private void UpdateBursts(float now, float dt)
	{
		foreach (CCSPlayerController player in _burstEnd.Keys.ToList())
		{
			if (!player.IsValid || now >= _burstEnd[player])
			{
				EndBurst(player);
				continue;
			}
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || pawn.LifeState != 0)
			{
				continue;
			}
			if (_config.Dices.PainConverter.BurstHpPerSecond > 0f && dt > 0f)
			{
				float acc = (_regenAcc.TryGetValue(player, out float value) ? value : 0f) + _config.Dices.PainConverter.BurstHpPerSecond * dt;
				int whole = (int)acc;
				if (whole > 0)
				{
					acc -= whole;
					pawn.Health = Math.Min(pawn.Health + whole, pawn.MaxHealth);
					Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
				}
				_regenAcc[player] = acc;
			}
			SyncSpeed(player);
		}
	}

	private void EndBurst(CCSPlayerController player)
	{
		if (player == null)
		{
			return;
		}
		if (_burstEnd.Remove(player))
		{
			DamageBonusManager.Unregister(player, "PainConverter");
			SpeedBonusManager.Unregister(player, "PainConverter");
			DamageReductionManager.Unregister(player, "PainConverterAdrenaline");
			SyncSpeed(player);
		}
		_regenAcc.Remove(player);
	}

	private void SyncSpeed(CCSPlayerController player)
	{
		if (player == null || !player.IsValid)
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || pawn.LifeState != 0)
		{
			return;
		}
		float expected = 1f + SpeedBonusManager.GetEffective(player, 100f);
		if (Math.Abs(pawn.VelocityModifier - expected) > 0.001f)
		{
			pawn.VelocityModifier = expected;
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private void NotifyPain(float now)
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (_burstEnd.ContainsKey(player))
			{
				continue;
			}
			ulong id = player.SteamID;
			float pain = _pain.TryGetValue(id, out float stored) ? stored : 0f;
			if (pain <= 0f)
			{
				continue;
			}
			if (_lastNotify.TryGetValue(id, out float last) && now - last < 2f)
			{
				continue;
			}
			_lastNotify[id] = now;
			int percent = (int)Math.Round(pain / _config.Dices.PainConverter.MaxPain * 100f);
			NotifyStatus(player, ClassName, new Dictionary<string, string>
			{
				{ "pain", percent.ToString() }
			});
		}
	}

	private void NotifyBurst(CCSPlayerController player, float damagePercent, float speedPercent)
	{
		if (_localizer["dice_PainConverter_burst"].ResourceNotFound)
		{
			return;
		}
		string text = _localizer["dice_PainConverter_burst"].Value
			.Replace("{damage}", Math.Round(damagePercent, 0).ToString())
			.Replace("{speed}", Math.Round(speedPercent, 0).ToString());
		if (_globalConfig.NotifyPlayerViaCenterMsg)
		{
			player.PrintToCenter(text);
		}
		if (_globalConfig.NotifyPlayerViaChatMsg)
		{
			player.PrintToChat(_localizer["command.prefix"].Value + text);
		}
	}
}
