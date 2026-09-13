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

public class FireLord : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, float> _nextMolotovTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _lastHealTime = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "FireLord";

	public override List<string> Listeners
	{
		get
		{
			int num = 3;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerTakeDamagePre";
			num2++;
			span[num2] = "OnEntitySpawned";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public FireLord(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextMolotovTime[player] = Server.CurrentTime + _config.Dices.FireLord.MolotovInterval;
			_lastHealTime[player] = 0f;
			_comboActive = DiceSynergy.HasPartner(player, "Fireball");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "焚天灭地", "10%火焰反噬+炎魔火中15HP/s+30%伤害+每20s燃烧瓶！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DamageBonusManager.Unregister(player, "FireLord");
		_players.Remove(player);
		_nextMolotovTime.Remove(player);
		_lastHealTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DamageBonusManager.Unregister(item, "FireLord");
		}
		_players.Clear();
		_nextMolotovTime.Clear();
		_lastHealTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
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
		if (((uint)info.BitsDamageType & 8u) != 0)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		if (DiceSynergy.HasPartner(val, "Fireball"))
		{
			CHandle<CBaseEntity> attacker = info.Attacker;
			object obj3;
			if (attacker == null)
			{
				obj3 = null;
			}
			else
			{
				CBaseEntity value2 = attacker.Value;
				if (value2 == null)
				{
					obj3 = null;
				}
				else
				{
					CCSPlayerPawn obj4 = ((NativeObject)value2).As<CCSPlayerPawn>();
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
							CBasePlayerController value3 = controller2.Value;
							obj3 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
						}
					}
				}
			}
			CCSPlayerController val2 = (CCSPlayerController)obj3;
			if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid)
			{
				CCSPlayerPawn val3 = val2.PlayerPawn?.Value;
				if ((CEntityInstance)(object)val3 != (CEntityInstance)null && ((CEntityInstance)val3).IsValid && ((CBaseEntity)val3).LifeState == 0)
				{
					int num = (int)float.Round(info.Damage * 0.1f);
					if (num > 0)
					{
						((CBaseEntity)val3).Health -= num;
						Utilities.SetStateChanged((CBaseEntity)(object)val3, "CBaseEntity", "m_iHealth", 0);
						val2.PrintToCenterAlert($"\ud83d\udd25 火焰反噬 -{num}!");
						if (((CBaseEntity)val3).Health <= 0)
						{
							if (!val2.IsBot && !((CBasePlayerController)val2).IsHLTV)
							{
								((CBasePlayerPawn)val3).CommitSuicide(false, true);
							}
							else
							{
								try
								{
									((CBasePlayerPawn)val3).CommitSuicide(false, true);
								}
								catch
								{
									((CBaseEntity)val3).Health = 0;
									Utilities.SetStateChanged((CBaseEntity)(object)val3, "CBaseEntity", "m_iHealth", 0);
								}
							}
						}
					}
				}
			}
		}
		return (HookResult)0;
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0)
		{
			return;
		}
		bool flag;
		switch (entity.DesignerName)
		{
		case "hegrenade_projectile":
		case "flashbang_projectile":
		case "smokegrenade_projectile":
		case "decoy_projectile":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Expected O, but got Unknown
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_015a: Expected O, but got Unknown
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Expected O, but got Unknown
			if (!(entity == (CEntityInstance)null) && entity.IsValid)
			{
				CBaseGrenade val = new CBaseGrenade(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid && ((CBaseEntity)val).AbsOrigin != null)
				{
					CCSPlayerPawn val2 = val.OriginalThrower?.Value;
					if (!((CEntityInstance)(object)val2 == (CEntityInstance)null) && ((CEntityInstance)val2).IsValid)
					{
						CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)val2).Controller;
						object obj;
						if (controller == null)
						{
							obj = null;
						}
						else
						{
							CBasePlayerController value = controller.Value;
							obj = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
						}
						CCSPlayerController val3 = (CCSPlayerController)obj;
						if (!((CEntityInstance)(object)val3 == (CEntityInstance)null) && ((CEntityInstance)val3).IsValid && _players.Contains(val3))
						{
							Vector val4 = new Vector((float?)((CBaseEntity)val).AbsOrigin.X, (float?)((CBaseEntity)val).AbsOrigin.Y, (float?)((CBaseEntity)val).AbsOrigin.Z);
							Vector val5 = new Vector((float?)((CBaseEntity)val).AbsVelocity.X, (float?)((CBaseEntity)val).AbsVelocity.Y, (float?)((CBaseEntity)val).AbsVelocity.Z);
							((CEntityInstance)val).AcceptInput("Kill", (CEntityInstance)null, (CEntityInstance)null, "", 0);
							CMolotovProjectile molotov = Utilities.CreateEntityByName<CMolotovProjectile>("molotov_projectile");
							if (!((CEntityInstance)(object)molotov == (CEntityInstance)null))
							{
								((CBaseEntity)molotov).Teleport(val4, new QAngle((float?)0f, (float?)0f, (float?)0f), val5);
								Entities.SetSchemaValue((CBaseEntity)(object)molotov, "CBaseGrenade", "m_hThrower", ((NativeEntity)val2).Handle);
								((CBaseEntity)molotov).DispatchSpawn();
								((CBaseGrenade)molotov).DetonateTime = 0.01f;
								((CEntityInstance)molotov).AcceptInput("InitializeSpawnFromWorld", (CEntityInstance)null, (CEntityInstance)null, "", 0);
								Server.NextFrame((Action)delegate
								{
									if (!((CEntityInstance)(object)molotov == (CEntityInstance)null) && ((CEntityInstance)molotov).IsValid)
									{
										((CEntityInstance)molotov).AcceptInput("Detonate", (CEntityInstance)null, (CEntityInstance)null, "", 0);
									}
								});
							}
						}
					}
				}
			}
		});
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
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid)
				{
					continue;
				}
				CHandle<CCSPlayerPawn> playerPawn = item.PlayerPawn;
				object obj;
				if (playerPawn == null)
				{
					obj = null;
				}
				else
				{
					CCSPlayerPawn value = playerPawn.Value;
					obj = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
				}
				if (obj == null)
				{
					continue;
				}
				CCSPlayerPawn value2 = item.PlayerPawn.Value;
				if (((CBaseEntity)value2).LifeState != 0)
				{
					continue;
				}
				if (_nextMolotovTime.TryGetValue(item, out var value3) && num >= value3)
				{
					_nextMolotovTime[item] = num + _config.Dices.FireLord.MolotovInterval;
					item.GiveNamedItem("weapon_molotov");
					item.PrintToCenterAlert("\ud83d\udd25 炎魔获得了燃烧瓶！");
				}
				bool flag = false;
				IEnumerable<CBaseEntity> enumerable = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("inferno");
				foreach (CBaseEntity item2 in enumerable)
				{
					if (item2 != null && ((CEntityInstance)item2).IsValid && item2.AbsOrigin != null)
					{
						float distance = Vectors.GetDistance(((CBaseEntity)value2).AbsOrigin, item2.AbsOrigin);
						if (distance < 150f)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					if (_lastHealTime.TryGetValue(item, out var value4) && num - value4 >= 1f)
					{
						_lastHealTime[item] = num;
						int fireHealPerSec = _config.Dices.FireLord.FireHealPerSec;
						((CBaseEntity)value2).Health = Math.Min(((CBaseEntity)value2).Health + fireHealPerSec, ((CBaseEntity)value2).MaxHealth);
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					}
					DamageBonusManager.Register(item, "FireLord", _config.Dices.FireLord.FireDamageBonus);
				}
				else
				{
					DamageBonusManager.Unregister(item, "FireLord");
				}
			}
			catch
			{
			}
		}
	}
}
