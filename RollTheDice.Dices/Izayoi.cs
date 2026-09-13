using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Izayoi : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _nextTriggerTime = new Dictionary<CCSPlayerController, float>();

	private static readonly float[] Timescales = new float[11]
	{
		0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.3f, 1.4f,
		1.5f
	};

	public override string ClassName => "Izayoi";

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

	public Izayoi(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		_comboActive = DiceSynergy.HasPartner(player, "Heaven");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "Heaven"))
			{
				DiceSynergy.AnnounceCombo(player, "超越天堂", "十六夜与天堂融合！获得超越天堂之力！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						instance.RemoveDiceFromPlayer(captured, "Izayoi");
						instance.RemoveDiceFromPlayer(captured, "Heaven");
						instance.ForceDiceForPlayer(captured, "BeyondHeaven");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！十六夜与天堂共鸣！");
		}
		_nextTriggerTime[player] = Server.CurrentTime + 1.5f;
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_nextTriggerTime.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextTriggerTime.Clear();
	}

	public void OnTick()
	{
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && _nextTriggerTime.TryGetValue(item, out var value) && !(num < value))
				{
					float num2 = Timescales[_random.Next(Timescales.Length)];
					float durationSeconds = _config.Dices.Izayoi.DurationSeconds;
					string text = ((num2 < 1f) ? $"减速 ({num2}x)" : ((num2 > 1f) ? $"加速 ({num2}x)" : $"正常 ({num2}x)"));
					Server.ExecuteCommand("host_timescale " + num2.ToString(CultureInfo.InvariantCulture));
					Server.PrintToChatAll("⏳ 时间被扰动了！" + text);
					new Timer(durationSeconds, (Action)delegate
					{
						Server.ExecuteCommand("host_timescale 1.0");
					}, (TimerFlags?)null);
					_nextTriggerTime[item] = num + _config.Dices.Izayoi.IntervalSeconds;
				}
			}
			catch
			{
			}
		}
	}
}
