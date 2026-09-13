using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class ImposterSyndrome : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextCheckTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _nextDecoyGrantTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, List<(CDynamicProp?, CDynamicProp?)>> _decoyGlows = new Dictionary<CCSPlayerController, List<(CDynamicProp, CDynamicProp)>>();

	public override string ClassName => "ImposterSyndrome";

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
			span[num2] = "OnEntitySpawned";
			return list;
		}
	}

	public ImposterSyndrome(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_nextCheckTime[player] = 0f;
			_nextDecoyGrantTime[player] = Server.CurrentTime + _config.Dices.ImposterSyndrome.DecoyInterval;
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
		_nextCheckTime.Remove(player);
		_nextDecoyGrantTime.Remove(player);
		CleanupDecoyGlows(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			CleanupDecoyGlows(item);
		}
		_players.Clear();
		_nextCheckTime.Clear();
		_nextDecoyGrantTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void CleanupDecoyGlows(CCSPlayerController player)
	{
		if (!_decoyGlows.TryGetValue(player, out List<(CDynamicProp, CDynamicProp)> value))
		{
			return;
		}
		foreach (var (glowProxy, glow) in value)
		{
			GlowUtil.RemoveGlow((CBaseEntity?)(object)glowProxy, (CBaseEntity?)(object)glow);
		}
		_decoyGlows.Remove(player);
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0 || entity.DesignerName != "decoy_projectile")
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Expected O, but got Unknown
			//IL_0215: Unknown result type (might be due to invalid IL or missing references)
			if (!(entity == (CEntityInstance)null) && entity.IsValid)
			{
				CDecoyProjectile val = new CDecoyProjectile(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid)
				{
					CCSPlayerPawn val2 = ((CBaseGrenade)val).Thrower?.Value;
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
							List<(CDynamicProp, CDynamicProp)> list = new List<(CDynamicProp, CDynamicProp)>();
							foreach (CCSPlayerController player in Utilities.GetPlayers())
							{
								if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player == (CEntityInstance)(object)val3) && ((CBaseEntity)player).TeamNum != ((CBaseEntity)val3).TeamNum && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CBaseEntity)player.PlayerPawn.Value).LifeState == 0)
								{
									(CDynamicProp, CDynamicProp) item = GlowUtil.CreateGlow((CBaseEntity)(object)player.PlayerPawn.Value, Color.Orange);
									list.Add(item);
								}
							}
							if (list.Count > 0)
							{
								_decoyGlows[val3] = list;
								val3.PrintToCenterAlert("\ud83d\udc41 诱饵弹暴露了敌人位置!");
							}
							CCSPlayerController capturedThrower = val3;
							new Timer(1f, (Action)delegate
							{
								CleanupDecoyGlows(capturedThrower);
							}, (TimerFlags?)null);
						}
					}
				}
			}
		});
	}

	public void OnTick()
	{
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item == (CEntityInstance)null || !((CEntityInstance)item).IsValid || (CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)item.PlayerPawn.Value).IsValid || ((CBaseEntity)item.PlayerPawn.Value).LifeState != 0)
				{
					continue;
				}
				if (_nextDecoyGrantTime.TryGetValue(item, out var value) && num >= value)
				{
					CCSPlayerPawn value2 = item.PlayerPawn.Value;
					bool flag = false;
					CPlayer_WeaponServices weaponServices = ((CBasePlayerPawn)value2).WeaponServices;
					NetworkedVector<CHandle<CBasePlayerWeapon>> val = ((weaponServices != null) ? weaponServices.MyWeapons : null);
					if (val != null)
					{
						foreach (CHandle<CBasePlayerWeapon> item2 in val)
						{
							CBasePlayerWeapon value3 = item2.Value;
							if (((value3 != null) ? ((CEntityInstance)value3).DesignerName : null) == "weapon_decoy")
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						item.GiveNamedItem("weapon_decoy");
						item.PrintToCenterAlert("\ud83c\udfaf 第六感：获得诱饵弹！");
					}
					_nextDecoyGrantTime[item] = num + _config.Dices.ImposterSyndrome.DecoyInterval;
				}
				if (_nextCheckTime.TryGetValue(item, out var value4) && value4 <= num)
				{
					_nextCheckTime[item] = num + 1f;
					CCSPlayerPawn value5 = item.PlayerPawn.Value;
					EntitySpottedState_t entitySpottedState = value5.EntitySpottedState;
					if (entitySpottedState != null && entitySpottedState.Spotted)
					{
						item.PrintToCenterAlert("\ud83d\udccd 你被雷达发现了!");
						((CBaseEntity)item).EmitSound("UI.PlayerPingUrgent", (RecipientFilter)null, 1f, 0f);
						_nextCheckTime[item] = num + 5f;
					}
				}
			}
			catch
			{
				_nextCheckTime.Remove(item);
				_nextDecoyGrantTime.Remove(item);
			}
		}
	}
}
