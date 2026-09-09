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

public class Empress : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _lastKnownMoney = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _totalEarned = new Dictionary<CCSPlayerController, int>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Empress";

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

	public Empress(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Emperor");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "王权永恒", "王权永恒联动生效！");
			}
			if (player.InGameMoneyServices != null)
			{
				_lastKnownMoney[player] = player.InGameMoneyServices.Account;
				_totalEarned[player] = 0;
			}
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
		_lastKnownMoney.Remove(player);
		_totalEarned.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_lastKnownMoney.Clear();
		_totalEarned.Clear();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || item.InGameMoneyServices == null)
				{
					continue;
				}
				if (!_lastKnownMoney.TryGetValue(item, out var value))
				{
					_lastKnownMoney[item] = item.InGameMoneyServices.Account;
					continue;
				}
				int account = item.InGameMoneyServices.Account;
				if (account > value)
				{
					int num = account - value;
					item.InGameMoneyServices.Account += num;
					Utilities.SetStateChanged((CBaseEntity)(object)item, "CCSPlayerController", "m_pInGameMoneyServices", 0);
					if (!_totalEarned.ContainsKey(item))
					{
						_totalEarned[item] = 0;
					}
				_totalEarned[item] += num * 2;
				int num2 = (DiceSynergy.HasPartner(item, "Emperor") ? 600 : 1200);
					if (_totalEarned[item] >= num2)
					{
						_totalEarned[item] -= num2;
						TryReviveTeammate(item);
					}
				}
				_lastKnownMoney[item] = item.InGameMoneyServices.Account;
			}
			catch
			{
			}
		}
	}

	private void TryReviveTeammate(CCSPlayerController empress)
	{
		List<CCSPlayerController> list = (from _ in Utilities.GetPlayers()
			where ((CEntityInstance)_).IsValid && !((CBasePlayerController)_).IsHLTV && (CEntityInstance)(object)_ != (CEntityInstance)(object)empress && ((CBaseEntity)_).TeamNum == ((CBaseEntity)empress).TeamNum && (CEntityInstance)(object)_.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)_.PlayerPawn.Value).IsValid && ((CBaseEntity)_.PlayerPawn.Value).LifeState != 0
			orderby _random.Next()
			select _).ToList();
		if (list.Count == 0)
		{
			return;
		}
		CCSPlayerController val = list.First();
		CCSPlayerController capturedDead = val;
		CCSPlayerController capturedEmpress = empress;
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
						//IL_006f: Unknown result type (might be due to invalid IL or missing references)
						//IL_0075: Invalid comparison between Unknown and I4
						//IL_0096: Unknown result type (might be due to invalid IL or missing references)
						//IL_009b: Unknown result type (might be due to invalid IL or missing references)
						//IL_00a4: Expected O, but got Unknown
						CCSPlayerController obj2 = capturedDead;
						if (!((CEntityInstance)(object)((obj2 == null) ? null : obj2.PlayerPawn?.Value) == (CEntityInstance)null) && ((CBaseEntity)capturedDead.PlayerPawn.Value).LifeState == 0)
						{
							capturedDead.RemoveWeapons();
							capturedDead.GiveNamedItem("weapon_knife");
							if ((int)capturedDead.Team == 3)
							{
								CCSPlayer_ItemServices val2 = new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)capturedDead.PlayerPawn.Value).ItemServices).Handle);
								val2.HasDefuser = true;
								CCSPlayer_ItemServices val3 = val2;
							}
							capturedDead.GiveNamedItem("weapon_ak47");
							capturedDead.GiveNamedItem("weapon_deagle");
							capturedDead.PlayerPawn.Value.ArmorValue = 100;
							capturedDead.PrintToCenterAlert("\ud83d\udc51 女皇复活了你!");
							CCSPlayerController obj3 = capturedEmpress;
							if (obj3 != null)
							{
								obj3.PrintToCenterAlert("\ud83d\udc51 你复活了 " + ((CBasePlayerController)capturedDead).PlayerName + "!");
							}
							string value = _localizer["command.prefix"].Value;
							Server.PrintToChatAll(" " + value + _localizer["dice_Empress_revive"].Value.Replace("{empressName}", ((CBasePlayerController)capturedEmpress).PlayerName).Replace("{deadName}", ((CBasePlayerController)capturedDead).PlayerName));
						}
					});
				}
			});
		});
	}
}
