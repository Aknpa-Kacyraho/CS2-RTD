using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 隐者 Hermit：常驻半透明；开火或受击后现形一段时间。
/// 与 ShadowWarrior 组合（暗影行者）：隐身更深。
/// </summary>
public class Hermit : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _revealUntil = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Hermit";

	public override List<string> Listeners => new List<string> { "OnTick" };

	public override List<string> Events => new List<string> { "EventWeaponFire", "EventPlayerHurt" };

	public override Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode>
	{
		{
			208,
			HookMode.Pre
		}
	};

	public Hermit(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_revealUntil[player] = 0f;
		if (DiceSynergy.HasPartner(player, "ShadowWarrior"))
		{
			DiceSynergy.AnnounceCombo(player, "暗影行者", "隐身更深！");
		}
		SetAlpha(player, BaseAlpha(player));
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
		_revealUntil.Remove(player);
		_players.Remove(player);
		if (player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (player != null && player.IsValid && player.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid)
			{
				((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
		_revealUntil.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		Reveal(@event.Userid);
		return HookResult.Continue;
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		Reveal(@event.Userid);
		return HookResult.Continue;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			bool revealed = _revealUntil.TryGetValue(player, out float until) && now < until;
			SetAlpha(player, revealed ? 255 : BaseAlpha(player));
		}
	}

	private void Reveal(CCSPlayerController player)
	{
		if (player != null && player.IsValid && _players.Contains(player))
		{
			_revealUntil[player] = Server.CurrentTime + _config.Dices.Hermit.RevealSeconds;
		}
	}

	private int BaseAlpha(CCSPlayerController player)
	{
		return DiceSynergy.HasPartner(player, "ShadowWarrior") ? 10 : 30;
	}

	private static void SetAlpha(CCSPlayerController player, int alpha)
	{
		CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
		if (pawn != null && pawn.IsValid)
		{
			((CBaseModelEntity)pawn).Render = Color.FromArgb(alpha, 255, 255, 255);
			Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	public HookResult HookUserMessage208(UserMessage um)
	{
		int sourceIndex = um.ReadInt("source_entity_index", null);
		foreach (CCSPlayerController player in _players)
		{
			if (player?.PlayerPawn?.Value != null && player.PlayerPawn.Value.IsValid && player.PlayerPawn.Value.Index == sourceIndex)
			{
				um.Recipients.Clear();
				return HookResult.Stop;
			}
		}
		return HookResult.Continue;
	}
}
