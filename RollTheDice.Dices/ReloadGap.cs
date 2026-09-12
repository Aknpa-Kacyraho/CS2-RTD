using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class ReloadGap : DiceBlueprint
{
	private sealed class ReloadState
	{
		public nint WeaponHandle;
		public float Start;
		public float GiveUpAt;
	}

	private readonly Dictionary<CCSPlayerController, ReloadState> _reloading = new Dictionary<CCSPlayerController, ReloadState>();

	private readonly Dictionary<CCSPlayerController, float> _bonusExpire = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "ReloadGap";

	public override List<string> Events => new List<string> { "EventWeaponReload" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public ReloadGap(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		DamageReductionManager.Unregister(player, "ReloadGap");
		DamageBonusManager.Unregister(player, "ReloadGap");
		_reloading.Remove(player);
		_bonusExpire.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			DamageReductionManager.Unregister(player, "ReloadGap");
			DamageBonusManager.Unregister(player, "ReloadGap");
		}
		_players.Clear();
		_reloading.Clear();
		_bonusExpire.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
	{
		if (_players.Count == 0)
		{
			return HookResult.Continue;
		}
		CCSPlayerController player = @event.Userid;
		if (player == null || !player.IsValid || !_players.Contains(player))
		{
			return HookResult.Continue;
		}
		CBasePlayerWeapon weapon = GetActiveWeapon(player);
		if (weapon == null || !weapon.IsValid)
		{
			return HookResult.Continue;
		}
		float now = Server.CurrentTime;
		DamageReductionManager.Register(player, "ReloadGap", _config.Dices.ReloadGap.ReloadDamageReduction);
		_reloading[player] = new ReloadState
		{
			WeaponHandle = weapon.Handle,
			Start = now,
			GiveUpAt = now + 10f
		};
		return HookResult.Continue;
	}

	public void OnTick()
	{
		float now = Server.CurrentTime;
		foreach (CCSPlayerController player in _reloading.Keys.ToList())
		{
			ReloadState state = _reloading[player];
			if (!player.IsValid)
			{
				FinishReload(player, completed: false);
				continue;
			}
			CBasePlayerWeapon weapon = GetActiveWeapon(player);
			if (weapon == null || !weapon.IsValid || weapon.Handle != state.WeaponHandle)
			{
				FinishReload(player, completed: false);
				continue;
			}
			CCSWeaponBase weaponBase = ((NativeObject)weapon).As<CCSWeaponBase>();
			bool stillReloading = weaponBase != null && weaponBase.InReload;
			if ((!stillReloading && now - state.Start >= 0.15f) || now >= state.GiveUpAt)
			{
				FinishReload(player, completed: true);
			}
		}
		foreach (CCSPlayerController player in _bonusExpire.Keys.ToList())
		{
			if (!player.IsValid || now >= _bonusExpire[player])
			{
				DamageBonusManager.Unregister(player, "ReloadGap");
				_bonusExpire.Remove(player);
			}
		}
	}

	private void FinishReload(CCSPlayerController player, bool completed)
	{
		DamageReductionManager.Unregister(player, "ReloadGap");
		_reloading.Remove(player);
		if (!completed)
		{
			return;
		}
		DamageBonusManager.Register(player, "ReloadGap", _config.Dices.ReloadGap.PostReloadDamageBonus);
		_bonusExpire[player] = Server.CurrentTime + _config.Dices.ReloadGap.PostReloadDuration;
	}

	private static CBasePlayerWeapon GetActiveWeapon(CCSPlayerController player)
	{
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if (pawn == null || !pawn.IsValid)
		{
			return null;
		}
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)pawn).WeaponServices;
		return weaponServices?.ActiveWeapon?.Value;
	}
}
