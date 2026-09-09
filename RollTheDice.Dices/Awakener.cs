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

public class Awakener : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _killCount = new Dictionary<CCSPlayerController, int>();

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Awakener";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerDeath";
			return list;
		}
	}

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

	public Awakener(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Evolution");
			_killCount[player] = (_comboActive ? 1 : 0);
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "超进化", "超进化联动生效！初始+1击杀！");
			}
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			if (_comboActive)
			{
				((CBaseEntity)value).MaxHealth += 100;
				((CBaseEntity)value).Health += 100;
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			}
			ApplyStats(player, _killCount[player]);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			int killsToMax = _config.Dices.Awakener.KillsToMax;
			player.PrintToCenterAlert($"⚡ 觉醒者！击杀或助攻{killsToMax}人以觉醒全部力量...");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		Revert(player);
		_players.Remove(player);
		_killCount.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Revert(item);
		}
		_players.Clear();
		_killCount.Clear();
	}

	private void Revert(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			player.PlayerPawn.Value.VelocityModifier = 1f;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
		if (_originalMaxHealth.TryGetValue(player, out var value) && (CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseEntity)player.PlayerPawn.Value).MaxHealth = value;
			((CBaseEntity)player.PlayerPawn.Value).Health = Math.Min(((CBaseEntity)player.PlayerPawn.Value).Health, value);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth", 0);
		}
	}

	private void ApplyStats(CCSPlayerController player, int kills)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			int killsToMax = _config.Dices.Awakener.KillsToMax;
			float num = Math.Min((float)kills / (float)killsToMax, 1f);
			float num2 = _config.Dices.Awakener.StartSpeedMult + num * (_config.Dices.Awakener.MaxSpeedMult - _config.Dices.Awakener.StartSpeedMult);
			player.PlayerPawn.Value.VelocityModifier = num2;
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
		}
	}

	private float GetDamageMult(CCSPlayerController player)
	{
		int num = (_killCount.TryGetValue(player, out var value) ? value : 0);
		int killsToMax = _config.Dices.Awakener.KillsToMax;
		float num2 = Math.Min((float)num / (float)killsToMax, 1f);
		return _config.Dices.Awakener.StartDamageMult + num2 * (_config.Dices.Awakener.MaxDamageMult - _config.Dices.Awakener.StartDamageMult);
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		CheckKill(@event.Attacker);
		CheckKill(@event.Assister);
		return (HookResult)0;
	}

	private void CheckKill(CCSPlayerController? player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player))
		{
			int num = (_killCount.TryGetValue(player, out var value) ? value : 0);
			num++;
			_killCount[player] = num;
			ApplyStats(player, num);
			if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
			{
				CCSPlayerPawn value2 = player.PlayerPawn.Value;
				((CBaseEntity)value2).MaxHealth += 100;
				((CBaseEntity)value2).Health += 100;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			}
			int killsToMax = _config.Dices.Awakener.KillsToMax;
			if (num == 1)
			{
				player.PrintToCenterAlert($"⚡ 觉醒中... +100HP！({num}/{killsToMax})");
			}
			else if (num >= killsToMax)
			{
				player.PrintToCenterAlert($"⚡ 觉醒完成！伤害×{_config.Dices.Awakener.MaxDamageMult} 速度×{_config.Dices.Awakener.MaxSpeedMult}！+100HP");
			}
			else
			{
				player.PrintToCenterAlert($"⚡ 觉醒中... +100HP！({num}/{killsToMax})");
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		info.Damage *= GetDamageMult(val);
		return (HookResult)1;
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if (!((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
				{
					int kills = (_killCount.TryGetValue(item, out var value) ? value : 0);
					ApplyStats(item, kills);
				}
			}
			catch
			{
			}
		}
	}
}
