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
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Gargoyle : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, float> _cooldowns = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _petrifyEndTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, List<string>> _savedWeapons = new Dictionary<CCSPlayerController, List<string>>();

	private readonly Dictionary<CCSPlayerController, int> _savedArmor = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, bool> _savedHelmet = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "Gargoyle";

	public override List<string> Listeners
	{
		get
		{
			int num = 3;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerButtonsChanged";
			num2++;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public override float GetCooldownRemaining(CCSPlayerController player)
	{
		float value;
		return _cooldowns.TryGetValue(player, out value) ? Math.Max(0f, value - Server.CurrentTime) : 0f;
	}

	public Gargoyle(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Titanfall");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "泰坦神像", "石像冷却减半，泰坦提前15s觉醒+50%移速+50%伤害！");
			}
			_cooldowns[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Unpetrify(player);
		_players.Remove(player);
		_cooldowns.Remove(player);
		_petrifyEndTime.Remove(player);
		_savedWeapons.Remove(player);
		_savedArmor.Remove(player);
		_savedHelmet.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Unpetrify(item);
		}
		_players.Clear();
		_cooldowns.Clear();
		_petrifyEndTime.Clear();
		_savedWeapons.Clear();
		_savedArmor.Clear();
		_savedHelmet.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void Unpetrify(CCSPlayerController player)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		MoveLockManager.Unlock(player, "Gargoyle");
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		CCSPlayerPawn value = player.PlayerPawn.Value;
		((CBaseModelEntity)value).Render = Color.FromArgb(255, 255, 255, 255);
		Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
		if (_savedArmor.TryGetValue(player, out var value2))
		{
			value.ArmorValue = value2;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
			_savedArmor.Remove(player);
		}
		if ((_savedHelmet.TryGetValue(player, out var value3) & value3) && ((CBasePlayerPawn)value).ItemServices != null)
		{
			new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)value).ItemServices).Handle).HasHelmet = true;
			_savedHelmet.Remove(player);
		}
		if (!_savedWeapons.TryGetValue(player, out List<string> value4) || ((CBaseEntity)value).LifeState != 0)
		{
			return;
		}
		foreach (string item in value4)
		{
			player.GiveNamedItem(item);
		}
		_savedWeapons.Remove(player);
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (_cooldowns.TryGetValue(player, out var value) && num < value)
		{
			return;
		}
		float duration = _config.Dices.Gargoyle.Duration;
		_cooldowns[player] = num + (DiceSynergy.HasPartner(player, "Titanfall") ? (_config.Dices.Gargoyle.Cooldown / 2f) : _config.Dices.Gargoyle.Cooldown);
		_petrifyEndTime[player] = num + duration;
		CCSPlayerPawn value2 = player.PlayerPawn.Value;
		_savedArmor[player] = value2.ArmorValue;
		CPlayer_ItemServices itemServices = ((CBasePlayerPawn)value2).ItemServices;
		_savedHelmet[player] = itemServices != null && new CCSPlayer_ItemServices(((NativeObject)itemServices).Handle).HasHelmet;
		CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value2).WeaponServices;
		if (((weaponServices != null) ? weaponServices.MyWeapons : null) != null)
		{
			List<string> list = new List<string>();
			foreach (CHandle<CBasePlayerWeapon> item in ((IEnumerable<CHandle<CBasePlayerWeapon>>)weaponServices.MyWeapons).ToList())
			{
				if ((CEntityInstance)(object)item?.Value != (CEntityInstance)null && ((CEntityInstance)item.Value).IsValid && !string.IsNullOrEmpty(((CEntityInstance)item.Value).DesignerName))
				{
					list.Add(((CEntityInstance)item.Value).DesignerName);
				}
			}
			_savedWeapons[player] = list;
		}
		player.RemoveWeapons();
		MoveLockManager.Lock(player, "Gargoyle");
		((CBaseModelEntity)value2).Render = Color.FromArgb(255, 140, 140, 150);
		Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseModelEntity", "m_clrRender", 0);
		player.PrintToCenterAlert("\ud83d\uddff 石像形态！3秒免疫子弹，攻击者-10HP！");
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
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
		if (_petrifyEndTime.TryGetValue(val, out var value2) && Server.CurrentTime < value2)
		{
			CHandle<CBaseEntity> attacker = info.Attacker;
			object obj3;
			if (attacker == null)
			{
				obj3 = null;
			}
			else
			{
				CBaseEntity value3 = attacker.Value;
				if (value3 == null)
				{
					obj3 = null;
				}
				else
				{
					CCSPlayerPawn obj4 = ((NativeObject)value3).As<CCSPlayerPawn>();
					if (obj4 == null)
					{
						obj3 = null;
					}
					else
					{
						CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
						if (controller2 == null)
						{
							obj3 = null;
						}
						else
						{
							CBasePlayerController value4 = controller2.Value;
							obj3 = ((value4 != null) ? ((NativeObject)value4).As<CCSPlayerController>() : null);
						}
					}
				}
			}
			CCSPlayerController val2 = (CCSPlayerController)obj3;
			if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid && (CEntityInstance)(object)val2 != (CEntityInstance)(object)val && (CEntityInstance)(object)val2.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)val2.PlayerPawn.Value).IsValid && ((CBaseEntity)val2.PlayerPawn.Value).LifeState == 0)
			{
				CCSPlayerPawn value5 = val2.PlayerPawn.Value;
				((CBaseEntity)value5).Health -= 10;
				Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseEntity", "m_iHealth", 0);
				if (((CBaseEntity)value5).Health <= 0)
				{
					if (!val2.IsBot && !((CBasePlayerController)val2).IsHLTV)
					{
						((CBasePlayerPawn)value5).CommitSuicide(false, true);
					}
					else
					{
						try
						{
							((CBasePlayerPawn)value5).CommitSuicide(false, true);
						}
						catch
						{
							((CBaseEntity)value5).Health = 0;
							Utilities.SetStateChanged((CBaseEntity)(object)value5, "CBaseEntity", "m_iHealth", 0);
						}
					}
				}
			}
			if (((uint)info.BitsDamageType & 2u) != 0)
			{
				info.Damage = 0f;
				return (HookResult)1;
			}
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_petrifyEndTime.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<CCSPlayerController, float> item in _petrifyEndTime.ToList())
		{
			CCSPlayerController key = item.Key;
			if (num >= item.Value)
			{
				Unpetrify(key);
				_petrifyEndTime.Remove(key);
				if (key != null)
				{
					key.PrintToCenterAlert("\ud83d\uddff 石化解除了!");
				}
			}
			else
			{
				MoveLockManager.Lock(key, "Gargoyle");
			}
		}
	}
}
