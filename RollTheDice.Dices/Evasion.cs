using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Evasion : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, float> _dodgeChance = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool?> _lastDodged = new Dictionary<CCSPlayerController, bool?>();

	public override string ClassName => "Evasion";

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

	public Evasion(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			Random random = new Random(Guid.NewGuid().GetHashCode());
			float num = _config.Dices.Evasion.DodgeChanceMin + (float)random.NextDouble() * (_config.Dices.Evasion.DodgeChanceMax - _config.Dices.Evasion.DodgeChanceMin);
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Shield");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "钢铁壁垒", "钢铁壁垒联动生效！");
			}
			_dodgeChance[player] = num;
			_lastDodged[player] = null;
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"chance",
					(num * 100f).ToString("F0")
				}
			});
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_dodgeChance.Remove(player);
		_lastDodged.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_dodgeChance.Clear();
		_lastDodged.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_dodgeChance.TryGetValue(val, out var _))
		{
			return (HookResult)0;
		}
		bool? flag = (_lastDodged.TryGetValue(val, out var value3) ? value3 : ((bool?)null));
		if (!flag.HasValue || flag == false || _random.NextDouble() < _dodgeChance[val])
		{
			info.Damage = 0f;
			_lastDodged[val] = true;
			val.PrintToCenterAlert("↗ 闪避!");
			return (HookResult)1;
		}
		_lastDodged[val] = false;
		return (HookResult)0;
	}
}
