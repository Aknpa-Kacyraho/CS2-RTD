using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class FourtyTwo : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextTrigger = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _invulEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _invisEnd = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, bool> _invisShown = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "FourtyTwo";

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

	public FourtyTwo(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextTrigger[player] = Server.CurrentTime + _config.Dices.FourtyTwo.Interval;
			_invulEnd[player] = 0f;
			_invisEnd[player] = 0f;
			_invisShown[player] = false;
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
		_nextTrigger.Remove(player);
		_invulEnd.Remove(player);
		_invisEnd.Remove(player);
		_invisShown.Remove(player);
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_nextTrigger.Clear();
		_invulEnd.Clear();
		_invisEnd.Clear();
		_invisShown.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
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
		float num = Server.CurrentTime;
		if (_invulEnd.TryGetValue(val, out var value2) && num < value2)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				continue;
			}
			CCSPlayerPawn value = item.PlayerPawn.Value;
			if (_nextTrigger.TryGetValue(item, out var value2) && num >= value2)
			{
				_nextTrigger[item] = num + _config.Dices.FourtyTwo.Interval;
				_invulEnd[item] = num + _config.Dices.FourtyTwo.InvulDuration;
				_invisEnd[item] = num + _config.Dices.FourtyTwo.InvulDuration + _config.Dices.FourtyTwo.InvisDuration;
				_invisShown[item] = false;
				item.PrintToCenterAlert("4\ufe0f\u20e32\ufe0f\u20e3 无敌4s！");
			}
			if (_invulEnd.TryGetValue(item, out var value3) && num >= value3 && _invisEnd.TryGetValue(item, out var value4) && num < value4 && !_invisShown.GetValueOrDefault(item, defaultValue: false))
			{
				_invisShown[item] = true;
				item.PrintToCenterAlert("隐身2s！");
			}
			if (_invisEnd.TryGetValue(item, out var value5) && num < value5)
			{
				float valueOrDefault = _invulEnd.GetValueOrDefault(item, num);
				if (num < valueOrDefault)
				{
					((CBaseModelEntity)value).Render = Color.FromArgb(100, 255, 255, 255);
				}
				else
				{
					((CBaseModelEntity)value).Render = Color.FromArgb(10, 255, 255, 255);
				}
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
			else if (((CBaseModelEntity)value).Render != Color.FromArgb(255, 255, 255, 255))
			{
				((CBaseModelEntity)value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
	}
}
