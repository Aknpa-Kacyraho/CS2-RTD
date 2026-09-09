using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class SmokeBomb : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, bool> _usedThisLife = new Dictionary<CCSPlayerController, bool>();

	public override string ClassName => "SmokeBomb";

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

	public SmokeBomb(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "SmokeVision");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "烟雾掌控", "隐形持续翻倍");
			}
			_usedThisLife[player] = false;
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
		_usedThisLife.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_usedThisLife.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
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
		bool value2 = default(bool);
		if (((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_usedThisLife.TryGetValue(val, out value2)) | value2)
		{
			return (HookResult)0;
		}
		float hpThresholdPercent = _config.Dices.SmokeBomb.HpThresholdPercent;
		int num = entity.Health - (int)float.Round(info.Damage);
		if (num > (int)((float)entity.MaxHealth * hpThresholdPercent))
		{
			return (HookResult)0;
		}
		info.Damage = ((entity.Health - 1 > 0) ? (entity.Health - 1) : 0);
		_usedThisLife[val] = true;
		bool comboActive = DiceSynergy.HasPartner(val, "SmokeVision");
		float num2 = comboActive ? (_config.Dices.SmokeBomb.InvisibilitySeconds * 2f) : _config.Dices.SmokeBomb.InvisibilitySeconds;
		CCSPlayerController capturedVictim = val;
		Server.NextFrame((Action)delegate
		{
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Expected O, but got Unknown
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Expected O, but got Unknown
			//IL_014d: Expected O, but got Unknown
			CCSPlayerController obj3 = capturedVictim;
			CCSPlayerPawn val2 = ((obj3 == null) ? null : obj3.PlayerPawn?.Value);
			if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid)
			{
				((CBaseModelEntity)val2).Render = Color.FromArgb(15, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseModelEntity", "m_clrRender", 0);
			}
			if (((val2 != null) ? ((CBaseEntity)val2).AbsOrigin : null) != null)
			{
				Vector val3 = new Vector((float?)((CBaseEntity)val2).AbsOrigin.X, (float?)((CBaseEntity)val2).AbsOrigin.Y, (float?)(((CBaseEntity)val2).AbsOrigin.Z + 5f));
				CSmokeGrenadeProjectile smoke = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>("smokegrenade_projectile");
				if ((CEntityInstance)(object)smoke != (CEntityInstance)null && ((CEntityInstance)smoke).IsValid)
				{
					((CBaseEntity)smoke).Teleport(val3, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
					((CBaseEntity)smoke).DispatchSpawn();
					smoke.SmokeColor.X = 200f;
					smoke.SmokeColor.Y = 200f;
					smoke.SmokeColor.Z = 200f;
					CCSPlayerController obj4 = capturedVictim;
					nint? obj5;
					if (obj4 == null)
					{
						obj5 = null;
					}
					else
					{
						CHandle<CCSPlayerPawn> playerPawn = obj4.PlayerPawn;
						if (playerPawn == null)
						{
							obj5 = null;
						}
						else
						{
							CCSPlayerPawn value3 = playerPawn.Value;
							obj5 = ((value3 != null) ? new nint?(((NativeEntity)value3).Handle) : ((nint?)null));
						}
					}
					nint? num3 = obj5;
					if (num3.HasValue)
					{
						Entities.SetSchemaValue((CBaseEntity)(object)smoke, "CBaseGrenade", "m_hThrower", num3.Value);
					}
					Entities.SetSchemaValue<Vector>((CBaseEntity)(object)smoke, "CSmokeGrenadeProjectile", "m_vSmokeDetonationPos", val3);
					((CBaseGrenade)smoke).DetonateTime = 0f;
					((CEntityInstance)smoke).AcceptInput("InitializeSpawnFromWorld", (CEntityInstance)null, (CEntityInstance)null, "", 0);
					((CEntityInstance)smoke).AcceptInput("Detonate", (CEntityInstance)null, (CEntityInstance)null, "", 0);
					Server.NextFrame((Action)delegate
					{
						if ((CEntityInstance)(object)smoke != (CEntityInstance)null && ((CEntityInstance)smoke).IsValid)
						{
							((CBaseGrenade)smoke).DetonateTime = 0f;
							((CEntityInstance)smoke).AcceptInput("Detonate", (CEntityInstance)null, (CEntityInstance)null, "", 0);
						}
					});
				}
			}
			CCSPlayerController obj6 = capturedVictim;
			if (obj6 != null)
			{
				obj6.PrintToCenterAlert($"\ud83d\udca8 迷雾逃生！隐身{num2:F0}秒！");
			}
		});
		new Timer(num2, (Action)delegate
		{
			CCSPlayerController obj3 = capturedVictim;
			CCSPlayerPawn val2 = ((obj3 == null) ? null : obj3.PlayerPawn?.Value);
			if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid)
			{
				((CBaseModelEntity)val2).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseModelEntity", "m_clrRender", 0);
			}
		}, (TimerFlags?)null);
		return (HookResult)1;
	}
}
