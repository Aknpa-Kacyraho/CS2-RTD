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

public class ReverseCausality : DiceBlueprint
{
	private struct PendingDamage
	{
		public float ApplyTime;

		public int Damage;
	}

	private static readonly Dictionary<ulong, List<PendingDamage>> _delayedDamage = new Dictionary<ulong, List<PendingDamage>>();

	private static readonly Dictionary<ulong, float> _doubleDamageUntil = new Dictionary<ulong, float>();

	public override string ClassName => "ReverseCausality";

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

	public ReverseCausality(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (!_delayedDamage.ContainsKey(((CBasePlayerController)player).SteamID))
			{
				_delayedDamage[((CBasePlayerController)player).SteamID] = new List<PendingDamage>();
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
		ApplyAllPending(((CBasePlayerController)player).SteamID);
		DamageBonusManager.UnregisterBySteamId(((CBasePlayerController)player).SteamID, "ReverseCausality");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			ApplyAllPending(((CBasePlayerController)item).SteamID);
			DamageBonusManager.UnregisterBySteamId(((CBasePlayerController)item).SteamID, "ReverseCausality");
		}
		_players.Clear();
		_delayedDamage.Clear();
		_doubleDamageUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void ApplyAllPending(ulong steamId)
	{
		if (!_delayedDamage.TryGetValue(steamId, out List<PendingDamage> value) || value.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (PendingDamage item in value)
		{
			num += item.Damage;
		}
		value.Clear();
		CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == steamId);
		if (!((CEntityInstance)(object)((val == null) ? null : val.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)val.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value2 = val.PlayerPawn.Value;
			((CBaseEntity)value2).Health -= num;
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			val.PrintToCenterAlert($"⏳ 因果结算！-{num}HP！");
			if (((CBaseEntity)value2).Health <= 0 && !val.IsBot)
			{
				((CBasePlayerPawn)value2).CommitSuicide(false, true);
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		if (info.Damage <= 0f)
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		float delaySeconds = _config.Dices.ReverseCausality.DelaySeconds;
		float num2 = num + delaySeconds;
		if (!_delayedDamage.ContainsKey(((CBasePlayerController)val).SteamID))
		{
			_delayedDamage[((CBasePlayerController)val).SteamID] = new List<PendingDamage>();
		}
		_delayedDamage[((CBasePlayerController)val).SteamID].Add(new PendingDamage
		{
			ApplyTime = num2,
			Damage = (int)info.Damage
		});
		_doubleDamageUntil[((CBasePlayerController)val).SteamID] = Math.Max(_doubleDamageUntil.TryGetValue(((CBasePlayerController)val).SteamID, out var value2) ? value2 : 0f, num2);
		float damageMultiplier = _config.Dices.ReverseCausality.DamageMultiplier;
		DamageBonusManager.RegisterBySteamId(((CBasePlayerController)val).SteamID, "ReverseCausality", damageMultiplier);
		info.Damage = 0f;
		val.PrintToCenterAlert($"⏳ 因果倒置！伤害延迟{delaySeconds:F0}s，期间伤害翻倍！");
		return (HookResult)1;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid)
		{
			return (HookResult)0;
		}
		if (!_players.Contains(userid))
		{
			return (HookResult)0;
		}
		ApplyAllPending(((CBasePlayerController)userid).SteamID);
		_doubleDamageUntil.Remove(((CBasePlayerController)userid).SteamID);
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (ulong steamId in _delayedDamage.Keys.ToList())
		{
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == steamId);
			if (!((CEntityInstance)(object)val == (CEntityInstance)null) && ((CEntityInstance)val).IsValid && !((CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)val.PlayerPawn.Value).IsValid && ((CBaseEntity)val.PlayerPawn.Value).LifeState == 0)
			{
			}
		}
		foreach (ulong steamId2 in _players.Select((CCSPlayerController p) => ((CBasePlayerController)p).SteamID).ToList())
		{
			if (!_delayedDamage.TryGetValue(steamId2, out List<PendingDamage> value) || value.Count == 0)
			{
				continue;
			}
			CCSPlayerController val2 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == steamId2);
			if ((CEntityInstance)(object)((val2 == null) ? null : val2.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)val2.PlayerPawn.Value).IsValid || ((CBaseEntity)val2.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			CCSPlayerPawn value2 = val2.PlayerPawn.Value;
			int num2 = 0;
			for (int num3 = value.Count - 1; num3 >= 0; num3--)
			{
				if (num >= value[num3].ApplyTime)
				{
					num2 += value[num3].Damage;
					value.RemoveAt(num3);
				}
			}
			if (num2 > 0)
			{
				((CBaseEntity)value2).Health -= num2;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				val2.PrintToCenterAlert($"⏳ 因果倒置！-{num2}HP！");
				if (((CBaseEntity)value2).Health <= 0 && !val2.IsBot)
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
				}
			}
			if (_doubleDamageUntil.TryGetValue(steamId2, out var value3) && num >= value3 && value.Count == 0)
			{
				DamageBonusManager.UnregisterBySteamId(steamId2, "ReverseCausality");
				_doubleDamageUntil.Remove(steamId2);
			}
		}
	}
}
