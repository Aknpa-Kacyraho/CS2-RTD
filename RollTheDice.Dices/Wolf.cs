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

public class Wolf : DiceBlueprint
{
	private float _roundStartTime;

	private readonly HashSet<ulong> _bonusGranted = new HashSet<ulong>();

	public override string ClassName => "Wolf";

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

	public Wolf(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (_roundStartTime == 0f)
			{
				_roundStartTime = Server.CurrentTime;
			}
			_bonusGranted.Remove(((CBasePlayerController)player).SteamID);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			if (DiceSynergy.HasPartner(player, "WolfKing"))
			{
				DiceSynergy.AnnounceCombo(player, "月下狼群", "每只狼的加成额外 +1 档！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		DamageBonusManager.Unregister(player, "Wolf");
		SpeedBonusManager.Unregister(player, "Wolf");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "Wolf");
			SpeedBonusManager.Unregister(item, "Wolf");
		}
		_players.Clear();
		_roundStartTime = 0f;
		_bonusGranted.Clear();
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
		if (num - _roundStartTime >= _config.Dices.Wolf.BonusDelay)
		{
			ApplyWolfPackBonuses();
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				float effective = SpeedBonusManager.GetEffective(item);
				item.PlayerPawn.Value.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	private void ApplyWolfPackBonuses()
	{
		foreach (CCSPlayerController player in _players.ToList())
		{
			if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && !_bonusGranted.Contains(((CBasePlayerController)player).SteamID))
			{
				int num = _players.Count((CCSPlayerController p) => ((CEntityInstance)p).IsValid && ((CBaseEntity)p).TeamNum == ((CBaseEntity)player).TeamNum);
				if (DiceSynergy.HasPartner(player, "WolfKing"))
				{
					num++;
				}
				int num2 = _config.Dices.Wolf.HpPerWolf * num;
				int num3 = _config.Dices.Wolf.ArmorPerWolf * num;
				float num4 = _config.Dices.Wolf.DamagePerWolf * (float)num;
				float num5 = _config.Dices.Wolf.SpeedPerWolf * (float)num;
				CCSPlayerPawn value = player.PlayerPawn.Value;
				((CBaseEntity)value).MaxHealth += num2;
				((CBaseEntity)value).Health += num2;
				value.ArmorValue = Math.Min(value.ArmorValue + num3, 100);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)value, "CCSPlayerPawn", "m_ArmorValue", 0);
				DamageBonusManager.Register(player, "Wolf", num4);
				SpeedBonusManager.Register(player, "Wolf", num5);
				_bonusGranted.Add(((CBasePlayerController)player).SteamID);
				player.PrintToCenterAlert($"\ud83d\udc3a 狼群之力！+{num2}HP +{num3}甲 +{(int)(num4 * 100f)}%伤害 +{(int)(num5 * 100f)}%速度！({num}只狼)");
			}
		}
	}
}
