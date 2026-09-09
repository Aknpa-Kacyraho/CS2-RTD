using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Nightglow : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _playerGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();

	private float _lastFadeReapply;

	private const float FadeReapplyInterval = 3f;

	public override string ClassName => "Nightglow";

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

	public Nightglow(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			ApplyDarkFade();
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udca1 {((CBasePlayerController)player).PlayerName} 释放了夜光！地图完全黑暗，所有玩家发光！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (_players.Count == 0)
		{
			ClearEffects();
		}
	}

	public override void Reset()
	{
		ClearEffects();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void ApplyDarkFade()
	{
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV
			select p)
		{
			UserMessage val = UserMessage.FromPartialName("Fade");
			val.SetInt("duration", 5000, (int?)null);
			val.SetInt("hold_time", 999999, (int?)null);
			val.SetInt("flags", 17, (int?)null);
			val.SetInt("color", -1610612736, (int?)null);
			val.Recipients.Add(item);
			val.Send();
		}
	}

	private void ClearEffects()
	{
		foreach (KeyValuePair<CCSPlayerController, (CDynamicProp, CDynamicProp)> item in _playerGlows.ToList())
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)item.Value.Item1, (CBaseEntity?)(object)item.Value.Item2);
		}
		_playerGlows.Clear();
		foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid
			select p)
		{
			UserMessage val = UserMessage.FromPartialName("Fade");
			val.SetInt("duration", 100, (int?)null);
			val.SetInt("hold_time", 0, (int?)null);
			val.SetInt("flags", 17, (int?)null);
			val.SetInt("color", 0, (int?)null);
			val.Recipients.Add(item2);
			val.Send();
		}
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num - _lastFadeReapply > 3f)
		{
			_lastFadeReapply = num;
			ApplyDarkFade();
		}
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p).ToList();
		HashSet<nint> hashSet = new HashSet<nint>();
		foreach (CCSPlayerController item in list)
		{
			hashSet.Add(((NativeEntity)item.PlayerPawn.Value).Handle);
			if (!_playerGlows.ContainsKey(item))
			{
				Color color = ((((CBaseEntity)item).TeamNum == 2) ? Color.OrangeRed : Color.DodgerBlue);
				_playerGlows[item] = GlowUtil.CreateGlow((CBaseEntity)(object)item.PlayerPawn.Value, color);
			}
		}
		foreach (KeyValuePair<CCSPlayerController, (CDynamicProp, CDynamicProp)> item2 in _playerGlows.ToList())
		{
			if ((CEntityInstance)(object)item2.Key.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item2.Key.PlayerPawn.Value).IsValid || ((CBaseEntity)item2.Key.PlayerPawn.Value).LifeState != 0 || !hashSet.Contains(((NativeEntity)item2.Key.PlayerPawn.Value).Handle))
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)item2.Value.Item1, (CBaseEntity?)(object)item2.Value.Item2);
				_playerGlows.Remove(item2.Key);
			}
		}
	}
}
