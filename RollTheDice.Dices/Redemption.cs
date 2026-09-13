using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Redemption : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, List<CCSPlayerController>> _killedBy = new Dictionary<CCSPlayerController, List<CCSPlayerController>>();

	public override string ClassName => "Redemption";

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

	public Redemption(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			float damageBonus = _config.Dices.Redemption.DamageBonus;
			DamageBonusManager.Register(player, "Redemption", damageBonus);
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"bonus",
					(damageBonus * 100f).ToString("F0")
				}
			});
			player.PrintToCenterAlert($"✝ 救赎！伤害+{damageBonus * 100f:F0}%，死后复活你杀过的人！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageBonusManager.Unregister(player, "Redemption");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "Redemption");
		}
		_players.Clear();
		_killedBy.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)userid)
		{
			return (HookResult)0;
		}
		if (!_killedBy.ContainsKey(attacker))
		{
			_killedBy[attacker] = new List<CCSPlayerController>();
		}
		_killedBy[attacker].Add(userid);
		if (_players.Contains(userid) && _killedBy.TryGetValue(userid, out List<CCSPlayerController> value))
		{
			foreach (CCSPlayerController item in value)
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
				{
					continue;
				}
				CCSPlayerController capturedDead = item;
				Server.NextFrame((Action)delegate
				{
					Server.NextFrame((Action)delegate
					{
						CCSPlayerController obj = capturedDead;
						if (!((CEntityInstance)(object)((obj == null) ? null : obj.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState != 0)
						{
							capturedDead.Respawn();
							Server.NextFrame((Action)delegate
							{
								CCSPlayerController obj2 = capturedDead;
								if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState == 0)
								{
									capturedDead.RemoveWeapons();
									capturedDead.GiveNamedItem("weapon_knife");
									capturedDead.PlayerPawn.Value.ArmorValue = 100;
									Utilities.SetStateChanged((CBaseEntity)(object)capturedDead.PlayerPawn.Value, "CCSPlayerPawn", "m_ArmorValue", 0);
									capturedDead.PrintToCenterAlert("✝ 赎罪！敌人复活了你");
								}
							});
						}
					});
				});
			}
			userid.PrintToCenterAlert("✝ 赎罪！你杀死的人复活了");
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Redemption_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)userid).PlayerName));
			_killedBy.Remove(userid);
		}
		return (HookResult)0;
	}
}
