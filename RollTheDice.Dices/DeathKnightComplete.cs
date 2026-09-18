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

public class DeathKnightComplete : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalArmor = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _lastHealTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, int> _startMaxHP = new Dictionary<CCSPlayerController, int>();

	public static readonly HashSet<ulong> DeniedNextRound = new HashSet<ulong>();

	// 上一回合被斩杀而"下回合禁骰"的名单：在回合开始时从 DeniedNextRound 提升为生效，
	// 本回合滚骰用它判定，回合结束时清空。之前唯一的一份名单在回合开始/结束被提前清掉，导致禁骰从未生效。
	private static readonly HashSet<ulong> _deniedThisRound = new HashSet<ulong>();

	public static bool IsDeniedThisRound(ulong steamId)
	{
		return _deniedThisRound.Contains(steamId);
	}

	/// <summary>回合开始时调用：把上一回合记录的禁骰名单提升为本回合生效。</summary>
	public static void PromoteDenied()
	{
		_deniedThisRound.Clear();
		foreach (ulong id in DeniedNextRound)
		{
			_deniedThisRound.Add(id);
		}
		DeniedNextRound.Clear();
	}

	/// <summary>回合结束时调用：本回合的禁骰失效。</summary>
	public static void ClearDeniedThisRound()
	{
		_deniedThisRound.Clear();
	}

	public override string ClassName => "DeathKnightComplete";


	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
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

	public DeathKnightComplete(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			_originalArmor[player] = value.ArmorValue;
			((CBaseEntity)value).MaxHealth = _config.Dices.DeathKnightComplete.BonusHP;
			((CBaseEntity)value).Health = _config.Dices.DeathKnightComplete.BonusHP;
			value.ArmorValue = _config.Dices.DeathKnightComplete.BonusArmor;
			_startMaxHP[player] = ((CBaseEntity)value).MaxHealth;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			_players.Add(player);
			_lastHealTime[player] = 0f;
			DamageReductionManager.Register(player, "DeathKnightComplete", _config.Dices.DeathKnightComplete.InitialDamageReduction);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageReductionManager.Unregister(player, "DeathKnightComplete");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
			if (_originalArmor.TryGetValue(player, out var value3))
			{
				value.ArmorValue = Math.Min(value.ArmorValue, value3);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				_originalArmor.Remove(player);
			}
		}
		_players.Remove(player);
		_lastHealTime.Remove(player);
		_startMaxHP.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
		_originalArmor.Clear();
		_lastHealTime.Clear();
		_startMaxHP.Clear();
		// 注意：这里不能清 DeniedNextRound。它在回合 N 记录、要在回合 N+1 开始时才提升生效，
		// 而 Reset 在回合 N 结束就会被调用——提前清掉会让禁骰永远不生效。只在 Destroy 里清。
	}

	public override void Destroy()
	{
		Reset();
		DeniedNextRound.Clear();
		_deniedThisRound.Clear();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			bool flag = false;
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
			object obj;
			if (weaponServices == null)
			{
				obj = null;
			}
			else
			{
				CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
				if (activeWeapon == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value = activeWeapon.Value;
					obj = ((value != null) ? ((CEntityInstance)value).DesignerName : null);
				}
			}
			string text = (string)obj;
			if (text != null && text.Contains("knife"))
			{
				flag = true;
			}
			if (flag && _lastHealTime.TryGetValue(item, out var value2) && num - value2 >= 1f)
			{
				_lastHealTime[item] = num;
				CCSPlayerPawn value3 = item.PlayerPawn.Value;
				((CBaseEntity)value3).Health = Math.Min(((CBaseEntity)value3).Health + _config.Dices.DeathKnightComplete.KnifeHealPerSec, ((CBaseEntity)value3).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			}
			if (!_startMaxHP.TryGetValue(item, out var value4) || value4 <= 0)
			{
				continue;
			}
			int num4 = value4 - ((CBaseEntity)item.PlayerPawn.Value).Health;
			if (num4 < 0)
			{
				num4 = 0;
			}
			float initialDamageReduction = _config.Dices.DeathKnightComplete.InitialDamageReduction;
			float num5 = (float)num4 / (float)value4;
			float num6 = initialDamageReduction + num5 * (1f - initialDamageReduction);
			float maxDamageReduction = _config.Dices.DeathKnightComplete.MaxDamageReduction;
			if (num6 > maxDamageReduction)
			{
				num6 = maxDamageReduction;
			}
			DamageReductionManager.Register(item, "DeathKnightComplete", num6);
		}
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (!_players.Contains(attacker))
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = attacker.PlayerPawn;
		object obj;
		if (playerPawn == null)
		{
			obj = null;
		}
		else
		{
			CCSPlayerPawn value = playerPawn.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
				if (weaponServices == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
					if (activeWeapon == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerWeapon value2 = activeWeapon.Value;
						obj = ((value2 != null) ? ((CEntityInstance)value2).DesignerName : null);
					}
				}
			}
		}
		if (((CBaseEntity)attacker).TeamNum != ((CBaseEntity)userid).TeamNum)
		{
			DeniedNextRound.Add(((CBasePlayerController)userid).SteamID);
			userid.PrintToCenterAlert("☠ 被死亡骑士斩杀！下回合无法获得骰子！");
		}
		return (HookResult)0;
	}
}
