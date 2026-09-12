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

public class Crouch : DiceBlueprint
{
	private readonly HashSet<CCSPlayerController> _crouching = new HashSet<CCSPlayerController>();

	private readonly Dictionary<ulong, float> _healAcc = new Dictionary<ulong, float>();

	private float _lastTick = -1f;

	public override string ClassName => "Crouch";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public Crouch(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		SetCrouch(player, false);
		_healAcc.Remove(player.SteamID);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			SetCrouch(player, false);
		}
		_players.Clear();
		_crouching.Clear();
		_healAcc.Clear();
		_lastTick = -1f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (_players.Count == 0 || player == null || !player.IsValid || !_players.Contains(player))
		{
			return;
		}
		if ((pressed & PlayerButtons.Duck) != 0)
		{
			SetCrouch(player, true);
		}
		if ((released & PlayerButtons.Duck) != 0)
		{
			SetCrouch(player, false);
		}
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
		if (dt <= 0f || _config.Dices.Crouch.HealPerSecond <= 0f)
		{
			return;
		}
		foreach (CCSPlayerController player in _crouching.ToList())
		{
			if (player == null || !player.IsValid || !_players.Contains(player))
			{
				SetCrouch(player, false);
				continue;
			}
			CCSPlayerPawn pawn = player.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || pawn.LifeState != 0)
			{
				continue;
			}
			float acc = (_healAcc.TryGetValue(player.SteamID, out float value) ? value : 0f) + _config.Dices.Crouch.HealPerSecond * dt;
			int whole = (int)acc;
			if (whole > 0)
			{
				acc -= whole;
				pawn.Health = Math.Min(pawn.Health + whole, pawn.MaxHealth);
				Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			}
			_healAcc[player.SteamID] = acc;
		}
	}

	private void SetCrouch(CCSPlayerController player, bool crouching)
	{
		if (player == null)
		{
			return;
		}
		if (crouching)
		{
			if (_crouching.Add(player))
			{
				DamageReductionManager.Register(player, "Crouch", _config.Dices.Crouch.DamageReduction);
			}
		}
		else if (_crouching.Remove(player))
		{
			DamageReductionManager.Unregister(player, "Crouch");
		}
	}
}
