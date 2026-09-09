using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Traitor : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Traitor";

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

	public Traitor(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
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
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid)
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)userid).TeamNum != ((CBaseEntity)attacker).TeamNum)
		{
			return (HookResult)0;
		}
		float delay = _config.Dices.Traitor.DeathDelay;
		string attName = ((CBasePlayerController)attacker).PlayerName;
		string vicName = ((CBasePlayerController)userid).PlayerName;
		int attackerTeam = ((CBaseEntity)attacker).TeamNum;
		Server.NextFrame((Action)delegate
		{
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != attackerTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
				select p).ToList();
			if (list.Count != 0)
			{
				CCSPlayerController target = list[_random.Next(list.Count)];
				string enemyName = ((CBasePlayerController)target).PlayerName;
				new Timer(delay, (Action)delegate
				{
					if (!((CEntityInstance)(object)target == (CEntityInstance)null) && ((CEntityInstance)target).IsValid && !((CEntityInstance)(object)target.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)target.PlayerPawn.Value).IsValid && ((CBaseEntity)target.PlayerPawn.Value).LifeState == 0)
					{
						if (!target.IsBot && !((CBasePlayerController)target).IsHLTV)
						{
							((CBasePlayerPawn)target.PlayerPawn.Value).CommitSuicide(false, true);
						}
						else
						{
							try
							{
								((CBasePlayerPawn)target.PlayerPawn.Value).CommitSuicide(false, true);
							}
							catch
							{
								((CBaseEntity)target.PlayerPawn.Value).Health = 0;
								Utilities.SetStateChanged((CBaseEntity)(object)target.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
							}
						}
						target.PrintToCenterAlert("\ud83d\udd2a 叛徒出卖了你!");
						Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Traitor_broadcast"].Value.Replace("{attacker}", attName).Replace("{victim}", vicName).Replace("{enemy}", enemyName));
					}
				}, (TimerFlags?)null);
			}
		});
		return (HookResult)0;
	}
}
