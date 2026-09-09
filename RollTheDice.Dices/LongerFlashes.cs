using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices;

public class LongerFlashes : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "LongerFlashes";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerBlind";
			return list;
		}
	}

	public LongerFlashes(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public HookResult EventPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
	{
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (userid == null || !((CEntityInstance)userid).IsValid || attacker == null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)userid.PlayerPawn.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		float minBlinddurationFactor = _config.Dices.LongerFlashes.MinBlinddurationFactor;
		float maxBlinddurationFactor = _config.Dices.LongerFlashes.MaxBlinddurationFactor;
		float num = (float)(_random.NextDouble() * (double)(maxBlinddurationFactor - minBlinddurationFactor) + (double)minBlinddurationFactor);
		@event.BlindDuration *= num;
		((CCSPlayerPawnBase)userid.PlayerPawn.Value).FlashDuration = @event.BlindDuration;
		Utilities.SetStateChanged((CBaseEntity)(object)userid.PlayerPawn.Value, "CCSPlayerPawnBase", "m_flFlashDuration", 0);
		((CCSPlayerPawnBase)userid.PlayerPawn.Value).BlindUntilTime = Server.CurrentTime + ((CCSPlayerPawnBase)userid.PlayerPawn.Value).FlashDuration;
		userid.PlayerPawn.Value.VelocityModifier = _config.Dices.LongerFlashes.SlowMultiplier;
		Utilities.SetStateChanged((CBaseEntity)(object)userid.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		CCSPlayerController captured = userid;
		new Timer(((CCSPlayerPawnBase)userid.PlayerPawn.Value).FlashDuration, (Action)delegate
		{
			CCSPlayerController obj = captured;
			if ((CEntityInstance)(object)((obj == null) ? null : obj.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)captured.PlayerPawn.Value).IsValid)
			{
				captured.PlayerPawn.Value.VelocityModifier = 1f;
				Utilities.SetStateChanged((CBaseEntity)(object)captured.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}, (TimerFlags?)null);
		return (HookResult)0;
	}
}
