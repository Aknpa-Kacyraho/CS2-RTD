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

public class Rally : DiceBlueprint
{
	private readonly Dictionary<(ulong Player, ulong Holder), float> _appliedDamage = new Dictionary<(ulong, ulong), float>();

	private readonly Dictionary<(ulong Player, ulong Holder), float> _appliedReduction = new Dictionary<(ulong, ulong), float>();

	private readonly Dictionary<ulong, int> _lastNotifiedLayer = new Dictionary<ulong, int>();

	private float _lastReconcile = -1f;

	public override string ClassName => "Rally";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public Rally(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		_players.Remove(player);
		_lastNotifiedLayer.Remove(player.SteamID);
		UnregisterHolder(player.SteamID);
	}

	public override void Reset()
	{
		UnregisterAll();
		_players.Clear();
		_lastNotifiedLayer.Clear();
		_lastReconcile = -1f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0 && _appliedDamage.Count == 0 && _appliedReduction.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		float interval = Math.Max(_config.Dices.Rally.RefreshInterval, 0.05f);
		if (_lastReconcile >= 0f && now - _lastReconcile < interval)
		{
			return;
		}
		_lastReconcile = now;
		Reconcile();
	}

	private void Reconcile()
	{
		Dictionary<(ulong Player, ulong Holder), float> desiredDamage = new Dictionary<(ulong, ulong), float>();
		Dictionary<(ulong Player, ulong Holder), float> desiredReduction = new Dictionary<(ulong, ulong), float>();
		float radius = _config.Dices.Rally.Radius;
		int maxStacks = Math.Max(_config.Dices.Rally.MaxStacks, 0);
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if (holder == null || !holder.IsValid || holder.IsHLTV || holder.IsBot)
			{
				continue;
			}
			CCSPlayerPawn holderPawn = holder.PlayerPawn?.Value;
			if (holderPawn == null || !holderPawn.IsValid || holderPawn.LifeState != 0 || holderPawn.AbsOrigin == null)
			{
				NotifyLayer(holder, 0);
				continue;
			}
			ulong holderId = holder.SteamID;
			List<ulong> allies = new List<ulong>();
			foreach (CCSPlayerController other in Utilities.GetPlayers())
			{
				if (other == null || other.IsBot || other.IsHLTV || !other.IsValid || other == holder)
				{
					continue;
				}
				if (((CBaseEntity)other).TeamNum != ((CBaseEntity)holder).TeamNum)
				{
					continue;
				}
				CCSPlayerPawn pawn = other.PlayerPawn?.Value;
				if (pawn == null || !pawn.IsValid || pawn.LifeState != 0 || pawn.AbsOrigin == null)
				{
					continue;
				}
				if (Vectors.GetDistance(holderPawn.AbsOrigin, pawn.AbsOrigin) <= radius)
				{
					allies.Add(other.SteamID);
				}
			}
			int layers = Math.Min(allies.Count, maxStacks);
			NotifyLayer(holder, layers);
			if (layers <= 0)
			{
				continue;
			}
			float damage = _config.Dices.Rally.DamagePerAlly * layers;
			float reduction = _config.Dices.Rally.ReductionPerAlly * layers;
			desiredDamage[(holderId, holderId)] = damage;
			desiredReduction[(holderId, holderId)] = reduction;
			foreach (ulong allyId in allies)
			{
				desiredDamage[(allyId, holderId)] = damage;
				desiredReduction[(allyId, holderId)] = reduction;
			}
		}
		ApplyDamage(desiredDamage);
		ApplyReduction(desiredReduction);
	}

	private void ApplyDamage(Dictionary<(ulong Player, ulong Holder), float> desired)
	{
		foreach ((ulong Player, ulong Holder) key in _appliedDamage.Keys.ToList())
		{
			if (!desired.ContainsKey(key))
			{
				DamageBonusManager.UnregisterBySteamId(key.Player, SourceFor(key.Holder));
				_appliedDamage.Remove(key);
			}
		}
		foreach (KeyValuePair<(ulong Player, ulong Holder), float> kv in desired)
		{
			if (!_appliedDamage.TryGetValue(kv.Key, out float previous) || Math.Abs(previous - kv.Value) > 0.0001f)
			{
				DamageBonusManager.RegisterBySteamId(kv.Key.Player, SourceFor(kv.Key.Holder), kv.Value);
				_appliedDamage[kv.Key] = kv.Value;
			}
		}
	}

	private void ApplyReduction(Dictionary<(ulong Player, ulong Holder), float> desired)
	{
		foreach ((ulong Player, ulong Holder) key in _appliedReduction.Keys.ToList())
		{
			if (!desired.ContainsKey(key))
			{
				DamageReductionManager.UnregisterBySteamId(key.Player, SourceFor(key.Holder));
				_appliedReduction.Remove(key);
			}
		}
		foreach (KeyValuePair<(ulong Player, ulong Holder), float> kv in desired)
		{
			if (!_appliedReduction.TryGetValue(kv.Key, out float previous) || Math.Abs(previous - kv.Value) > 0.0001f)
			{
				DamageReductionManager.RegisterBySteamId(kv.Key.Player, SourceFor(kv.Key.Holder), kv.Value);
				_appliedReduction[kv.Key] = kv.Value;
			}
		}
	}

	private void UnregisterHolder(ulong holderId)
	{
		string source = SourceFor(holderId);
		foreach ((ulong Player, ulong Holder) key in _appliedDamage.Keys.Where((k) => k.Holder == holderId).ToList())
		{
			DamageBonusManager.UnregisterBySteamId(key.Player, source);
			_appliedDamage.Remove(key);
		}
		foreach ((ulong Player, ulong Holder) key in _appliedReduction.Keys.Where((k) => k.Holder == holderId).ToList())
		{
			DamageReductionManager.UnregisterBySteamId(key.Player, source);
			_appliedReduction.Remove(key);
		}
	}

	private void UnregisterAll()
	{
		foreach ((ulong Player, ulong Holder) key in _appliedDamage.Keys.ToList())
		{
			DamageBonusManager.UnregisterBySteamId(key.Player, SourceFor(key.Holder));
		}
		foreach ((ulong Player, ulong Holder) key in _appliedReduction.Keys.ToList())
		{
			DamageReductionManager.UnregisterBySteamId(key.Player, SourceFor(key.Holder));
		}
		_appliedDamage.Clear();
		_appliedReduction.Clear();
	}

	private void NotifyLayer(CCSPlayerController holder, int layers)
	{
		if (holder == null || !holder.IsValid)
		{
			return;
		}
		ulong id = holder.SteamID;
		if (_lastNotifiedLayer.TryGetValue(id, out int last) && last == layers)
		{
			return;
		}
		_lastNotifiedLayer[id] = layers;
		NotifyStatus(holder, ClassName, new Dictionary<string, string>
		{
			{ "stacks", layers.ToString() },
			{ "damage", Math.Round(_config.Dices.Rally.DamagePerAlly * layers * 100f, 0).ToString() },
			{ "reduction", Math.Round(_config.Dices.Rally.ReductionPerAlly * layers * 100f, 0).ToString() }
		});
	}

	private static string SourceFor(ulong holderId)
	{
		return "Rally_" + holderId;
	}
}
