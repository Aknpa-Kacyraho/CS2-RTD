using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DiceBlueprint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
{
	public readonly PluginConfig _globalConfig = GlobalConfig;

	public readonly MapConfig _config = Config;

	public readonly IStringLocalizer _localizer = Localizer;

	public readonly List<CCSPlayerController> _players = new List<CCSPlayerController>();

	/// <summary>No holder → skip this dice's tick/damage callbacks.</summary>
	public bool HasAnyHolder => _players.Count > 0;

	/// <summary>
	/// True when a dice must keep ticking after its holders are gone, e.g. while its
	/// frame-budgeted CBeam teardown queue is draining. Override in dice that own such a
	/// queue; otherwise the empty-holder shortcut would freeze the queue and leak beams.
	/// </summary>
	public virtual bool NeedsBackgroundTick => false;

	// Every timer created by a dice goes through ScopedTimer and is killed on
	// round end / module destroy. Raw "new Timer" callbacks outlive a reload.
	private readonly List<Timer> _scopedTimers = new List<Timer>();

	protected Timer ScopedTimer(float delay, Action callback, TimerFlags? flags = null)
	{
		Timer? timer = null;
		timer = global::RollTheDice.RollTheDice.Instance!.AddTimer(delay, () =>
		{
			if (timer != null)
			{
				_scopedTimers.Remove(timer);
			}
			callback();
		}, flags);
		_scopedTimers.Add(timer);
		return timer;
	}

	/// <summary>
	/// Cancel a timer created by <see cref="ScopedTimer"/> and remove it from the
	/// ownership list at the same time. Calling <c>timer.Kill()</c> directly leaves
	/// the cancelled timer in <see cref="_scopedTimers"/> until round end.
	/// </summary>
	protected void CancelScopedTimer(Timer? timer)
	{
		if (timer == null)
		{
			return;
		}
		try
		{
			timer.Kill();
		}
		catch
		{
		}
		_scopedTimers.Remove(timer);
	}

	/// <summary>Kill every timer owned by this dice. Called on round end and unload.</summary>
	public void KillTimers()
	{
		foreach (Timer timer in _scopedTimers)
		{
			try
			{
				timer.Kill();
			}
			catch
			{
			}
		}
		_scopedTimers.Clear();
	}

	/// <summary>
	/// False between round end and the next round start. Delayed callbacks that would
	/// damage/spawn/revive (e.g. a 10s black-hole explosion) check this so they cannot
	/// leak into the next round.
	/// </summary>
	public static bool RoundActive { get; set; }

	/// <summary>
	/// Damage hooks registered by DynamicHandlers for this dice. The raw delegate is wrapped
	/// so non-player entities never reach a dice that would cast them to CCSPlayerPawn.
	/// </summary>
	public Func<CBaseEntity, CTakeDamageInfo, HookResult>? DamageHook { get; set; }

	/// <summary>The delegate instance actually registered with BasePlugin (needed for removal).</summary>
	public Delegate? DamageHookWrapper { get; set; }

	/// <summary>Original dice OnTick, invoked only while at least one player holds this dice.</summary>
	public Action? TickHook { get; set; }

	/// <summary>The wrapper registered with BasePlugin (needed for removal).</summary>
	public Delegate? TickHookWrapper { get; set; }

	public void SafeOnTick()
	{
		if (!RoundActive && !NeedsBackgroundTick)
		{
			return;
		}
		if (HasAnyHolder || NeedsBackgroundTick)
		{
			TickHook?.Invoke();
		}
	}

	public HookResult SafeOnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		// A dice nobody holds must not pay for entity resolution on every hit.
		if (!HasAnyHolder)
		{
			return HookResult.Continue;
		}
		if (!RoundActive)
		{
			return HookResult.Continue;
		}
		if (info == null || PlayerResolver.FromEntity(entity) == null)
		{
			return HookResult.Continue;
		}
		CBaseEntity? attackerEntity = info.Attacker?.Value;
		if (attackerEntity != null && PlayerResolver.FromEntity(attackerEntity) == null)
		{
			return HookResult.Continue;
		}
		return DamageHook?.Invoke(entity, info) ?? HookResult.Continue;
	}

	public virtual string Description { get; private set; } = "Unknown Dice";

	public virtual string ClassName => "DiceBlueprint";

	public virtual bool CanBeDrawn => true;

	public virtual float Weight
	{
		get
		{
			var rarity = _globalConfig?.Dices?.Rarity;
			if (rarity != null)
			{
				if (rarity.DiceTier.TryGetValue(ClassName, out string tier) && tier != null && rarity.TierWeights.TryGetValue(tier, out float configured))
				{
					return configured;
				}
				if (rarity.TierWeights.TryGetValue("common", out float common))
				{
					return common;
				}
			}
			return 1f;
		}
	}

	public virtual bool IsSpecial => false;

	public virtual float SecondRoundProbability => 0f;

	public virtual string? SecondRoundRewardId => null;

	public virtual bool RequiresDrawSimulation => false;

	public virtual List<string> ExcludeFromReroll => new List<string>();

	public virtual string? TeammateBonusDice => null;

	public virtual float TeammateBonusChance => 0f;

	public virtual List<string> Events => new List<string>();

	public virtual List<string> Listeners => new List<string>();

	public virtual Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode>();

	public virtual List<string> Precache => new List<string>();

	public virtual float GetCooldownRemaining(CCSPlayerController player)
	{
		return 0f;
	}

	public virtual void OnDiceSetChanged(CCSPlayerController player)
	{
	}

	public virtual void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			// A player may roll/give the same dice again; never stack duplicate holders.
			if (HasPlayer(player.SteamID))
			{
				return;
			}
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	/// <summary>True when this dice is held by the given SteamID (survives controller recreation).</summary>
	public bool HasPlayer(ulong steamId)
	{
		foreach (CCSPlayerController p in _players)
		{
			if (p != null && p.IsValid && p.SteamID == steamId)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Remove every holder with the given SteamID (including stale controller instances).</summary>
	public void RemoveBySteamId(ulong steamId)
	{
		_players.RemoveAll(p => p == null || !p.IsValid || p.SteamID == steamId);
	}

	/// <summary>
	/// Disconnect fallback: remove holders by SteamID and let dice that own
	/// per-player timers/state clean up. The controller may already be invalid, so
	/// derived dice should match on SteamID instead of requiring a valid controller.
	/// </summary>
	public virtual void RemoveBySteamId(ulong steamId, DiceRemoveReason reason)
	{
		RemoveBySteamId(steamId);
		OnHolderRemoved(steamId, reason);
	}

	/// <summary>
	/// Disconnect/prune hook for dice that keep per-player timers or dictionaries.
	/// The controller may already be invalid; implementations must match on SteamID.
	/// </summary>
	public virtual void OnHolderRemoved(ulong steamId, DiceRemoveReason reason)
	{
	}

	/// <summary>
	/// Remove controller-keyed entries without touching invalid pawn/schema data.
	/// Invalid keys are treated as stale; valid keys must match the SteamID.
	/// </summary>
	protected static void RemoveControllerEntries<T>(
		Dictionary<CCSPlayerController, T> map,
		ulong steamId)
	{
		List<CCSPlayerController> remove = new List<CCSPlayerController>();
		foreach (KeyValuePair<CCSPlayerController, T> pair in map)
		{
			CCSPlayerController key = pair.Key;
			if (key == null)
			{
				continue;
			}
			if (!key.IsValid || key.SteamID == steamId)
			{
				remove.Add(key);
			}
		}
		foreach (CCSPlayerController key in remove)
		{
			map.Remove(key);
		}
	}

	/// <summary>Drop controllers that are no longer valid. Returns the number removed.</summary>
	public int PruneInvalid()
	{
		return _players.RemoveAll(p => p == null || !p.IsValid);
	}

	public virtual void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		bool valid = player != null && ((CEntityInstance)player).IsValid;
		ulong steamId = valid ? player.SteamID : 0UL;
		_players.RemoveAll(p => p == null || !p.IsValid || (steamId != 0UL && p.SteamID == steamId));
	}

	public virtual void Reset()
	{
		_players.Clear();
	}

	public virtual void Destroy()
	{
		Reset();
	}

	public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
	{
		if (_localizer["dice_" + diceName + "_player"].ResourceNotFound || (!_globalConfig.NotifyPlayerViaChatMsg && !_globalConfig.NotifyPlayerViaCenterMsg))
		{
			return;
		}
		string text = _localizer["dice_" + diceName + "_player"].Value;
		foreach (KeyValuePair<string, string> datum in data)
		{
			text = text.Replace("{" + datum.Key + "}", datum.Value);
		}
		if (_globalConfig.NotifyPlayerViaCenterMsg)
		{
			player.PrintToCenter(text);
		}
		if (_globalConfig.NotifyPlayerViaChatMsg)
		{
			player.PrintToChat(_localizer["command.prefix"].Value + text);
		}
		Description = text;
	}

	public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
	{
		if (_localizer["dice_" + diceName + "_status"].ResourceNotFound)
		{
			return;
		}
		string text = _localizer["dice_" + diceName + "_status"].Value;
		foreach (KeyValuePair<string, string> datum in data)
		{
			text = text.Replace("{" + datum.Key + "}", datum.Value);
		}
		player.PrintToCenter(text);
		player.PrintToChat(_localizer["command.prefix"].Value + text);
		Description = text;
	}
}
