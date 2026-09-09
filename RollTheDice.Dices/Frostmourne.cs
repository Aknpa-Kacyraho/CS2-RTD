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

public class Frostmourne : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, float> _lastHealTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Frostmourne";

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

	public Frostmourne(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		_lastHealTime[player] = 0f;
		_comboActive = DiceSynergy.HasPartner(player, "DeathKnight");
		if (_comboActive)
		{
			RollTheDice instance = RollTheDice.Instance;
			if (instance != null && instance.HasDiceActive(player, "DeathKnight"))
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
		DamageReductionManager.Unregister(player, "Frostmourne");
		_players.Remove(player);
		_lastHealTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageReductionManager.Unregister(item, "Frostmourne");
		}
		_players.Clear();
		_lastHealTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
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
		bool flag = false;
		CHandle<CCSPlayerPawn> playerPawn = val.PlayerPawn;
		object obj3;
		if (playerPawn == null)
		{
			obj3 = null;
		}
		else
		{
			CCSPlayerPawn value2 = playerPawn.Value;
			if (value2 == null)
			{
				obj3 = null;
			}
			else
			{
				CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value2).WeaponServices;
				if (weaponServices == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
					if (activeWeapon == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerWeapon value3 = activeWeapon.Value;
						obj3 = ((value3 != null) ? ((CEntityInstance)value3).DesignerName : null);
					}
				}
			}
		}
		string text = (string)obj3;
		if (text != null && text.Contains("knife"))
		{
			flag = true;
		}
		if (flag)
		{
			float knifeDamageReduction = _config.Dices.Frostmourne.KnifeDamageReduction;
			info.Damage = (int)(info.Damage * (1f - knifeDamageReduction));
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
			if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			bool flag = false;
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
			object obj;
			if (weaponServices == null)
			{
				obj = null;
			}
			else
			{
				CHandle<CBasePlayerWeapon> activeWeapon = weaponServices.ActiveWeapon;
				if (activeWeapon == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value = activeWeapon.Value;
					obj = ((value != null) ? ((CEntityInstance)value).DesignerName : null);
				}
			}
			string text = (string)obj;
			if (text != null && text.Contains("knife"))
			{
				flag = true;
			}
			if (flag && _lastHealTime.TryGetValue(item, out var value2) && num - value2 >= 1f)
			{
				_lastHealTime[item] = num;
				CCSPlayerPawn value3 = item.PlayerPawn.Value;
				((CBaseEntity)value3).Health = Math.Min(((CBaseEntity)value3).Health + _config.Dices.Frostmourne.HealPerSec, ((CBaseEntity)value3).MaxHealth);
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}
}
