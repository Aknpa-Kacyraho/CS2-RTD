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

public class FrontlineBeast : DiceBlueprint
{
	private bool _comboActive;

	public static readonly Dictionary<ulong, int> BeastKills = new Dictionary<ulong, int>();

	public override string ClassName => "FrontlineBeast";

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

	private float BaseSpeed => _config.Dices.FrontlineBeast.SpeedMult;

	private float PerKill => _config.Dices.FrontlineBeast.SpeedMultPerKill;

	private float MaxSpeed => _config.Dices.FrontlineBeast.SpeedMultMax;

	public FrontlineBeast(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "SpeedOnKill");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "猎杀本能", "前线速度加倍 猎杀时限翻倍");
			}
			if (!BeastKills.ContainsKey(((CBasePlayerController)player).SteamID))
			{
				BeastKills[((CBasePlayerController)player).SteamID] = 0;
			}
			CCSPlayerPawn value = player.PlayerPawn.Value;
			SpeedBonusManager.Register(player, "FrontlineBeast", BaseSpeed - 1f);
			value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert($"\ud83e\udd81 前线巨兽！速度×{BaseSpeed}，每次击杀+{PerKill}，最高×{MaxSpeed}！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		SpeedBonusManager.Unregister(player, "FrontlineBeast");
		if (reason != DiceRemoveReason.NewDice)
		{
			BeastKills.Remove(((CBasePlayerController)player).SteamID);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			SpeedBonusManager.Unregister(item, "FrontlineBeast");
		}
		foreach (ulong steamId in BeastKills.Keys.ToList())
		{
			SpeedBonusManager.UnregisterBySteamId(steamId, "FrontlineBeast");
		}
		_players.Clear();
		BeastKills.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Expected O, but got Unknown
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)attacker == (CEntityInstance)null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)attacker == (CEntityInstance)(object)userid)
		{
			return (HookResult)0;
		}
		if (((CBaseEntity)attacker).TeamNum == ((CBaseEntity)userid).TeamNum)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn value = attacker.PlayerPawn.Value;
		if ((CEntityInstance)(object)value == (CEntityInstance)null || !((CEntityInstance)value).IsValid)
		{
			return (HookResult)0;
		}
		if (!BeastKills.TryGetValue(((CBasePlayerController)attacker).SteamID, out var value2))
		{
			value2 = 0;
		}
		value2++;
		BeastKills[((CBasePlayerController)attacker).SteamID] = value2;
		float num = Math.Min(BaseSpeed + (float)value2 * PerKill, MaxSpeed);
		if (DiceSynergy.HasPartner(attacker, "SpeedOnKill"))
		{
			num = Math.Min(num * 2f, MaxSpeed * 2f);
		}
		List<CBaseEntity> list = (from s in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist"))
			where ((CEntityInstance)s).IsValid && s.AbsOrigin != null
			select s).ToList();
		if (list.Count > 0)
		{
			List<CBaseEntity> list2 = list.Where((CBaseEntity s) => (((CBaseEntity)attacker).TeamNum == 2 && ((CEntityInstance)s).DesignerName.Contains("terrorist")) || (((CBaseEntity)attacker).TeamNum == 3 && ((CEntityInstance)s).DesignerName.Contains("counterterrorist"))).ToList();
			if (list2.Count == 0)
			{
				list2 = list;
			}
			Random random = new Random(Guid.NewGuid().GetHashCode());
			CBaseEntity val = list2[random.Next(list2.Count)];
			((CBaseEntity)value).Teleport(val.AbsOrigin, val.AbsRotation, new Vector((float?)0f, (float?)0f, (float?)0f));
		}
		((CBaseEntity)value).Health = ((CBaseEntity)value).MaxHealth;
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
		SpeedBonusManager.Register(attacker, "FrontlineBeast", num - 1f);
		value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(attacker, 100f);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		attacker.PrintToCenterAlert($"\ud83e\udd81 前线巨兽！{value2}杀 速度×{num:F1} 满血！");
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (BeastKills.Count == 0 && _players.Count == 0)
		{
			return;
		}
		float baseSpeed = BaseSpeed;
		float perKill = PerKill;
		float maxSpeed = MaxSpeed;
		foreach (KeyValuePair<ulong, int> kv in BeastKills.ToList())
		{
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBasePlayerController)p).SteamID == kv.Key);
			if (!((CEntityInstance)(object)val == (CEntityInstance)null) && ((CEntityInstance)val).IsValid && !((CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)val.PlayerPawn.Value).IsValid && ((CBaseEntity)val.PlayerPawn.Value).LifeState == 0)
			{
				CCSPlayerPawn value = val.PlayerPawn.Value;
				float num = Math.Min(baseSpeed + (float)kv.Value * perKill, maxSpeed);
				if (DiceSynergy.HasPartner(val, "SpeedOnKill"))
				{
					num = Math.Min(num * 2f, maxSpeed * 2f);
				}
				SpeedBonusManager.Register(val, "FrontlineBeast", num - 1f);
				float numExpected = 1f + SpeedBonusManager.GetEffective(val, 100f);
				if (Math.Abs(value.VelocityModifier - numExpected) > 0.01f)
				{
					value.VelocityModifier = numExpected;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !BeastKills.ContainsKey(((CBasePlayerController)item).SteamID) && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				SpeedBonusManager.Register(item, "FrontlineBeast", baseSpeed - 1f);
				float numExpected2 = 1f + SpeedBonusManager.GetEffective(item, 100f);
				if (Math.Abs(value2.VelocityModifier - numExpected2) > 0.01f)
				{
					value2.VelocityModifier = numExpected2;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				}
			}
		}
	}
}
