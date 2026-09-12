using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class DeathKnight : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, float> _nextRegenTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "DeathKnight";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			return list;
		}
	}

	public DeathKnight(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		_nextRegenTime[player] = 0f;
		_comboActive = DiceSynergy.HasPartner(player, "Frostmourne");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "Frostmourne"))
			{
				DiceSynergy.AnnounceCombo(player, "死亡骑士完全体", "死亡骑士+霜之哀伤合成为死亡骑士完全体！");
				CCSPlayerController captured = player;
				Server.NextFrame((Action)delegate
				{
					if (instance != null && ((CEntityInstance)captured).IsValid)
					{
						instance.RemoveDiceFromPlayer(captured, "DeathKnight");
						instance.RemoveDiceFromPlayer(captured, "Frostmourne");
						instance.ForceDiceForPlayer(captured, "DeathKnightComplete");
					}
				});
				return;
			}
			DiceSynergy.AnnounceCombo(player, "死亡骑士完全体", "团队联动！死亡骑士与霜之哀伤共鸣！");
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
		_nextRegenTime.Remove(player);
		DamageReductionManager.Unregister(player, "DeathKnight");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageReductionManager.Unregister(item, "DeathKnight");
		}
		_players.Clear();
		_nextRegenTime.Clear();
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
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			CCSPlayerPawn value = item.PlayerPawn.Value;
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value).WeaponServices;
			if (weaponServices != null)
			{
				CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
				bool? obj;
				if (activeWeapon == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value2 = activeWeapon.Value;
					obj = ((value2 == null) ? ((bool?)null) : ((CEntityInstance)value2).DesignerName?.Contains("knife", StringComparison.OrdinalIgnoreCase));
				}
				bool? flag = obj;
				if (flag == true && (!_nextRegenTime.TryGetValue(item, out var value3) || num >= value3))
				{
					_nextRegenTime[item] = num + 1f;
					((CBaseEntity)value).Health = Math.Min(((CBaseEntity)value).Health + _config.Dices.DeathKnight.HpRegen, ((CBaseEntity)value).MaxHealth);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				}
			}
			float val = 1f - (float)((CBaseEntity)value).Health / (float)Math.Max(((CBaseEntity)value).MaxHealth, 1);
			float percentage = Math.Min(val, _config.Dices.DeathKnight.ReductionCap * (DiceSynergy.HasPartner(item, "Frostmourne") ? 1.5f : 1f));
			DamageReductionManager.Register(item, "DeathKnight", percentage);
		}
	}
}
