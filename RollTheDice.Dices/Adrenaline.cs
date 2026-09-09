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

public class Adrenaline : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, (bool Active, float Speed, float Reduction)> _adrenalineState = new Dictionary<CCSPlayerController, (bool, float, float)>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private bool _comboActive;

	public override string ClassName => "Adrenaline";

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

	public Adrenaline(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		_adrenalineState[player] = (false, 1f, 0f);
		_comboActive = DiceSynergy.HasPartner(player, "Berserker") || DiceSynergy.HasPartner(player, "Overheat");
		if (_comboActive)
		{
			if (DiceSynergy.HasPartner(player, "Berserker"))
			{
				DiceSynergy.AnnounceCombo(player, "狂暴血脉", "肾上腺素速度额外+50%！狂战士的怒火在燃烧");
			}
			if (DiceSynergy.HasPartner(player, "Overheat"))
			{
				DiceSynergy.AnnounceCombo(player, "狂热", "红温速度获取翻倍，上限翻倍！");
			}
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_adrenalineState.Remove(player);
		SpeedBonusManager.Unregister(player, "Adrenaline");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			SpeedBonusManager.Unregister(item, "Adrenaline");
			_players.Remove(item);
		}
		_players.Clear();
		_adrenalineState.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_adrenalineState.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0 || !_adrenalineState.TryGetValue(item, out (bool, float, float) value))
				{
					continue;
				}
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				float num = (float)((CBaseEntity)value2).Health / (float)Math.Max(((CBaseEntity)value2).MaxHealth, 1);
				bool flag = num <= _config.Dices.Adrenaline.HpThresholdPercent;
				if (flag && !value.Item1)
				{
					float num2 = _config.Dices.Adrenaline.SpeedMultiplierMin + (float)_random.NextDouble() * (_config.Dices.Adrenaline.SpeedMultiplierMax - _config.Dices.Adrenaline.SpeedMultiplierMin);
					float num3 = _config.Dices.Adrenaline.ReductionMin + (float)_random.NextDouble() * (_config.Dices.Adrenaline.ReductionMax - _config.Dices.Adrenaline.ReductionMin);
					if (DiceSynergy.HasPartner(item, "Berserker") || DiceSynergy.HasPartner(item, "Overheat"))
					{
						num2 *= 1.5f;
					}
					SpeedBonusManager.Register(item, "Adrenaline", num2 - 1f);
					DamageReductionManager.Register(item, "Adrenaline", num3);
					_adrenalineState[item] = (true, num2, num3);
					item.PrintToCenterAlert($"⚡ 肾上腺素! 速度 {(num2 - 1f) * 100f:F0}% 减伤 {num3 * 100f:F0}%!");
				}
				else if (!flag && value.Item1)
				{
					SpeedBonusManager.Unregister(item, "Adrenaline");
					DamageReductionManager.Unregister(item, "Adrenaline");
					_adrenalineState[item] = (false, 1f, 0f);
				}
				float effective = SpeedBonusManager.GetEffective(item);
				value2.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
			catch
			{
				_adrenalineState.Remove(item);
			}
		}
	}
}
