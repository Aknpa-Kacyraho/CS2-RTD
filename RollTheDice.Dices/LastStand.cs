using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class LastStand : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _expire = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "LastStand";

	public override List<string> Events => new List<string> { "EventWeaponFire" };

	public override List<string> Listeners => new List<string> { "OnTick" };

	public LastStand(PluginConfig globalConfig, MapConfig config, IStringLocalizer localizer)
		: base(globalConfig, config, localizer)
	{
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (player == null)
		{
			return;
		}
		DamageBonusManager.Unregister(player, "LastStand");
		_expire.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			DamageBonusManager.Unregister(player, "LastStand");
		}
		_players.Clear();
		_expire.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
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
		CBasePlayerWeaponVData vData = weapon.VData;
		if (vData == null || vData.MaxClip1 <= 1)
		{
			return HookResult.Continue;
		}
		if (weapon.Clip1 > _config.Dices.LastStand.ClipThreshold)
		{
			return HookResult.Continue;
		}
		DamageBonusManager.Register(player, "LastStand", _config.Dices.LastStand.DamageMultiplier - 1f);
		_expire[player] = Server.CurrentTime + _config.Dices.LastStand.WindowSeconds;
		return HookResult.Continue;
	}

	public void OnTick()
	{
		if (_expire.Count == 0)
		{
			return;
		}
		float now = Server.CurrentTime;
		foreach (CCSPlayerController player in _expire.Keys.ToList())
		{
			if (!player.IsValid || now >= _expire[player])
			{
				DamageBonusManager.Unregister(player, "LastStand");
				_expire.Remove(player);
			}
		}
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
