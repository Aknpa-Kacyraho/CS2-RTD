using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class RouletteGambler : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, int> _bonusShots = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "RouletteGambler";

	public override List<string> Events
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "EventPlayerDeath";
			num2++;
			span[num2] = "EventWeaponFire";
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

	public RouletteGambler(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_bonusShots[player] = 0;
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
		RevertBonus(player);
		_bonusShots.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			RevertBonus(item);
		}
		_players.Clear();
		_bonusShots.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void RevertBonus(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private void ApplyBonus(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			int num = (_bonusShots.TryGetValue(player, out var value) ? value : 0);
			float bonusMaxPercent = _config.Dices.RouletteGambler.BonusMaxPercent;
			float num2 = Math.Min((float)num * 0.01f, bonusMaxPercent / 100f);
			player.PlayerPawn.Value.VelocityModifier = 1f + num2;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public void OnTick()
	{
		if (_bonusShots.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<CCSPlayerController, int> item in _bonusShots.ToList())
		{
			try
			{
				CCSPlayerController key = item.Key;
				if (!((CEntityInstance)(object)((key == null) ? null : key.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)item.Key.PlayerPawn.Value).IsValid && ((CBaseEntity)item.Key.PlayerPawn.Value).LifeState == 0)
				{
					ApplyBonus(item.Key);
				}
			}
			catch
			{
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		if (_bonusShots.Count == 0)
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
		int num = (_bonusShots.TryGetValue(val, out var value3) ? value3 : 0);
		float bonusMaxPercent = _config.Dices.RouletteGambler.BonusMaxPercent;
		float num2 = Math.Min((float)num * 0.01f, bonusMaxPercent / 100f);
		info.Damage *= 1f + num2;
		return (HookResult)1;
	}

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid) || (CEntityInstance)(object)userid.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)userid.PlayerPawn.Value).IsValid || ((CBaseEntity)userid.PlayerPawn.Value).LifeState != 0)
		{
			return (HookResult)0;
		}
		float deathChance = _config.Dices.RouletteGambler.DeathChance;
		if (_random.NextDouble() <= (double)deathChance)
		{
			if (!userid.IsBot)
			{
				((CBasePlayerPawn)userid.PlayerPawn.Value).CommitSuicide(false, true);
			}
			userid.PrintToCenterAlert("\ud83d\udd2b 赌命失败!");
			return (HookResult)0;
		}
		int num = (_bonusShots.TryGetValue(userid, out var value) ? value : 0);
		int num2 = (int)_config.Dices.RouletteGambler.BonusMaxPercent;
		if (num < num2)
		{
			_bonusShots[userid] = num + 1;
			ApplyBonus(userid);
			userid.PrintToCenterAlert($"\ud83d\udd2b 赌命成功! 当前加成: {num + 1}%");
		}
		return (HookResult)0;
	}
}
