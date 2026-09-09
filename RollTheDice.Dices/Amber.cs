using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Amber : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<ulong, float> _frozenUntil = new Dictionary<ulong, float>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Amber";

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

	public Amber(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "IceBeam");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "极寒地狱", "冻结时间翻倍+琥珀概率翻倍！");
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
		_players.Clear();
		_frozenUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
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
		if (_frozenUntil.TryGetValue(((CBasePlayerController)val).SteamID, out var value2) && Server.CurrentTime < value2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj3;
		if (attacker == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value3 = attacker.Value;
			if (value3 == null)
			{
				obj3 = null;
			}
			else
			{
				CCSPlayerPawn obj4 = ((NativeObject)value3).As<CCSPlayerPawn>();
				if (obj4 == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerController value4 = controller2.Value;
						obj3 = ((value4 != null) ? ((NativeObject)value4).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj3;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val2 == (CEntityInstance)(object)val)
		{
			return (HookResult)0;
		}
		if (_frozenUntil.TryGetValue(((CBasePlayerController)val2).SteamID, out var value5) && Server.CurrentTime < value5)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		float num = (float)(_random.NextDouble() * (double)(_config.Dices.Amber.FreezeChanceMax - _config.Dices.Amber.FreezeChanceMin) + (double)_config.Dices.Amber.FreezeChanceMin) * (DiceSynergy.HasPartner(val, "IceBeam") ? 2f : 1f);
		if (_random.NextDouble() < (double)num)
		{
			float val3 = Server.CurrentTime;
			float freezeDuration = _config.Dices.Amber.FreezeDuration;
			float valueOrDefault = _frozenUntil.GetValueOrDefault(((CBasePlayerController)val2).SteamID, 0f);
			float value6 = Math.Max(val3, valueOrDefault) + freezeDuration;
			_frozenUntil[((CBasePlayerController)val2).SteamID] = value6;
			val2.PrintToCenterAlert("\ud83d\udfe1 被琥珀冻结！无法移动和攻击！");
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_frozenUntil.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<ulong, float> item in _frozenUntil.ToList())
		{
			var (steamId, num4) = item;
			if (num >= num4)
			{
				_frozenUntil.Remove(steamId);
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == steamId);
				CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
				if (val2 != null && ((CEntityInstance)val2).IsValid)
				{
					((CBaseEntity)val2).MoveType = (MoveType_t)2;
					Schema.SetSchemaValue<int>(((NativeEntity)val2).Handle, "CBaseEntity", "m_nActualMoveType", 2);
					((CBaseModelEntity)val2).Render = Color.FromArgb(255, 255, 255, 255);
					Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseModelEntity", "m_clrRender", 0);
				}
			}
			else
			{
				CCSPlayerController val3 = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == steamId);
				CCSPlayerPawn val4 = ((val3 == null) ? null : val3.PlayerPawn?.Value);
				if (val4 != null && ((CEntityInstance)val4).IsValid)
				{
					((CBaseEntity)val4).MoveType = (MoveType_t)0;
					Schema.SetSchemaValue<int>(((NativeEntity)val4).Handle, "CBaseEntity", "m_nActualMoveType", 0);
					((CBaseModelEntity)val4).Render = Color.FromArgb(255, 255, 200, 50);
					Utilities.SetStateChanged((CBaseEntity)(object)val4, "CBaseModelEntity", "m_clrRender", 0);
				}
			}
		}
	}
}
