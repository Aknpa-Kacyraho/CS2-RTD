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

public class DragonSoul : DiceBlueprint
{
	private bool _comboActive;

	private static bool _transforming;

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "DragonSoul";

	public override bool IsSpecial => true;

	public override float SecondRoundProbability => 0.1f;

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

	public DragonSoul(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			((CBaseEntity)value).MaxHealth = ((CBaseEntity)value).MaxHealth + _config.Dices.DragonSoul.BonusHP;
			((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health + _config.Dices.DragonSoul.BonusHP, ((CBaseEntity)value).MaxHealth);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Dragonborn");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "巨龙共鸣", "巨龙之魂+龙裔！龙裔化龙时额外获得龙魂之力！");
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
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value = player.PlayerPawn.Value;
			if (_originalMaxHealth.TryGetValue(player, out var value2))
			{
				((CBaseEntity)value).MaxHealth = value2;
				((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				_originalMaxHealth.Remove(player);
			}
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
		_transforming = false;
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_players.Count < 2 || _transforming)
		{
			return;
		}
		IEnumerable<IGrouping<byte, CCSPlayerController>> enumerable = from p in _players.Where(delegate(CCSPlayerController p)
			{
				bool flag = (CEntityInstance)(object)p != (CEntityInstance)null && ((CEntityInstance)p).IsValid;
				bool flag2 = flag;
				if (flag2)
				{
					byte teamNum = ((CBaseEntity)p).TeamNum;
					bool flag3 = (uint)(teamNum - 2) <= 1u;
					flag2 = flag3;
				}
				return flag2 && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0;
			})
			group p by ((CBaseEntity)p).TeamNum into g
			where g.Count() >= 2
			select g;
		foreach (IGrouping<byte, CCSPlayerController> item in enumerable)
		{
			List<CCSPlayerController> list = item.ToList();
			if (list.Count >= 2)
			{
				_transforming = true;
				CCSPlayerController first = list[0];
				CCSPlayerController second = list[1];
				RollTheDice instance = RollTheDice.Instance;
				if (instance == null)
				{
					_transforming = false;
					break;
				}
				instance.RemoveDiceFromPlayer(first, "DragonSoul");
				instance.RemoveDiceFromPlayer(second, "DragonSoul");
				Server.NextFrame((Action)delegate
				{
					instance.ForceDiceForPlayer(first, "IceDragon");
					instance.ForceDiceForPlayer(second, "FireDragon");
					Server.PrintToChatAll($" {_localizer["command.prefix"].Value}\ud83e\uddca\ud83d\udd25 {((CBasePlayerController)first).PlayerName} 和 {((CBasePlayerController)second).PlayerName} 的巨龙之魂共鸣！分别进化为冰巨龙与火巨龙！");
					_transforming = false;
				});
			}
		}
	}
}
