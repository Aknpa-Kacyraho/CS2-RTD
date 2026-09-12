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

public class RouletteGambler : DiceBlueprint
{
	private const float BonusPerShot = 0.01f;

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
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
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
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		DamageBonusManager.Unregister(player, ClassName);
		SpeedBonusManager.Unregister(player, ClassName);
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f + SpeedBonusManager.GetEffective(player, 100f);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private void ApplyBonus(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			float num2 = SpeedBonusManager.GetEffective(player, 100f);
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

	public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
	{
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
		float current = DamageBonusManager.GetSource(userid, ClassName);
		float cap = _config.Dices.RouletteGambler.BonusMaxPercent;
		if (cap > 0f && current >= cap / 100f)
		{
			return (HookResult)0;
		}
		float amount = BonusPerShot;
		if (cap > 0f && current + amount > cap / 100f)
		{
			amount = cap / 100f - current;
		}
		DamageBonusManager.AddStack(userid, ClassName, amount);
		SpeedBonusManager.AddStack(userid, ClassName, amount);
		_bonusShots[userid] = (_bonusShots.TryGetValue(userid, out var value) ? value : 0) + 1;
		ApplyBonus(userid);
		userid.PrintToCenterAlert($"\ud83d\udd2b 赌命成功! 当前加成: +{(current + amount) * 100f:F0}%");
		return (HookResult)0;
	}
}
