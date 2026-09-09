using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DamageMultiplier : DiceBlueprint
{
	public readonly Random _random = new Random();

	public readonly Dictionary<uint, float> _playerMultipliers = new Dictionary<uint, float>();

	private bool _comboActive;

	public override string ClassName => "DamageMultiplier";

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

	public DamageMultiplier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			float minMultiplier = _config.Dices.DamageMultiplier.MinMultiplier;
			float maxMultiplier = _config.Dices.DamageMultiplier.MaxMultiplier;
			float value = (float)Math.Round(_random.NextDouble() * (double)(maxMultiplier - minMultiplier) + (double)minMultiplier, 2);
			_playerMultipliers.Add(((CEntityInstance)((CBasePlayerController)player).Pawn.Value).Index, value);
			_comboActive = DiceSynergy.HasPartner(player, "Berserker");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "狂暴之力", "毁灭之力+狂战士！极限倍率+50%");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"multiplier",
					value.ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		if (!((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_playerMultipliers.Remove(((CEntityInstance)((CBasePlayerController)player).Pawn.Value).Index);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_playerMultipliers.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)info.Attacker.Value == (CEntityInstance)null || !_playerMultipliers.ContainsKey(((CEntityInstance)info.Attacker.Value).Index))
		{
			return (HookResult)0;
		}
		if (_playerMultipliers.TryGetValue(((CEntityInstance)info.Attacker.Value).Index, out var value))
		{
			CCSPlayerController val = _players.FirstOrDefault((CCSPlayerController p) => (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).Index == ((CEntityInstance)info.Attacker.Value).Index);
			info.Damage *= (val != null && DiceSynergy.HasPartner(val, "Berserker") ? (value * 1.5f) : value);
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
