using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Combo : DiceBlueprint
{
	private sealed class GlowEntry
	{
		public CCSPlayerController Victim;
		public CDynamicProp Proxy;
		public CDynamicProp Glow;
		public float ExpireAt;
	}

	private readonly Dictionary<ulong, int> _stacks = new Dictionary<ulong, int>();

	private readonly Dictionary<ulong, float> _expireAt = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _lastHit = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, float> _lastNotify = new Dictionary<ulong, float>();

	private readonly List<GlowEntry> _glows = new List<GlowEntry>();

	public override string ClassName => "Combo";

	public override List<string> Events => new List<string> { "EventPlayerHurt" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Combo(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Add(CCSPlayerController player)
	{
		base.Add(player);
		if (!_players.Contains(player))
		{
			return;
		}
		ulong id = player.SteamID;
		_stacks[id] = 0;
		_expireAt[id] = 0f;
		_lastHit[id] = 0f;
		_lastNotify[id] = 0f;
		if (DiceSynergy.HasPartner(player, "Vampire"))
		{
			DiceSynergy.AnnounceCombo(player, "血怒连击", "每层命中回复 1HP！");
		}
		if (DiceSynergy.HasPartner(player, "Overheat"))
		{
			DiceSynergy.AnnounceCombo(player, "过热连击", "断链窗口延长，满层再 +10% 伤害！");
		}
		if (DiceSynergy.HasPartner(player, "SpeedOnKill"))
		{
			DiceSynergy.AnnounceCombo(player, "杀戮节奏", "连击到 5 层触发速度爆发！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		ClearBuffs(player);
		RemoveGlowsFor(player);
		_players.Remove(player);
		ulong id = player.SteamID;
		_stacks.Remove(id);
		_expireAt.Remove(id);
		_lastHit.Remove(id);
		_lastNotify.Remove(id);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			ClearBuffs(player);
			RemoveGlowsFor(player);
		}
		_players.Clear();
		ClearAllGlows();
		_stacks.Clear();
		_expireAt.Clear();
		_lastHit.Clear();
		_lastNotify.Clear();
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
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController victim = @event.Userid;
		if (attacker == null || victim == null || !attacker.IsValid || !victim.IsValid)
		{
			return HookResult.Continue;
		}
		if (attacker == victim || !_players.Contains(attacker))
		{
			return HookResult.Continue;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)victim).TeamNum)
		{
			return HookResult.Continue;
		}
		CCSPlayerPawn victimPawn = victim.PlayerPawn?.Value;
		if (victimPawn == null || !victimPawn.IsValid || victimPawn.LifeState != 0)
		{
			return HookResult.Continue;
		}
		float now = Server.CurrentTime;
		ulong id = attacker.SteamID;
		if (_lastHit.TryGetValue(id, out float last) && now - last < _config.Dices.Combo.MinHitInterval)
		{
			return HookResult.Continue;
		}
		_lastHit[id] = now;
		int max = _config.Dices.Combo.MaxStacks;
		int stacks = Math.Min((_stacks.TryGetValue(id, out int current) ? current : 0) + 1, max);
		_stacks[id] = stacks;
		float window = (DiceSynergy.HasPartner(attacker, "Overheat") ? _config.Dices.Combo.WindowSeconds * 1.5f : _config.Dices.Combo.WindowSeconds);
		_expireAt[id] = now + window;

		DamageBonusManager.Register(attacker, "Combo", stacks * _config.Dices.Combo.DamagePerStack, max * _config.Dices.Combo.DamagePerStack);
		if (stacks > 0 && DiceSynergy.HasPartner(attacker, "Overheat"))
		{
			DamageBonusManager.Register(attacker, "ComboOverheat", 0.1f);
		}
		else
		{
			DamageBonusManager.Unregister(attacker, "ComboOverheat");
		}
		if (stacks >= max)
		{
			SpeedBonusManager.Register(attacker, "Combo", _config.Dices.Combo.FullStackSpeed);
		}
		else
		{
			SpeedBonusManager.Unregister(attacker, "Combo");
		}
		SyncSpeed(attacker);

		int heal = 0;
		if (DiceSynergy.HasPartner(attacker, "Vampire"))
		{
			heal = stacks;
		}
		else if (stacks >= _config.Dices.Combo.HealThreshold)
		{
			heal = _config.Dices.Combo.HealPerHit;
		}
		if (heal > 0)
		{
			CCSPlayerPawn attackerPawn = attacker.PlayerPawn?.Value;
			if (attackerPawn != null && attackerPawn.IsValid && attackerPawn.LifeState == 0)
			{
				attackerPawn.Health = Math.Min(attackerPawn.Health + heal, attackerPawn.MaxHealth);
				Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
		if (stacks == 5 && DiceSynergy.HasPartner(attacker, "SpeedOnKill"))
		{
			SpeedOnKill.Instance?.TriggerBoost(attacker);
		}

		ApplyGlow(victim, now);
		NotifyCombo(attacker, stacks, now);
		return HookResult.Continue;
	}

	public void OnTick()
	{
		if (_players.Count == 0 && _glows.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (CCSPlayerController player in _players.ToList())
		{
			ulong id = player.SteamID;
			if (_expireAt.TryGetValue(id, out float expire) && expire > 0f && now >= expire)
			{
				BreakCombo(player);
			}
			else
			{
				SyncSpeed(player);
			}
		}
		RemoveExpiredGlows(now);
	}

	private void BreakCombo(CCSPlayerController player)
	{
		ulong id = player.SteamID;
		int previous = _stacks.TryGetValue(id, out int stacks) ? stacks : 0;
		_stacks[id] = 0;
		_expireAt[id] = 0f;
		DamageBonusManager.Unregister(player, "Combo");
		DamageBonusManager.Unregister(player, "ComboOverheat");
		SpeedBonusManager.Unregister(player, "Combo");
		SyncSpeed(player);
		if (previous > 0)
		{
			NotifyStatus(player, ClassName, new Dictionary<string, string>
			{
				{ "stacks", "0" },
				{ "bonus", "0" }
			});
		}
	}

	private void ClearBuffs(CCSPlayerController player)
	{
		DamageBonusManager.Unregister(player, "Combo");
		DamageBonusManager.Unregister(player, "ComboOverheat");
		SpeedBonusManager.Unregister(player, "Combo");
		SyncSpeed(player);
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

	private void ApplyGlow(CCSPlayerController victim, float now)
	{
		float expire = now + _config.Dices.Combo.GlowSeconds;
		GlowEntry existing = _glows.FirstOrDefault((GlowEntry entry) => entry.Victim == victim);
		if (existing != null)
		{
			existing.ExpireAt = expire;
			return;
		}
		CCSPlayerPawn pawn = victim.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return;
		}
		var (proxy, glow) = GlowUtil.CreateGlow(pawn, Color.FromArgb(255, 255, 140, 0));
		if (glow == null)
		{
			return;
		}
		_glows.Add(new GlowEntry
		{
			Victim = victim,
			Proxy = proxy,
			Glow = glow,
			ExpireAt = expire
		});
	}

	private void RemoveExpiredGlows(float now)
	{
		foreach (GlowEntry entry in _glows.Where((GlowEntry glow) => now >= glow.ExpireAt).ToList())
		{
			GlowUtil.RemoveGlow(entry.Proxy, entry.Glow);
			_glows.Remove(entry);
		}
	}

	private void RemoveGlowsFor(CCSPlayerController victim)
	{
		foreach (GlowEntry entry in _glows.Where((GlowEntry glow) => glow.Victim == victim).ToList())
		{
			GlowUtil.RemoveGlow(entry.Proxy, entry.Glow);
			_glows.Remove(entry);
		}
	}

	private void ClearAllGlows()
	{
		foreach (GlowEntry entry in _glows.ToList())
		{
			GlowUtil.RemoveGlow(entry.Proxy, entry.Glow);
		}
		_glows.Clear();
	}

	private void NotifyCombo(CCSPlayerController player, int stacks, float now)
	{
		ulong id = player.SteamID;
		if (_lastNotify.TryGetValue(id, out float last) && now - last < 0.6f && stacks < _config.Dices.Combo.MaxStacks)
		{
			return;
		}
		_lastNotify[id] = now;
		NotifyStatus(player, ClassName, new Dictionary<string, string>
		{
			{ "stacks", stacks.ToString() },
			{ "bonus", Math.Round(stacks * _config.Dices.Combo.DamagePerStack * 100f, 0).ToString() }
		});
	}
}
