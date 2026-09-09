using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Twilight : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private bool _swapped;

	private float _nextSwapTime;

	private const float SwapInterval = 30f;

	public override string ClassName => "Twilight";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			return list;
		}
	}

	public Twilight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		if (!_swapped)
		{
			_swapped = true;
			_nextSwapTime = Server.CurrentTime + 30f;
			Server.NextFrame((Action)delegate
			{
				SwapAllPlayers();
			});
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_swapped = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count != 0)
		{
			float num = Server.CurrentTime;
			if (num >= _nextSwapTime)
			{
				_nextSwapTime = num + 30f;
				SwapAllPlayers();
			}
		}
	}

	private void SwapAllPlayers()
	{
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_016e: Expected O, but got Unknown
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
			select p).ToList();
		if (list.Count >= 2)
		{
			List<(CCSPlayerController, Vector)> list2 = list.Select(delegate(CCSPlayerController p)
			{
				//IL_0052: Unknown result type (might be due to invalid IL or missing references)
				//IL_005c: Expected O, but got Unknown
				return ((CCSPlayerController p, Vector))(p: p, new Vector((float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.Z));
			}).ToList();
			List<Vector> list3 = list2.Select<(CCSPlayerController, Vector), Vector>(((CCSPlayerController p, Vector) p) => p.Item2).ToList();
			for (int num = list3.Count - 1; num > 0; num--)
			{
				int num2 = _random.Next(num + 1);
				List<Vector> list4 = list3;
				int index = num;
				int index2 = num2;
				Vector value = list3[num2];
				Vector value2 = list3[num];
				list4[index] = value;
				list3[index2] = value2;
			}
			for (int num3 = 0; num3 < list2.Count; num3++)
			{
				((CBaseEntity)list2[num3].Item1.PlayerPawn.Value).Teleport(list3[num3], new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
			}
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83c\udf05 黄昏之时！全员位置随机互换！");
		}
	}
}
