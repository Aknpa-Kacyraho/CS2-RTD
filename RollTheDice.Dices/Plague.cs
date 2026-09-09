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

public class Plague : DiceBlueprint
{
	public static readonly HashSet<ulong> InfectedPlayers = new HashSet<ulong>();

	private float _lastDamageTime;

	public override string ClassName => "Plague";

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

	public Plague(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "Karma"))
			{
				DiceSynergy.AnnounceCombo(player, "因果循环", "双倍感染率");
			}
			if (DiceSynergy.HasPartner(player, "Parasite"))
			{
				DiceSynergy.AnnounceCombo(player, "生化危机", "瘟疫+寄生！传染翻倍+寄生效果翻倍！");
			}
			InfectedPlayers.Add(((CBasePlayerController)player).SteamID);
			player.PrintToCenterAlert("\ud83e\udda0 你感染了瘟疫！每秒-1HP，攻击可传染！");
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
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid)
		{
			InfectedPlayers.Remove(((CBasePlayerController)player).SteamID);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		InfectedPlayers.Clear();
		_lastDamageTime = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		if (InfectedPlayers.Count == 0)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !InfectedPlayers.Contains(((CBasePlayerController)val).SteamID))
		{
			return (HookResult)0;
		}
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
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val == (CEntityInstance)(object)val2)
		{
			return (HookResult)0;
		}
		if (InfectedPlayers.Contains(((CBasePlayerController)val2).SteamID))
		{
			return (HookResult)0;
		}
		InfectedPlayers.Add(((CBasePlayerController)val2).SteamID);
		val2.PrintToCenterAlert("\ud83e\udda0 你被瘟疫传染了！");
		val2.PrintToChat(" " + _localizer["command.prefix"].Value + _localizer["dice_Plague_infected"].Value);
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_Plague_spread"].Value.Replace("{attacker}", ((CBasePlayerController)val).PlayerName).Replace("{victim}", ((CBasePlayerController)val2).PlayerName));
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (InfectedPlayers.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num - _lastDamageTime < 1f)
		{
			return;
		}
		_lastDamageTime = num;
		int damagePerSecond = _config.Dices.Plague.DamagePerSecond;
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && InfectedPlayers.Contains(((CBasePlayerController)p).SteamID) && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			CCSPlayerPawn value = item.PlayerPawn.Value;
			((CBaseEntity)value).Health -= damagePerSecond;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)value).Health <= 0)
			{
				InfectedPlayers.Remove(((CBasePlayerController)item).SteamID);
				if (!item.IsBot)
				{
					((CBasePlayerPawn)value).CommitSuicide(false, true);
				}
			}
		}
		InfectedPlayers.RemoveWhere(delegate(ulong steamId)
		{
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController pl) => ((CBasePlayerController)pl).SteamID == steamId);
			return (CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || (CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid || ((CBaseEntity)val.PlayerPawn.Value).LifeState != 0;
		});
	}
}
