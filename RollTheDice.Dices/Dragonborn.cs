using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Dragonborn : DiceBlueprint
{
	private bool _comboActive;

	private bool _hasDragonSoul;

	private bool _hasRespawn;

	private readonly Dictionary<CCSPlayerController, int> _killCount = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, bool> _transformed = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "Dragonborn";

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
			span[index] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public Dragonborn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_hasDragonSoul = DiceSynergy.HasPartner(player, "DragonSoul");
			_hasRespawn = DiceSynergy.HasPartner(player, "Respawn");
			_comboActive = _hasDragonSoul || _hasRespawn;
			if (_hasRespawn)
			{
				DiceSynergy.AnnounceCombo(player, "浴火重生", "浴火重生联动生效！重生后200HP 200护甲！");
			}
			if (_hasDragonSoul)
			{
				DiceSynergy.AnnounceCombo(player, "巨龙共鸣", "巨龙之魂+龙裔！化龙时获得龙魂之力，HP+150！");
			}
			_killCount[player] = 0;
			_transformed[player] = false;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udc09 龙裔！击杀2人后按E化身为龙！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_killCount.Remove(player);
		_transformed.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_killCount.Clear();
		_transformed.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)attacker == (CEntityInstance)(object)@event.Userid)
		{
			return (HookResult)0;
		}
		if (_transformed.TryGetValue(attacker, out var value) & value)
		{
			return (HookResult)0;
		}
		int num = (_killCount.TryGetValue(attacker, out var value2) ? value2 : 0);
		num++;
		_killCount[attacker] = num;
		int killsRequired = _config.Dices.Dragonborn.KillsRequired;
		if (num >= killsRequired)
		{
			attacker.PrintToCenterAlert("\ud83d\udc09 龙之血已满！按 E 化身为龙！");
		}
		else
		{
			attacker.PrintToCenterAlert($"\ud83d\udc09 龙之血 ({num}/{killsRequired})");
		}
		return (HookResult)0;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (_transformed.TryGetValue(player, out var value) & value))
		{
			return;
		}
		int num = (_killCount.TryGetValue(player, out var value2) ? value2 : 0);
		int killsRequired = _config.Dices.Dragonborn.KillsRequired;
		if (num < killsRequired || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_transformed[player] = true;
		CCSPlayerPawn value3 = player.PlayerPawn.Value;
		bool flag = ((CBaseEntity)value3).LifeState != 0;
		CCSPlayerController captured = player;
		if (DiceSynergy.HasPartner(player, "DragonSoul"))
		{
			RollTheDice? instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(captured, "DragonSoul"))
			{
				RollTheDice instance2 = RollTheDice.Instance;
				if (instance2 != null)
				{
					string dragonType = ((Random.Shared.Next(2) == 0) ? "IceDragon" : "FireDragon");
					Server.NextFrame((Action)delegate
					{
						if (!((CEntityInstance)(object)captured == (CEntityInstance)null) && ((CEntityInstance)captured).IsValid)
						{
							instance2.RemoveDiceFromPlayer(captured, "Dragonborn");
							instance2.RemoveDiceFromPlayer(captured, "DragonSoul");
							Server.NextFrame((Action)delegate
							{
								if (!((CEntityInstance)(object)captured == (CEntityInstance)null) && ((CEntityInstance)captured).IsValid)
								{
									if ((CEntityInstance)(object)captured.PlayerPawn?.Value != (CEntityInstance)null && ((CBaseEntity)captured.PlayerPawn.Value).LifeState != 0)
									{
										captured.Respawn();
										Server.NextFrame((Action)delegate
										{
											instance2.ForceDiceForPlayer(captured, dragonType);
											Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc09\ud83d\udd25 {((CBasePlayerController)captured).PlayerName} 龙魂共鸣，化身为{((dragonType == "IceDragon") ? "冰巨龙" : "火巨龙")}！");
										});
									}
									else
									{
										instance2.ForceDiceForPlayer(captured, dragonType);
										Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc09\ud83d\udd25 {((CBasePlayerController)captured).PlayerName} 龙魂共鸣，化身为{((dragonType == "IceDragon") ? "冰巨龙" : "火巨龙")}！");
									}
								}
							});
						}
					});
					captured.PrintToCenterAlert("\ud83d\udc09 龙魂共鸣！化身为龙！");
					return;
				}
			}
		}
		int hp = _config.Dices.Dragonborn.DragonHP;
		int armor = _config.Dices.Dragonborn.DragonArmor;
		if (DiceSynergy.HasPartner(player, "DragonSoul"))
		{
			hp += 150;
			armor += 150;
		}
		if (flag)
		{
			Server.NextFrame((Action)delegate
			{
				Server.NextFrame((Action)delegate
				{
					CCSPlayerController obj = captured;
					if (!((CEntityInstance)(object)((obj == null) ? null : obj.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)captured.PlayerPawn.Value).LifeState != 0)
					{
						captured.Respawn();
						Server.NextFrame((Action)delegate
						{
							CCSPlayerController obj2 = captured;
							if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)captured.PlayerPawn.Value).LifeState == 0)
							{
								CCSPlayerPawn value4 = captured.PlayerPawn.Value;
								((CBaseEntity)value4).MaxHealth = hp;
								((CBaseEntity)value4).Health = hp;
								value4.ArmorValue = armor;
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CBaseEntity", "m_iMaxHealth", 0);
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CBaseEntity", "m_iHealth", 0);
								Utilities.SetStateChanged((CBaseEntity)(object)value4, "CCSPlayerPawn", "m_ArmorValue", 0);
							}
						});
					}
				});
			});
		}
		else
		{
			((CBaseEntity)value3).MaxHealth = hp;
			((CBaseEntity)value3).Health = hp;
			value3.ArmorValue = armor;
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CCSPlayerPawn", "m_ArmorValue", 0);
		}
		captured.PrintToCenterAlert(DiceSynergy.HasPartner(player, "DragonSoul") ? "\ud83d\udc09 龙魂共鸣！450HP 450护甲！" : "\ud83d\udc09 化身为龙！300HP 300护甲！");
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Dragonborn_transform"].Value.Replace("{playerName}", ((CBasePlayerController)captured).PlayerName));
	}
}
