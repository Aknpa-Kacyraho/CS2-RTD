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

public class Heaven : DiceBlueprint
{
	private bool _comboActive;

	private bool _active;

	private float _timescale;

	private float _nextStepTime;

	public override string ClassName => "Heaven";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public Heaven(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_comboActive = DiceSynergy.HasPartner(player, "World") || DiceSynergy.HasPartner(player, "Izayoi");
		RollTheDice.LogDebug($"[Heaven] Add: combo={_comboActive} selfWorld={(RollTheDice.Instance?.HasDiceActive(player, "World") ?? false)} selfIzayoi={(RollTheDice.Instance?.HasDiceActive(player, "Izayoi") ?? false)}\n");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			bool flag = instance != null && instance.HasDiceActive(player, "World");
			bool flag2 = instance != null && instance.HasDiceActive(player, "Izayoi");
			if (flag | flag2)
			{
				DiceSynergy.AnnounceCombo(player, "超越天堂", "天堂与世界/十六夜融合！获得超越天堂之力！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						if (DiceSynergy.HasPartner(captured, "World"))
						{
							instance.RemoveDiceFromPlayer(captured, "World");
						}
						if (DiceSynergy.HasPartner(captured, "Izayoi"))
						{
							instance.RemoveDiceFromPlayer(captured, "Izayoi");
						}
						instance.RemoveDiceFromPlayer(captured, "Heaven");
						RollTheDice.LogDebug($"[Heaven] combo -> GrantComboDice(BeyondHeaven)={instance.GrantComboDice(captured, "BeyondHeaven")}\n");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "超越天堂", "团队联动！天堂共鸣！");
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		player.PrintToCenterAlert("\ud83c\udf0c 按E键进入天堂！时间加速！");
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		ResetTimescale();
	}

	public override void Reset()
	{
		_players.Clear();
		ResetTimescale();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void ResetTimescale()
	{
		_active = false;
		_timescale = 1f;
		Server.ExecuteCommand("host_timescale 1.0");
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		RollTheDice.LogDebug($"[Heaven] E press: players={_players.Count} active={_active} contains={_players.Contains(player)} use={((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32)}\n");
		if (_players.Count != 0 && !_active && !((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player) && ((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_active = true;
			_timescale = _config.Dices.Heaven.MinTimescale;
			_nextStepTime = Server.CurrentTime + _config.Dices.Heaven.StepInterval;
			Server.ExecuteCommand($"host_timescale {_timescale:F1}");
			RollTheDice.LogDebug($"[Heaven] activated -> host_timescale {_timescale:F1}\n");
			player.PrintToCenterAlert("\ud83c\udf0c 天堂之门开启！");
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83c\udf0c {((CBasePlayerController)player).PlayerName} 打开了天堂之门！时间从{_config.Dices.Heaven.MinTimescale:F1}x加速！");
		}
	}

	public void OnTick()
	{
		if (!_active)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (!(num >= _nextStepTime) || !(_timescale < _config.Dices.Heaven.MaxTimescale))
		{
			return;
		}
		_timescale = Math.Min(_timescale + _config.Dices.Heaven.Step, _config.Dices.Heaven.MaxTimescale);
		_nextStepTime = num + _config.Dices.Heaven.StepInterval;
		Server.ExecuteCommand($"host_timescale {_timescale:F1}");
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (item != null && ((CEntityInstance)item).IsValid)
			{
				item.PrintToCenterAlert($"⏱ {_timescale:F1}倍速");
			}
		}
	}
}
