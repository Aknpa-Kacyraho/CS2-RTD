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

/// <summary>
/// 终焉 Ragnarok：开局 {InvulDuration}s 无敌；{RoundDuration}s 后降下终焉，对所有敌人造成 {FinalDamage} 审判伤害。
/// （2026-09-18 重做：移除"自爆+随机献祭队友"的净负面设计，改为纯收益的全场处决。）
/// </summary>
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
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏳ 终焉降临！持有者30s无敌，60s后审判所有敌人！");
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
		if (!_active() || (CEntityInstance)(object)_holder.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)_holder.PlayerPawn.Value).IsValid)
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

	private bool _active()
	{
		return _players.Count != 0 && !_roundEnded && (CEntityInstance)(object)_holder != (CEntityInstance)null;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
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
		Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83d\udc80 {((CBasePlayerController)userid).PlayerName} 提前陨落！终焉消逝...");
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
			_holder?.PrintToCenterAlert("⏳ 终焉还剩30秒");
		}
		if (num2 >= num3 - 20f && num2 < num3 - 19f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏳ 终焉还剩20秒！");
			_holder?.PrintToCenterAlert("⏳ 终焉还剩20秒");
		}
		if (num2 >= num3 - 10f && num2 < num3 - 9.9f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "⏰ 终焉还剩10秒！");
			_holder?.PrintToCenterAlert("⏳ 终焉还剩10秒");
		}
		if (num2 >= num3 - 5f && num2 < num3 - 4.9f)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udc80 终焉还剩5秒！");
			_holder?.PrintToCenterAlert("⏳ 终焉还剩5秒");
		}
		if (!(num2 >= num3))
		{
			return;
		}
		_roundEnded = true;
		int finalDamage = _config.Dices.Ragnarok.FinalDamage;
		foreach (CCSPlayerController enemy in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != _holderTeam && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			CCSPlayerPawn pawn = enemy.PlayerPawn.Value;
			((CBaseEntity)pawn).Health -= finalDamage;
			Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)pawn).Health > 0)
			{
				enemy.PrintToCenterAlert($"\u2696 终焉审判！-{finalDamage}HP");
				continue;
			}
			if (!enemy.IsBot && !((CBasePlayerController)enemy).IsHLTV)
			{
				((CBasePlayerPawn)pawn).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)pawn).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)pawn).Health = 0;
				Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
			}
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udc80 终焉降临！所有敌人受到审判！");
	}
}
