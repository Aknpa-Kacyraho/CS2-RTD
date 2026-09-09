using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Deaf : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, (CDynamicProp? Proxy, CDynamicProp? Glow, CCSPlayerController? Target)> _activeGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp, CCSPlayerController)>();

	public override string ClassName => "Deaf";

	public override Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode> { 
	{
		208,
		(HookMode)0
	} };

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public Deaf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid)
		{
			_players.Add(player);
			_cooldowns[player] = 0f;
			player.ExecuteClientCommand("volume 0");
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udd07 完全失聪！按E穿墙透视随机敌人4秒！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		RemoveGlowForPlayer(player);
		player.ExecuteClientCommand("volume 0.5");
		_players.Remove(player);
		_cooldowns.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (item != null)
			{
				item.ExecuteClientCommand("volume 0.5");
			}
			RemoveGlowForPlayer(item);
		}
		_players.Clear();
		_cooldowns.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RemoveGlowForPlayer(CCSPlayerController holder)
	{
		if (_activeGlows.TryGetValue(holder, out (CDynamicProp, CDynamicProp, CCSPlayerController) value))
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)value.Item1, (CBaseEntity?)(object)value.Item2);
			_activeGlows.Remove(holder);
		}
	}

	public HookResult HookUserMessage208(UserMessage um)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		int num = um.ReadInt("source_entity_index", (int?)null);
		foreach (CCSPlayerController player in _players)
		{
			uint? obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
				if (playerPawn == null)
				{
					obj = null;
				}
				else
				{
					CCSPlayerPawn value = playerPawn.Value;
					obj = ((value != null) ? new uint?(((CEntityInstance)value).Index) : ((uint?)null));
				}
			}
			if (obj == num)
			{
				um.Recipients.Clear();
				return (HookResult)4;
			}
		}
		return (HookResult)0;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (_cooldowns.TryGetValue(player, out var value) && num < value)
		{
			float value2 = value - num;
			player.PrintToCenterAlert($"⏳ 冷却中... {value2:F0}秒");
			return;
		}
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
			select p).ToList();
		if (list.Count == 0)
		{
			player.PrintToCenterAlert("❌ 没有存活的敌人！");
			return;
		}
		CCSPlayerController val = list[Random.Shared.Next(list.Count)];
		if ((CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid)
		{
			return;
		}
		RemoveGlowForPlayer(player);
		var (val2, val3) = GlowUtil.CreateGlow((CBaseEntity)(object)val.PlayerPawn.Value, Color.Red);
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || (CEntityInstance)(object)val3 == (CEntityInstance)null)
		{
			player.PrintToCenterAlert("❌ 透视失败！");
			return;
		}
		float wallhackDuration = _config.Dices.Deaf.WallhackDuration;
		_activeGlows[player] = (val2, val3, val);
		_cooldowns[player] = num + _config.Dices.Deaf.Cooldown;
		player.PrintToCenterAlert($"\ud83d\udc41 透视 {((CBasePlayerController)val).PlayerName}！持续{wallhackDuration}秒");
		CCSPlayerController captured = player;
		new Timer(wallhackDuration, (Action)delegate
		{
			if (_activeGlows.TryGetValue(captured, out (CDynamicProp, CDynamicProp, CCSPlayerController) value3))
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)value3.Item1, (CBaseEntity?)(object)value3.Item2);
				_activeGlows.Remove(captured);
			}
		}, (TimerFlags?)null);
	}
}
