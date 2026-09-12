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

public class Miser : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _startingMoney = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, float> _reductionCache = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Miser";

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

	public Miser(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Bank");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "资本要塞", "资本要塞联动生效！");
			}
			if (player.InGameMoneyServices != null)
			{
				_startingMoney[player] = player.InGameMoneyServices.Account;
			}
			_reductionCache[player] = 0f;
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
		_startingMoney.Remove(player);
		_reductionCache.Remove(player);
		DamageReductionManager.Unregister(player, "Miser");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players)
		{
			DamageReductionManager.Unregister(item, "Miser");
		}
		_players.Clear();
		_startingMoney.Clear();
		_reductionCache.Clear();
	}

	public void OnTick()
	{
		foreach (CCSPlayerController item in _players)
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			float reduction = GetReduction(item);
			float applied = DiceSynergy.HasPartner(item, "Bank") ? Math.Min(reduction * 1.5f, 0.75f) : reduction;
			DamageReductionManager.Register(item, "Miser", applied);
		}
	}

	private float GetReduction(CCSPlayerController player)
	{
		if (player.InGameMoneyServices == null)
		{
			return 0f;
		}
		int num = (_startingMoney.TryGetValue(player, out var value) ? value : 0);
		int account = player.InGameMoneyServices.Account;
		int num2 = num - account;
		if (num2 <= 0)
		{
			return 0f;
		}
		int threshold = _config.Dices.Miser.Threshold;
		float reductionPerStep = _config.Dices.Miser.ReductionPerStep;
		float maxReduction = _config.Dices.Miser.MaxReduction;
		int num3 = num2 / threshold;
		return Math.Min((float)num3 * reductionPerStep, maxReduction);
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
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
		if ((CEntityInstance)(object)val.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)val.PlayerPawn.Value).IsValid)
		{
			return (HookResult)0;
		}
		float reduction = GetReduction(val);
		if (reduction > 0.001f)
		{
			float num = (_reductionCache.TryGetValue(val, out var value2) ? value2 : 0f);
			if (Math.Abs(reduction - num) > 0.01f)
			{
				_reductionCache[val] = reduction;
				int value3 = (int)(reduction * 100f);
				val.PrintToCenterAlert($"\ud83d\udcb0 吝啬减伤 {value3}%");
			}
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
