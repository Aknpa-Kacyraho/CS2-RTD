using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Rewind : DiceBlueprint
{
	private bool _comboActive;

	private readonly HashSet<CCSPlayerController> _used = new HashSet<CCSPlayerController>();

	private readonly HashSet<CCSPlayerController> _pending = new HashSet<CCSPlayerController>();

	public override string ClassName => "Rewind";

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

	public Rewind(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Countdown");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "时空主宰", "时空主宰联动生效！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("⏪ 回溯求源就绪！按E触发，15秒后全服回溯！被击杀则失效！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_used.Clear();
		_pending.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		if (!_pending.Contains(userid))
		{
			return (HookResult)0;
		}
		_pending.Remove(userid);
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + ((CBasePlayerController)userid).PlayerName + " 被杀，回溯已取消！");
		return (HookResult)0;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || _used.Contains(player) || _pending.Contains(player) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		string roundBackupFile = RollTheDice.GetRoundBackupFile();
		if (string.IsNullOrEmpty(roundBackupFile))
		{
			player.PrintToChat(" " + _localizer["command.prefix"].Value + "回溯失败：回合备份文件尚未就绪！");
			return;
		}
		_used.Add(player);
		_pending.Add(player);
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Rewind_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏪ {((CBasePlayerController)player).PlayerName} 发起了回溯！15秒后回滚到回合初！击杀{((CBasePlayerController)player).PlayerName}可阻止！");
		string capturedFileName = roundBackupFile;
		int[] array = new int[5] { 10, 5, 3, 2, 1 };
		int[] array2 = array;
		foreach (int cd in array2)
		{
			float num = 15f - (float)cd;
			new Timer(num, (Action)delegate
			{
				if (_pending.Contains(player))
				{
					Server.PrintToChatAll($" {_localizer["command.prefix"].Value}⏪ 回溯倒计时 {cd} 秒...");
				}
			}, (TimerFlags?)null);
		}
		new Timer(15f, (Action)delegate
		{
			if (_pending.Contains(player))
			{
				_pending.Remove(player);
				if ((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
				{
					Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + ((CBasePlayerController)player).PlayerName + " 已死亡，回溯取消！");
				}
				else
				{
					Server.ExecuteCommand("mp_backup_restore_load_file " + capturedFileName);
				}
			}
		}, (TimerFlags?)null);
	}
}
