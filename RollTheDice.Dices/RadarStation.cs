using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class RadarStation : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)>> _enemyGlows = new Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>>();

	private readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _selfGlows = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();

	public override string ClassName => "RadarStation";


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

	public RadarStation(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_enemyGlows[player] = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();
			ApplyEnemyGlows(player);
			if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
			{
				_selfGlows[player] = GlowUtil.CreateGlow((CBaseEntity)(object)player.PlayerPawn.Value, Color.Orange);
				player.PlayerPawn.Value.VelocityModifier = 0.6f;
				Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
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
		if (_enemyGlows.TryGetValue(player, out Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> value))
		{
			foreach (KeyValuePair<CCSPlayerController, (CDynamicProp, CDynamicProp)> item in value)
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)item.Value.Item1, (CBaseEntity?)(object)item.Value.Item2);
			}
			value.Clear();
		}
		_enemyGlows.Remove(player);
		if (_selfGlows.TryGetValue(player, out (CDynamicProp, CDynamicProp) value2))
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)value2.Item1, (CBaseEntity?)(object)value2.Item2);
			_selfGlows.Remove(player);
		}
		_players.Remove(player);
		if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid && (CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (_enemyGlows.TryGetValue(item, out Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> value))
			{
				foreach (KeyValuePair<CCSPlayerController, (CDynamicProp, CDynamicProp)> item2 in value)
				{
					GlowUtil.RemoveGlow((CBaseEntity?)(object)item2.Value.Item1, (CBaseEntity?)(object)item2.Value.Item2);
				}
			}
			if (_selfGlows.TryGetValue(item, out (CDynamicProp, CDynamicProp) value2))
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)value2.Item1, (CBaseEntity?)(object)value2.Item2);
			}
		}
		_players.Clear();
		_enemyGlows.Clear();
		_selfGlows.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController diceOwner in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)diceOwner == (CEntityInstance)null || !((CEntityInstance)diceOwner).IsValid || (CEntityInstance)(object)diceOwner.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)diceOwner.PlayerPawn.Value).IsValid || ((CBaseEntity)diceOwner.PlayerPawn.Value).LifeState != 0)
				{
					continue;
				}
				diceOwner.PlayerPawn.Value.VelocityModifier = 0.6f;
				Utilities.SetStateChanged((CBaseEntity)(object)diceOwner.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				if (!_enemyGlows.TryGetValue(diceOwner, out Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> value))
				{
					continue;
				}
				HashSet<CCSPlayerController> hashSet = new HashSet<CCSPlayerController>();
				foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && ((CBaseEntity)p).TeamNum != ((CBaseEntity)diceOwner).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
					select p)
				{
					hashSet.Add(item);
					if (!value.ContainsKey(item))
					{
						Color color = ((((CBaseEntity)item).TeamNum == 2) ? Color.Red : Color.Blue);
						value[item] = GlowUtil.CreateGlow((CBaseEntity)(object)item.PlayerPawn.Value, color);
					}
				}
				foreach (CCSPlayerController item2 in value.Keys.ToList())
				{
					if (!hashSet.Contains(item2))
					{
						(CDynamicProp, CDynamicProp) tuple = value[item2];
						GlowUtil.RemoveGlow((CBaseEntity?)(object)tuple.Item1, (CBaseEntity?)(object)tuple.Item2);
						value.Remove(item2);
					}
				}
			}
			catch
			{
			}
		}
	}

	private void ApplyEnemyGlows(CCSPlayerController diceOwner)
	{
		if (!_enemyGlows.TryGetValue(diceOwner, out Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> value))
		{
			return;
		}
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && ((CBaseEntity)p).TeamNum != ((CBaseEntity)diceOwner).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			if (!value.ContainsKey(item))
			{
				Color color = ((((CBaseEntity)item).TeamNum == 2) ? Color.Red : Color.Blue);
				value[item] = GlowUtil.CreateGlow((CBaseEntity)(object)item.PlayerPawn.Value, color);
			}
		}
	}
}
