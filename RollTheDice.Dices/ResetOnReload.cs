using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices;

public class ResetOnReload : DiceBlueprint
{
	public readonly Random _random = new Random();

	public override string ClassName => "ResetOnReload";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventWeaponReload";
			return list;
		}
	}

	public ResetOnReload(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if (userid == null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		userid.Respawn();
		return (HookResult)0;
	}
}
