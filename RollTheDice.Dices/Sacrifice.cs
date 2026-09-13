using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Sacrifice : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _teammateDeathCount = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _speedBonusEndTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Sacrifice";

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

	public Sacrifice(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_teammateDeathCount[player] = 0;
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
		_teammateDeathCount.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _speedBonusEndTime.Keys.ToList())
		{
			SpeedBonusManager.Unregister(item, "Sacrifice");
		}
		_players.Clear();
		_teammateDeathCount.Clear();
		_speedBonusEndTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if ((CEntityInstance)(object)holder == (CEntityInstance)null || !((CEntityInstance)holder).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)(object)holder || ((CBaseEntity)userid).TeamNum != ((CBaseEntity)holder).TeamNum)
			{
				continue;
			}
			int num = ((!_teammateDeathCount.TryGetValue(holder, out var value)) ? 1 : (value + 1));
			_teammateDeathCount[holder] = num;
			int requiredTeammateDeaths = _config.Dices.Sacrifice.RequiredTeammateDeaths;
			holder.PrintToChat(_localizer["command.prefix"].Value + _localizer["dice_Sacrifice_progress"].Value.Replace("{current}", num.ToString()).Replace("{required}", requiredTeammateDeaths.ToString()));
			if ((CEntityInstance)(object)holder.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)holder.PlayerPawn.Value).IsValid || ((CBaseEntity)holder.PlayerPawn.Value).LifeState != 0 || num < requiredTeammateDeaths)
			{
				continue;
			}
			_teammateDeathCount[holder] = 0;
			string playerName = ((CBasePlayerController)holder).PlayerName;
			int holderTeam = ((CBaseEntity)holder).TeamNum;
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum == holderTeam && (CEntityInstance)(object)p != (CEntityInstance)(object)holder && ((CEntityInstance)(object)p.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)p.PlayerPawn.Value).IsValid || ((CBaseEntity)p.PlayerPawn.Value).LifeState != 0)
				select p).ToList();
			if (list.Count == 0)
			{
				holder.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Sacrifice_no_target"].Value);
				continue;
			}
			CCSPlayerController reviveTarget = list[Random.Shared.Next(list.Count)];
			string playerName2 = ((CBasePlayerController)reviveTarget).PlayerName;
			Server.NextFrame((Action)delegate
			{
				Server.NextFrame((Action)delegate
				{
					CCSPlayerController obj = reviveTarget;
					if (!((CEntityInstance)(object)((obj == null) ? null : obj.PlayerPawn?.Value) != (CEntityInstance)null) || ((CBaseEntity)reviveTarget.PlayerPawn.Value).LifeState != 0)
					{
						reviveTarget.Respawn();
						Server.NextFrame((Action)delegate
						{
							//IL_0192: Unknown result type (might be due to invalid IL or missing references)
							//IL_0198: Invalid comparison between Unknown and I4
							//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
							//IL_01af: Unknown result type (might be due to invalid IL or missing references)
							//IL_01b8: Expected O, but got Unknown
							CCSPlayerController obj2 = reviveTarget;
							if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)reviveTarget.PlayerPawn.Value).LifeState == 0)
							{
								CCSPlayerPawn value2 = reviveTarget.PlayerPawn.Value;
								reviveTarget.RemoveWeapons();
								reviveTarget.GiveNamedItem("weapon_knife");
								reviveTarget.GiveNamedItem("weapon_ak47");
								value2.ArmorValue = _config.Dices.Sacrifice.ReviveArmor;
								Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_ArmorValue", 0);
								((CBaseEntity)value2).Health = _config.Dices.Sacrifice.ReviveHP;
								((CBaseEntity)value2).MaxHealth = _config.Dices.Sacrifice.ReviveHP;
								Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
								Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
								SpeedBonusManager.Register(reviveTarget, "Sacrifice", _config.Dices.Sacrifice.SpeedMultiplier - 1f);
								value2.VelocityModifier = 1f + SpeedBonusManager.GetEffective(reviveTarget, 100f);
								Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
								_speedBonusEndTime[reviveTarget] = float.MaxValue;
								if ((int)reviveTarget.Team == 3)
								{
									CCSPlayer_ItemServices val = new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value2).ItemServices).Handle);
									val.HasDefuser = true;
									CCSPlayer_ItemServices val2 = val;
								}
								reviveTarget.PrintToCenterAlert("\ud83d\udc80 你被献祭复活了！300HP 300甲 AK47 速度×2");
							}
						});
					}
				});
			});
			new Timer(0.5f, (Action)delegate
			{
				if ((CEntityInstance)(object)holder != (CEntityInstance)null && ((CEntityInstance)holder).IsValid && (CEntityInstance)(object)holder.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)holder.PlayerPawn.Value).IsValid && ((CBaseEntity)holder.PlayerPawn.Value).LifeState == 0 && !holder.IsBot && !((CBasePlayerController)holder).IsHLTV)
				{
					((CBasePlayerPawn)holder.PlayerPawn.Value).CommitSuicide(false, true);
				}
			}, (TimerFlags?)null);
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Sacrifice_broadcast"].Value.Replace("{holder}", playerName).Replace("{revived}", playerName2));
			break;
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_speedBonusEndTime.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<CCSPlayerController, float> item in _speedBonusEndTime.ToList())
		{
			CCSPlayerController key = item.Key;
			if ((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)key.PlayerPawn.Value).IsValid || ((CBaseEntity)key.PlayerPawn.Value).LifeState != 0)
			{
				if (key != null)
				{
					SpeedBonusManager.Unregister(key, "Sacrifice");
				}
				_speedBonusEndTime.Remove(key);
			}
			else
			{
				float num = 1f + SpeedBonusManager.GetEffective(key, 100f);
				if (key.PlayerPawn.Value.VelocityModifier != num)
				{
					key.PlayerPawn.Value.VelocityModifier = num;
					Utilities.SetStateChanged((CBaseEntity)(object)key.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}
}
