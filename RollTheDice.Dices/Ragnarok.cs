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

public class Ragnarok : DiceBlueprint
{
	private bool _comboActive;

	private static bool _timeAccelerated;

	private float _roundStartTime;

	private bool _roundEnded;

	private CCSPlayerController? _holder;

	private int _holderTeam;

	private bool _holderDied;

	public override string ClassName => "Ragnarok";

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

	public Ragnarok(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (_roundStartTime == 0f)
			{
				_roundStartTime = Server.CurrentTime;
			}
			_roundEnded = false;
			_holder = player;
			_holderTeam = ((CBaseEntity)player).TeamNum;
			_holderDied = false;
			_comboActive = DiceSynergy.HasPartner(player, "NukeLeak");
			if (_comboActive && !_timeAccelerated)
			{
				_timeAccelerated = true;
				DiceSynergy.AnnounceCombo(player, "末日审判", "诸神黄昏+核泄漏！终焉加速降临！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏳ 终焉降临！持有者30s无敌，60s后与一名队友共赴黄昏！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_roundStartTime = 0f;
		_roundEnded = true;
		_timeAccelerated = false;
		_holder = null;
		_holderDied = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || _roundEnded || (CEntityInstance)(object)_holder == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)_holder.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)_holder.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		if (((NativeEntity)entity).Handle != ((NativeEntity)_holder.PlayerPawn.Value).Handle)
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		float num2 = _config.Dices.Ragnarok.InvulDuration * (_timeAccelerated ? 0.5f : 1f);
		if (num - _roundStartTime < num2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (_roundEnded || _holderDied)
		{
			return (HookResult)0;
		}
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || (CEntityInstance)(object)_holder == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)userid != (CEntityInstance)(object)_holder)
		{
			return (HookResult)0;
		}
		_holderDied = true;
		_roundEnded = true;
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)_holder && ((CBaseEntity)p).TeamNum == _holderTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p).ToList();
		CCSPlayerController val = null;
		if (list.Count > 0)
		{
			val = list[Random.Shared.Next(list.Count)];
		}
		if ((CEntityInstance)(object)val != (CEntityInstance)null)
		{
			CCSPlayerController val2 = val;
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
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc80 {((CBasePlayerController)userid).PlayerName} 提前陨落！{((CBasePlayerController)val).PlayerName} 被终焉之力吞噬！");
		}
		else
		{
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc80 {((CBasePlayerController)userid).PlayerName} 提前陨落！终焉消逝...");
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0 || _roundEnded)
		{
			return;
		}
		float num = Server.CurrentTime;
		float num2 = num - _roundStartTime;
		float num3 = _config.Dices.Ragnarok.RoundDuration * (_timeAccelerated ? 0.5f : 1f);
		if (num2 >= num3 - 30f && num2 < num3 - 29f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏳ 终焉还剩30秒！");
			CCSPlayerController? holder = _holder;
			if (holder != null)
			{
				holder.PrintToCenterAlert("⏳ 终焉还剩30秒");
			}
		}
		if (num2 >= num3 - 20f && num2 < num3 - 19f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏳ 终焉还剩20秒！");
			CCSPlayerController? holder2 = _holder;
			if (holder2 != null)
			{
				holder2.PrintToCenterAlert("⏳ 终焉还剩20秒");
			}
		}
		if (num2 >= num3 - 10f && num2 < num3 - 9.9f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏰ 终焉还剩10秒！");
			CCSPlayerController? holder3 = _holder;
			if (holder3 != null)
			{
				holder3.PrintToCenterAlert("⏳ 终焉还剩10秒");
			}
		}
		if (num2 >= num3 - 5f && num2 < num3 - 4.9f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udc80 终焉还剩5秒！");
			CCSPlayerController? holder4 = _holder;
			if (holder4 != null)
			{
				holder4.PrintToCenterAlert("⏳ 终焉还剩5秒");
			}
		}
		if (!(num2 >= num3))
		{
			return;
		}
		_roundEnded = true;
		CCSPlayerController? holder5 = _holder;
		if ((CEntityInstance)(object)((holder5 == null) ? null : holder5.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)_holder.PlayerPawn.Value).IsValid && ((CBaseEntity)_holder.PlayerPawn.Value).LifeState == 0)
		{
			if (!_holder.IsBot && !((CBasePlayerController)_holder).IsHLTV)
			{
				((CBasePlayerPawn)_holder.PlayerPawn.Value).CommitSuicide(false, true);
			}
			else
			{
				try
				{
					((CBasePlayerPawn)_holder.PlayerPawn.Value).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)_holder.PlayerPawn.Value).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)_holder.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
				}
			}
		}
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)_holder && ((CBaseEntity)p).TeamNum == _holderTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p).ToList();
		if (list.Count > 0)
		{
			CCSPlayerController val = list[Random.Shared.Next(list.Count)];
			if (!val.IsBot && !((CBasePlayerController)val).IsHLTV)
			{
				((CBasePlayerPawn)val.PlayerPawn.Value).CommitSuicide(false, true);
			}
			else
			{
				try
				{
					((CBasePlayerPawn)val.PlayerPawn.Value).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)val.PlayerPawn.Value).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)val.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
				}
			}
			Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc80 终焉！{((CBasePlayerController)val).PlayerName} 被终焉之力吞噬！");
		}
		else
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udc80 终焉降临！持有者已陨落...");
		}
	}
}
