using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Tactician : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextRevealTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, List<(CDynamicProp?, CDynamicProp?)>> _activeGlows = new Dictionary<CCSPlayerController, List<(CDynamicProp, CDynamicProp)>>();

	private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _selfGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();

	public override string ClassName => "Tactician";

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

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

	public Tactician(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextRevealTime[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_nextRevealTime.Remove(player);
		RemoveGlowsForPlayer(player);
		RemoveSelfGlow(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RemoveGlowsForPlayer(item);
			RemoveSelfGlow(item);
		}
		_players.Clear();
		_nextRevealTime.Clear();
		_activeGlows.Clear();
		_selfGlows.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0 && _nextRevealTime.TryGetValue(item, out var value) && !(value > num))
				{
					RevealEnemies(item);
					_nextRevealTime[item] = num + _config.Dices.Tactician.RevealInterval;
				}
			}
			catch
			{
			}
		}
	}

	private void RevealEnemies(CCSPlayerController player)
	{
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		RemoveGlowsForPlayer(player);
		RemoveSelfGlow(player);
		List<(CDynamicProp, CDynamicProp)> list = new List<(CDynamicProp, CDynamicProp)>();
		foreach (CCSPlayerController item2 in Utilities.GetPlayers().Where(delegate(CCSPlayerController p)
		{
			int result;
			if (((CEntityInstance)p).IsValid)
			{
				CHandle<CCSPlayerPawn> playerPawn = p.PlayerPawn;
				if (playerPawn != null)
				{
					CCSPlayerPawn value = playerPawn.Value;
					if (((value != null) ? new bool?(((CEntityInstance)value).IsValid) : ((bool?)null)) == true && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum)
					{
						result = ((((CBaseEntity)p.PlayerPawn.Value).LifeState == 0) ? 1 : 0);
						goto IL_0069;
					}
				}
			}
			result = 0;
			goto IL_0069;
			IL_0069:
			return (byte)result != 0;
		}))
		{
			(CDynamicProp, CDynamicProp) item = GlowUtil.CreateGlow((CBaseEntity)(object)item2.PlayerPawn.Value, Color.Yellow);
			if ((CEntityInstance)(object)item.Item1 != (CEntityInstance)null && (CEntityInstance)(object)item.Item2 != (CEntityInstance)null)
			{
				list.Add(item);
			}
		}
		if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_selfGlows[player] = GlowUtil.CreateGlow((CBaseEntity)(object)player.PlayerPawn.Value, Color.Yellow);
		}
		if (list.Count > 0)
		{
			_activeGlows[player] = list;
			new Timer(_config.Dices.Tactician.RevealDuration, (Action)delegate
			{
				RemoveGlowsForPlayer(player);
				RemoveSelfGlow(player);
			}, (TimerFlags?)null);
		}
	}

	private void RemoveGlowsForPlayer(CCSPlayerController player)
	{
		if (!_activeGlows.TryGetValue(player, out List<(CDynamicProp, CDynamicProp)> value))
		{
			return;
		}
		foreach (var (glowProxy, glow) in value)
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)glowProxy, (CBaseEntity?)(object)glow);
		}
		_activeGlows.Remove(player);
	}

	private void RemoveSelfGlow(CCSPlayerController player)
	{
		if (_selfGlows.TryGetValue(player, out (CDynamicProp, CDynamicProp) value))
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)value.Item1, (CBaseEntity?)(object)value.Item2);
			_selfGlows.Remove(player);
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		RevealEnemies(attacker);
		attacker.PrintToCenterAlert("\ud83c\udfaf 击杀！暴露剩余敌人位置2秒");
		return (HookResult)0;
	}
}
