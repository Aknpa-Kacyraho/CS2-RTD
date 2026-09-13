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

public class Parasite : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<ulong, float> _parasiteDamage = new Dictionary<ulong, float>();

	private readonly Dictionary<ulong, HashSet<ulong>> _markedTargets = new Dictionary<ulong, HashSet<ulong>>();

	public override string ClassName => "Parasite";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerTakeDamagePre";
			num2++;
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

	public Parasite(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_parasiteDamage[((CBasePlayerController)player).SteamID] = 0f;
			_comboActive = DiceSynergy.HasPartner(player, "Plague");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "生化危机", "瘟疫+寄生！传染翻倍+寄生效果翻倍！");
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
		_parasiteDamage.Remove(((CBasePlayerController)player).SteamID);
		_markedTargets.Remove(((CBasePlayerController)player).SteamID);
		DamageBonusManager.Unregister(player, "Parasite");
		SpeedBonusManager.Unregister(player, "Parasite");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "Parasite");
			SpeedBonusManager.Unregister(item, "Parasite");
		}
		_players.Clear();
		_parasiteDamage.Clear();
		_markedTargets.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
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
		ulong steamID = ((CBasePlayerController)val).SteamID;
		if (!_markedTargets.ContainsKey(steamID))
		{
			_markedTargets[steamID] = new HashSet<ulong>();
		}
		_markedTargets[steamID].Add(((CBasePlayerController)val2).SteamID);
		return (HookResult)0;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		ulong steamID = ((CBasePlayerController)userid).SteamID;
		foreach (KeyValuePair<ulong, HashSet<ulong>> item in _markedTargets.ToList())
		{
			var (holderId, hashSet2) = item;
			if (!hashSet2.Contains(steamID))
			{
				continue;
			}
			hashSet2.Remove(steamID);
			if (hashSet2.Count == 0)
			{
				_markedTargets.Remove(holderId);
			}
			CCSPlayerController val2 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == holderId);
			bool flag = (CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid && DiceSynergy.HasPartner(val2, "Plague");
			float valueOrDefault = _parasiteDamage.GetValueOrDefault(holderId, 0f);
			float damageCap = _config.Dices.Parasite.DamageCap;
			float num2 = _config.Dices.Parasite.DamageBonus * (flag ? 2f : 1f);
			float num3 = Math.Min(valueOrDefault + num2, damageCap);
			_parasiteDamage[holderId] = num3;
			DamageBonusManager.RegisterBySteamId(holderId, "Parasite", num3);
			float val = (flag ? 1f : 0.5f);
			float num4 = Math.Min(_config.Dices.Parasite.SpeedBonus * (num3 / Math.Max(_config.Dices.Parasite.DamageBonus, 0.01f)), val);
			SpeedBonusManager.RegisterBySteamId(holderId, "Parasite", num4);
			if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid)
			{
				CCSPlayerPawn val3 = val2.PlayerPawn?.Value;
				if ((CEntityInstance)(object)val3 != (CEntityInstance)null && ((CEntityInstance)val3).IsValid)
				{
					((CBaseEntity)val3).MaxHealth = Math.Max(((CBaseEntity)val3).MaxHealth, ((CBaseEntity)val3).Health + _config.Dices.Parasite.HpRestore);
					((CBaseEntity)val3).Health = ((CBaseEntity)val3).Health + _config.Dices.Parasite.HpRestore;
					Utilities.SetStateChanged((CBaseEntity)(object)val3, "CBaseEntity", "m_iMaxHealth", 0);
					Utilities.SetStateChanged((CBaseEntity)(object)val3, "CBaseEntity", "m_iHealth", 0);
				}
				val2.PrintToCenterAlert($"\ud83e\udda0 寄生！+{(int)(num3 * 100f)}%伤害 +{(int)(num4 * 100f)}%速度！");
			}
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				float effective = SpeedBonusManager.GetEffective(item);
				if (effective > 0f)
				{
					item.PlayerPawn.Value.VelocityModifier = 1f + effective;
					Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}
}
