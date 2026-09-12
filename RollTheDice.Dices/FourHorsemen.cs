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

public class FourHorsemen : DiceBlueprint
{
	private static readonly Dictionary<ulong, string> _assignments = new Dictionary<ulong, string>();

	private static readonly HashSet<ulong> _plagueInfected = new HashSet<ulong>();

	private static float _lastPlagueTick;

	private static bool _active;

	private static readonly string[] _horsemenList = new string[4] { "war", "plague", "famine", "death" };

	private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "FourHorsemen";

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
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public FourHorsemen(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (!_active)
			{
				_active = true;
				AssignHorsemen();
				AnnounceAll();
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
	}

	public override void Reset()
	{
		foreach (KeyValuePair<ulong, string> assignment in _assignments)
		{
			if (assignment.Value == "war")
			{
				DamageBonusManager.UnregisterBySteamId(assignment.Key, "FourHorsemenWar");
			}
		}
		_players.Clear();
		_assignments.Clear();
		_plagueInfected.Clear();
		_active = false;
		_lastPlagueTick = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	private void AssignHorsemen()
	{
		_assignments.Clear();
		_plagueInfected.Clear();
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && !p.IsBot && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p).ToList();
		if (list.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in list)
		{
			string text = _horsemenList[_random.Next(_horsemenList.Length)];
			_assignments[((CBasePlayerController)item).SteamID] = text;
			if (text == "war")
			{
				DamageBonusManager.RegisterBySteamId(((CBasePlayerController)item).SteamID, "FourHorsemenWar", _config.Dices.FourHorsemen.WarDamageBonus);
			}
			else if (text == "plague")
			{
				_plagueInfected.Add(((CBasePlayerController)item).SteamID);
			}
		}
		_lastPlagueTick = Server.CurrentTime;
	}

	private void AnnounceAll()
	{
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV
			select p)
		{
			if (_assignments.TryGetValue(((CBasePlayerController)item).SteamID, out string value))
			{
				if (1 == 0)
				{
				}
				string text = value switch
				{
					"war" => _localizer["dice_FourHorsemen_war_name"].Value, 
					"plague" => _localizer["dice_FourHorsemen_plague_name"].Value, 
					"famine" => _localizer["dice_FourHorsemen_famine_name"].Value, 
					"death" => _localizer["dice_FourHorsemen_death_name"].Value, 
					_ => "???", 
				};
				if (1 == 0)
				{
				}
				string text2 = text;
				item.PrintToCenterAlert("⚖\ufe0f 四骑士降临！你是：" + text2);
				item.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_FourHorsemen_assigned"].Value.Replace("{horseman}", text2));
			}
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_FourHorsemen_broadcast"].Value);
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		if (!_active)
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		CCSPlayerPawn obj3 = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj4;
		if (obj3 == null)
		{
			obj4 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj3).Controller;
			if (controller2 == null)
			{
				obj4 = null;
			}
			else
			{
				CBasePlayerController value3 = controller2.Value;
				obj4 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj4;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid)
		{
			return (HookResult)0;
		}
		if (_assignments.TryGetValue(((CBasePlayerController)val2).SteamID, out string value4) && value4 == "war")
		{
			info.Damage *= 1f + _config.Dices.FourHorsemen.WarDamageTaken;
		}
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || (CEntityInstance)(object)val == (CEntityInstance)(object)val2)
		{
			return (HookResult)0;
		}
		if (_assignments.TryGetValue(((CBasePlayerController)val).SteamID, out string value5) && value5 == "plague" && !_plagueInfected.Contains(((CBasePlayerController)val2).SteamID))
		{
			_plagueInfected.Add(((CBasePlayerController)val2).SteamID);
			val2.PrintToCenterAlert("\ud83e\udda0 你被" + _localizer["dice_FourHorsemen_plague_name"].Value + "传染了！");
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_FourHorsemen_plague_spread"].Value.Replace("{attacker}", ((CBasePlayerController)val).PlayerName).Replace("{victim}", ((CBasePlayerController)val2).PlayerName));
		}
		if (_assignments.TryGetValue(((CBasePlayerController)val2).SteamID, out string value6) && value6 == "famine")
		{
			CHandle<CCSPlayerPawn> playerPawn = val.PlayerPawn;
			object obj5;
			if (playerPawn == null)
			{
				obj5 = null;
			}
			else
			{
				CCSPlayerPawn value7 = playerPawn.Value;
				if (value7 == null)
				{
					obj5 = null;
				}
				else
				{
					CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value7).WeaponServices;
					obj5 = ((weaponServices == null) ? null : weaponServices.ActiveWeapon?.Value);
				}
			}
			if ((CEntityInstance)obj5 != (CEntityInstance)null)
			{
				CBasePlayerWeapon value8 = ((CBasePlayerPawn)val.PlayerPawn.Value).WeaponServices.ActiveWeapon.Value;
				string designerName = ((CEntityInstance)value8).DesignerName;
				if (designerName != null && !designerName.Contains("knife") && !designerName.Contains("c4") && !designerName.Contains("taser"))
				{
					value8.Clip1 = 0;
					val.PrintToCenterAlert("\ud83c\udf5e 饥荒！弹夹清零！");
				}
			}
		}
		return (HookResult)0;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		if (!_active)
		{
			return (HookResult)0;
		}
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		bool isDeath = _assignments.TryGetValue(((CBasePlayerController)userid).SteamID, out string value) && value == "death";
		CCSPlayerController val = (isDeath ? @event.Attacker : null);
		ulong killerSid = (((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && !((CBasePlayerController)val).IsHLTV) ? ((CBasePlayerController)val).SteamID : 0);
		ulong deadSid = ((CBasePlayerController)userid).SteamID;
		Server.NextFrame((Action)delegate
		{
			if (isDeath && killerSid != 0)
			{
				CCSPlayerController val2 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBasePlayerController)p).SteamID == killerSid);
				if ((CEntityInstance)(object)((val2 == null) ? null : val2.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)val2.PlayerPawn.Value).IsValid && ((CBaseEntity)val2.PlayerPawn.Value).LifeState == 0)
				{
					int killerDamage = (int)float.Round(_config.Dices.FourHorsemen.DeathKillerDamage);
					((CBaseEntity)val2.PlayerPawn.Value).Health -= killerDamage;
					Utilities.SetStateChanged((CBaseEntity)(object)val2.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)val2.PlayerPawn.Value).Health <= 0)
					{
						if (!val2.IsBot && !((CBasePlayerController)val2).IsHLTV)
						{
							((CBasePlayerPawn)val2.PlayerPawn.Value).CommitSuicide(false, true);
						}
						else
						{
							try
							{
								((CBasePlayerPawn)val2.PlayerPawn.Value).CommitSuicide(false, true);
							}
							catch
							{
								((CBaseEntity)val2.PlayerPawn.Value).Health = 0;
								Utilities.SetStateChanged((CBaseEntity)(object)val2.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
							}
						}
					}
					val2.PrintToCenterAlert("☠\ufe0f 死亡骑士的反噬！");
				}
			}
			_assignments.Remove(deadSid);
			_plagueInfected.Remove(deadSid);
			DamageBonusManager.UnregisterBySteamId(deadSid, "FourHorsemenWar");
		});
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (!_active)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num - _lastPlagueTick < 1f)
		{
			return;
		}
		_lastPlagueTick = num;
		int plagueDamagePerSecond = _config.Dices.FourHorsemen.PlagueDamagePerSecond;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && _plagueInfected.Contains(((CBasePlayerController)p).SteamID) && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			CCSPlayerPawn value = item.PlayerPawn.Value;
			((CBaseEntity)value).Health -= plagueDamagePerSecond;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)value).Health > 0)
			{
				continue;
			}
			_plagueInfected.Remove(((CBasePlayerController)item).SteamID);
			_assignments.Remove(((CBasePlayerController)item).SteamID);
			if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
			{
				((CBasePlayerPawn)value).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)value).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)value).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			}
		}
		_plagueInfected.RemoveWhere(delegate(ulong steamId)
		{
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController pl) => ((CBasePlayerController)pl).SteamID == steamId);
			return (CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || (CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid || ((CBaseEntity)val.PlayerPawn.Value).LifeState != 0;
		});
		List<ulong> list = (from kvp in _assignments.Where<KeyValuePair<ulong, string>>(delegate(KeyValuePair<ulong, string> kvp)
			{
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController pl) => ((CBasePlayerController)pl).SteamID == kvp.Key);
				return (CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || (CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid || ((CBaseEntity)val.PlayerPawn.Value).LifeState != 0;
			})
			select kvp.Key).ToList();
		foreach (ulong item2 in list)
		{
			_assignments.Remove(item2);
		}
	}
}
