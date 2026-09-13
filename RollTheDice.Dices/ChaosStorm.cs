using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class ChaosStorm : DiceBlueprint
{
	private float _nextSwapTime;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "ChaosStorm";

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

	public ChaosStorm(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (_nextSwapTime == 0f)
			{
				_nextSwapTime = Server.CurrentTime + GetInterval();
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_ChaosStorm_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
			if (DiceSynergy.HasPartner(player, "Twilight"))
			{
				DiceSynergy.AnnounceCombo(player, "时空乱流", "风暴间隔缩短至 30s，换位后减速 50%！");
			}
		}
	}

	private float GetInterval()
	{
		return (_players.Any((CCSPlayerController p) => DiceSynergy.HasPartner(p, "Twilight")) ? 30f : _config.Dices.ChaosStorm.Interval);
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_nextSwapTime = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Expected O, but got Unknown
		//IL_021b: Expected O, but got Unknown
		if (_players.Count == 0 || _nextSwapTime == 0f)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num < _nextSwapTime)
		{
			return;
		}
		float interval = GetInterval();
		_nextSwapTime = num + interval;
		List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
			select p).ToList();
		if (list.Count < 2)
		{
			return;
		}
		var list2 = list.Select(delegate(CCSPlayerController p)
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected O, but got Unknown
			return new
			{
				Player = p,
				Pos = new Vector((float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)p.PlayerPawn.Value).AbsOrigin.Z)
			};
		}).ToList();
		List<Vector> list3 = list2.Select(p => p.Pos).ToList();
		for (int num2 = list3.Count - 1; num2 > 0; num2--)
		{
			int num3 = _random.Next(num2 + 1);
			List<Vector> list4 = list3;
			int index = num2;
			int index2 = num3;
			Vector value = list3[num3];
			Vector value2 = list3[num2];
			list4[index] = value;
			list3[index2] = value2;
		}
		for (int num4 = 0; num4 < list2.Count; num4++)
		{
			CCSPlayerController player = list2[num4].Player;
			if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null))
			{
				Vector val = list3[num4];
				((CBaseEntity)player.PlayerPawn.Value).Teleport(val, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
			}
		}
		if (_players.Any((CCSPlayerController p) => DiceSynergy.HasPartner(p, "Twilight")))
		{
			foreach (var entry in list2)
			{
				CCSPlayerController slowed = entry.Player;
				if (slowed == null || !((CEntityInstance)slowed).IsValid || slowed.PlayerPawn?.Value == null || !((CEntityInstance)slowed.PlayerPawn.Value).IsValid)
				{
					continue;
				}
				SpeedBonusManager.Register(slowed, "ChaosStormCombo", -0.5f);
				CCSPlayerPawn slowedPawn = slowed.PlayerPawn.Value;
				slowedPawn.VelocityModifier = 1f + SpeedBonusManager.GetEffective(slowed, 100f);
				Utilities.SetStateChanged((CBaseEntity)(object)slowedPawn, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				slowed.PrintToCenterAlert("\ud83c\udf00 \u65f6\u7a7a\u4e71\u6d41\uff01\u51cf\u901f 50% 1.5\u79d2");
				CCSPlayerController capturedSlowed = slowed;
				new Timer(1.5f, (Action)delegate
				{
					SpeedBonusManager.Unregister(capturedSlowed, "ChaosStormCombo");
					CCSPlayerPawn after = capturedSlowed?.PlayerPawn?.Value;
					if (after != null && ((CEntityInstance)after).IsValid)
					{
						after.VelocityModifier = 1f + SpeedBonusManager.GetEffective(capturedSlowed, 100f);
						Utilities.SetStateChanged((CBaseEntity)(object)after, "CCSPlayerPawn", "m_flVelocityModifier", 0);
					}
				}, (TimerFlags?)null);
			}
		}
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_ChaosStorm_swap"].Value);
	}
}
