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
/// 倒计时 Countdown：按 E 启动倒计时，结束后回到出生点、满血满甲并还原武器。
/// 与 Rewind 组合（时空主宰）。
/// </summary>
public class Countdown : DiceBlueprint
{
	private sealed class CountdownState
	{
		public Vector SpawnPosition;

		public QAngle SpawnAngles;

		public List<string> OriginalWeapons = new List<string>();

		public int OriginalHP;

		public int OriginalArmor;

		public bool OriginalHelmet;

		public float EndTime;

		public float CooldownUntil;

		public bool Active;

		public bool ComboActive;
	}

	private readonly Dictionary<CCSPlayerController, CountdownState> _states = new Dictionary<CCSPlayerController, CountdownState>();

	public override string ClassName => "Countdown";

	public override List<string> Listeners => new List<string> { "OnPlayerButtonsChanged", "OnTick" };

	public Countdown(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		CCSPlayerPawn pawn = player.PlayerPawn.Value;
		List<string> weapons = new List<string>();
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)pawn).WeaponServices;
		if (weaponServices?.MyWeapons != null)
		{
			foreach (CHandle<CBasePlayerWeapon> handle in weaponServices.MyWeapons)
			{
				string designerName = handle?.Value?.DesignerName;
				if (designerName != null && !designerName.Contains("knife") && !designerName.Contains("bayonet"))
				{
					weapons.Add(designerName);
				}
			}
		}
		Vector origin = ((CBaseEntity)pawn).AbsOrigin;
		QAngle rotation = ((CBaseEntity)pawn).AbsRotation;
		CountdownState state = new CountdownState
		{
			SpawnPosition = new Vector(origin?.X ?? 0f, origin?.Y ?? 0f, origin?.Z ?? 0f),
			SpawnAngles = new QAngle(rotation?.X ?? 0f, rotation?.Y ?? 0f, rotation?.Z ?? 0f),
			OriginalWeapons = weapons,
			OriginalHP = ((CBaseEntity)pawn).Health,
			OriginalArmor = pawn.ArmorValue,
			OriginalHelmet = ((CBasePlayerPawn)pawn).ItemServices != null && new CCSPlayer_ItemServices(((CBasePlayerPawn)pawn).ItemServices.Handle).HasHelmet
		};
		state.ComboActive = DiceSynergy.HasPartner(player, "Rewind");
		if (state.ComboActive)
		{
			DiceSynergy.AnnounceCombo(player, "时空主宰", "倒计时结束获得额外生命！");
		}
		_states[player] = state;
		_players.Add(player);
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
		player.PrintToCenterAlert("按 E 启动倒计时回溯");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_states.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		_states.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (player == null || !player.IsValid || !_states.TryGetValue(player, out CountdownState state))
		{
			return;
		}
		if (!pressed.HasFlag(PlayerButtons.Use) || state.Active)
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		if (now < state.CooldownUntil)
		{
			player.PrintToCenterAlert($"倒计时冷却中… {state.CooldownUntil - now:F0}s");
			return;
		}
		state.Active = true;
		state.EndTime = now + _config.Dices.Countdown.Seconds;
		player.PrintToCenterAlert($"倒计时启动！{_config.Dices.Countdown.Seconds:F0}s 后回溯");
	}

	public void OnTick()
	{
		if (_states.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, CountdownState> entry in _states.ToList())
		{
			CCSPlayerController player = entry.Key;
			CountdownState state = entry.Value;
			if (!state.Active)
			{
				continue;
			}
			float remaining = state.EndTime - now;
			if (remaining > 0f)
			{
				if (Server.TickCount % 64 == 0 && player != null)
				{
					player.PrintToCenterAlert($"倒计时 {Math.Ceiling(remaining):F0}s");
				}
				continue;
			}
			state.Active = false;
			state.CooldownUntil = now + _config.Dices.Countdown.Cooldown;
			CCSPlayerPawn pawn = player?.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			int hp = state.ComboActive ? (Math.Max(state.OriginalHP, 100) + 50) : Math.Max(state.OriginalHP, 100);
			((CBaseEntity)pawn).Teleport(state.SpawnPosition, state.SpawnAngles, new Vector(0f, 0f, 0f));
			((CBaseEntity)pawn).MaxHealth = Math.Max(((CBaseEntity)pawn).MaxHealth, hp);
			((CBaseEntity)pawn).Health = hp;
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			player.RemoveWeapons();
			player.GiveNamedItem("weapon_knife");
			foreach (string weapon in state.OriginalWeapons)
			{
				player.GiveNamedItem(weapon);
			}
			pawn.ArmorValue = (state.OriginalArmor > 0) ? state.OriginalArmor : 100;
			Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
			if (state.OriginalHelmet && ((CBasePlayerPawn)pawn).ItemServices != null)
			{
				new CCSPlayer_ItemServices(((CBasePlayerPawn)pawn).ItemServices.Handle).HasHelmet = true;
			}
			if ((int)player.Team == 3 && ((CBasePlayerPawn)pawn).ItemServices != null)
			{
				new CCSPlayer_ItemServices(((CBasePlayerPawn)pawn).ItemServices.Handle).HasDefuser = true;
			}
			player.PrintToCenterAlert("倒计时结束！已回溯至出生点，满血满甲！");
		}
	}
}
