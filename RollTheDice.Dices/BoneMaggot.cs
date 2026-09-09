using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class BoneMaggot : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _markedVictims = new Dictionary<ulong, float>();

	public override string ClassName => "BoneMaggot";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public BoneMaggot(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
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
		_markedVictims.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Expected O, but got Unknown
		//IL_02aa: Expected O, but got Unknown
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val) || (CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || (CEntityInstance)(object)val2 == (CEntityInstance)(object)val || ((CBaseEntity)val2).TeamNum == ((CBaseEntity)val).TeamNum)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)val2.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val2.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (_markedVictims.TryGetValue(((CBasePlayerController)val2).SteamID, out var value4) && num < value4)
		{
			return (HookResult)0;
		}
		float markDuration = _config.Dices.BoneMaggot.MarkDuration;
		_markedVictims[((CBasePlayerController)val2).SteamID] = num + markDuration;
		CCSPlayerPawn value5 = val2.PlayerPawn.Value;
		var (glowProxy, glow) = GlowUtil.CreateGlow((CBaseEntity)(object)value5, Color.FromArgb(255, 50, 255, 50));
		if ((CEntityInstance)(object)glow != (CEntityInstance)null)
		{
			((CBaseModelEntity)glow).Glow.GlowType = 3;
			((CBaseModelEntity)glow).Glow.GlowRange = 5000;
			((CBaseModelEntity)glow).Glow.GlowRangeMin = 0;
		}
		CParticleSystem particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		if ((CEntityInstance)(object)particle != (CEntityInstance)null)
		{
			particle.EffectName = "particles/critters/chicken/chicken_impact_burst_zombie.vpcf";
			((CBaseEntity)particle).Teleport(((CBaseEntity)value5).AbsOrigin, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			particle.StartActive = true;
			((CBaseEntity)particle).DispatchSpawn();
			new Timer(2f, (Action)delegate
			{
				if ((CEntityInstance)(object)particle != (CEntityInstance)null && ((CEntityInstance)particle).IsValid)
				{
					((CEntityInstance)particle).Remove();
				}
			}, (TimerFlags?)null);
		}
		val2.PrintToCenterAlert("\ud83d\udc1b 你被标记了!");
		new Timer(markDuration, (Action)delegate
		{
			if ((CEntityInstance)(object)glowProxy != (CEntityInstance)null && ((CEntityInstance)glowProxy).IsValid)
			{
				((CEntityInstance)glowProxy).Remove();
			}
			if ((CEntityInstance)(object)glow != (CEntityInstance)null && ((CEntityInstance)glow).IsValid)
			{
				((CEntityInstance)glow).Remove();
			}
		}, (TimerFlags?)null);
		return (HookResult)0;
	}
}
