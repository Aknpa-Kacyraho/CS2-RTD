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

public class HotPotato : DiceBlueprint
{
	private float _lastDamageTime;

	public override string ClassName => "HotPotato";

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

	public HotPotato(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			if (DiceSynergy.HasPartner(player, "C4Expert"))
			{
				DiceSynergy.AnnounceCombo(player, "炸弹专家", "热土豆灼烧免疫，持包者额外 99% 减伤！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_lastDamageTime = 0f;
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
		if (num - _lastDamageTime < 1f)
		{
			return;
		}
		_lastDamageTime = num;
		int damage = _config.Dices.HotPotato.Damage;
		float radius = _config.Dices.HotPotato.Radius;
		Vector val = null;
		CPlantedC4 val2 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault((CPlantedC4 c) => ((CEntityInstance)c).IsValid && ((CBaseEntity)c).AbsOrigin != null);
		if ((CEntityInstance)(object)val2 != (CEntityInstance)null)
		{
			val = ((CBaseEntity)val2).AbsOrigin;
		}
		else
		{
			List<CBasePlayerWeapon> list = (from c in Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>("weapon_c4")
				where ((CEntityInstance)c).IsValid && ((CBaseEntity)c).AbsOrigin != null && (CEntityInstance)(object)((CBaseEntity)c).OwnerEntity?.Value == (CEntityInstance)null
				select c).ToList();
			if (list.Count > 0)
			{
				val = ((CBaseEntity)list[0]).AbsOrigin;
			}
		}
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			if (DiceSynergy.HasPartner(item, "C4Expert"))
			{
				continue;
			}
			CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)item.PlayerPawn.Value).WeaponServices;
			NetworkedVector<CHandle<CBasePlayerWeapon>> val3 = ((weaponServices != null) ? weaponServices.MyWeapons : null);
			if (val3 == null)
			{
				continue;
			}
			foreach (CHandle<CBasePlayerWeapon> item2 in val3)
			{
				object obj;
				if (item2 == null)
				{
					obj = null;
				}
				else
				{
					CBasePlayerWeapon value = item2.Value;
					obj = ((value != null) ? ((CEntityInstance)value).DesignerName : null);
				}
				if (!((string?)obj == "weapon_c4"))
				{
					continue;
				}
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				((CBaseEntity)value2).Health -= damage;
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				item.PrintToCenterAlert("\ud83d\udd25 C4烫手！-" + damage + "HP");
				if (((CBaseEntity)value2).Health > 0)
				{
					break;
				}
				if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
					break;
				}
				try
				{
					((CBasePlayerPawn)value2).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)value2).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
				}
				break;
			}
		}
		if (val == null)
		{
			return;
		}
		foreach (CCSPlayerController item3 in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
			select p)
		{
			if (DiceSynergy.HasPartner(item3, "C4Expert"))
			{
				continue;
			}
			CCSPlayerPawn value3 = item3.PlayerPawn.Value;
			float distance = Vectors.GetDistance(val, ((CBaseEntity)value3).AbsOrigin);
			if (!(distance <= radius))
			{
				continue;
			}
			((CBaseEntity)value3).Health -= damage;
			Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			if (((CBaseEntity)value3).Health > 0)
			{
				continue;
			}
			if (!item3.IsBot && !((CBasePlayerController)item3).IsHLTV)
			{
				((CBasePlayerPawn)value3).CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerPawn)value3).CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)value3).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
			}
		}
	}
}
