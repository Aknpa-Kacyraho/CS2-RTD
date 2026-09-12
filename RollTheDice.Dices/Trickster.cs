using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Trickster : DiceBlueprint
{
	private bool _revealed;

	private static readonly string[] FakeNames = new string[6] { "命悬一线", "疾风步", "千钧", "轻功", "铁腕", "涅槃" };

	public static Dictionary<ulong, string> PendingFakeNames = new Dictionary<ulong, string>();

	public override string ClassName => "Trickster";

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

	public Trickster(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_revealed = false;
			string text = FakeNames[Random.Shared.Next(FakeNames.Length)];
			PendingFakeNames[((CBasePlayerController)player).SteamID] = text;
			string text2 = _localizer["dice_Trickster_fake"].Value.Replace("{fakeName}", text);
			player.PrintToChat(_localizer["command.prefix"].Value + text2);
			player.PrintToCenterAlert("\ud83c\udfad 你获得了 " + text + "！");
			if (DiceSynergy.HasPartner(player, "Mimic"))
			{
				DiceSynergy.AnnounceCombo(player, "千面戏法", "击杀揭露时将复制敌人的真实骰子！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		PendingFakeNames.Remove(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_revealed = false;
		PendingFakeNames.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		if (_revealed)
		{
			return (HookResult)0;
		}
		_revealed = true;
		PendingFakeNames.Remove(((CBasePlayerController)attacker).SteamID);
		attacker.PrintToCenterAlert("\ud83c\udfad 诡术揭晓！露出了真正的面目...");
		attacker.PrintToChat(_localizer["command.prefix"].Value + _localizer["dice_Trickster_reveal"].Value);
		RollTheDice instance = RollTheDice.Instance;
		bool copied = false;
		if (instance != null && DiceSynergy.HasPartner(attacker, "Mimic"))
		{
			List<string> candidates = new List<string>();
			foreach (CCSPlayerController enemy in Utilities.GetPlayers())
			{
				if ((CEntityInstance)(object)enemy == (CEntityInstance)null || !((CEntityInstance)enemy).IsValid || enemy.IsBot || ((CBasePlayerController)enemy).IsHLTV || ((CBaseEntity)enemy).TeamNum == ((CBaseEntity)attacker).TeamNum)
				{
					continue;
				}
				candidates.AddRange(instance.GetAllDiceForPlayer(enemy));
			}
			if (candidates.Count > 0)
			{
				string pick = candidates[Random.Shared.Next(candidates.Count)];
				copied = instance.ForceDiceForPlayer(attacker, pick);
				if (copied)
				{
					attacker.PrintToCenterAlert($"\ud83c\udfad 千面戏法！复制了 {pick}！");
					Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udfad {((CBasePlayerController)attacker).PlayerName} 触发千面戏法，复制了 {pick}！");
				}
			}
		}
		if (!copied)
		{
			instance?.ForceExtraDiceForPlayer(attacker);
		}
		return (HookResult)0;
	}
}
