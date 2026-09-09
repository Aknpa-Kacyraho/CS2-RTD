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

namespace RollTheDice.Dices;

public class DecoyDummy : DiceBlueprint
{
	private readonly Dictionary<CCSPlayerController, float> _nextGrenadeTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, List<CDynamicProp>> _activeDummies = new Dictionary<CCSPlayerController, List<CDynamicProp>>();

	public override string ClassName => "DecoyDummy";

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

	public DecoyDummy(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_nextGrenadeTime[player] = Server.CurrentTime + _config.Dices.DecoyDummy.GrenadeInterval;
			player.GiveNamedItem("weapon_decoy");
			player.PrintToCenterAlert("\ud83e\ude86 获得诱饵弹！每20秒补一颗！");
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
		_nextGrenadeTime.Remove(player);
		CleanupDummies(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			CleanupDummies(item);
		}
		_players.Clear();
		_nextGrenadeTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void CleanupDummies(CCSPlayerController player)
	{
		if (!_activeDummies.TryGetValue(player, out List<CDynamicProp> value))
		{
			return;
		}
		foreach (CDynamicProp item in value)
		{
			try
			{
				if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid)
				{
					((CEntityInstance)item).Remove();
				}
			}
			catch
			{
			}
		}
		_activeDummies.Remove(player);
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
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Expected O, but got Unknown
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			if (!(entity == (CEntityInstance)null) && entity.IsValid)
			{
				CDecoyProjectile val = new CDecoyProjectile(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid && ((CBaseEntity)val).AbsOrigin != null)
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
							Vector pos = new Vector((float?)((CBaseEntity)val).AbsOrigin.X, (float?)((CBaseEntity)val).AbsOrigin.Y, (float?)(((CBaseEntity)val).AbsOrigin.Z + 72f));
							CCSPlayerController captured = val3;
							new Timer(1.5f, (Action)delegate
							{
								//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
								//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
								//IL_00d4: Expected O, but got Unknown
								//IL_00d4: Expected O, but got Unknown
								//IL_0176: Unknown result type (might be due to invalid IL or missing references)
								if (!((CEntityInstance)(object)captured == (CEntityInstance)null) && ((CEntityInstance)captured).IsValid && pos != null)
								{
									CDynamicProp val4 = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
									if (!((CEntityInstance)(object)val4 == (CEntityInstance)null))
									{
										((CBaseModelEntity)val4).SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
										((CBaseModelEntity)val4).Render = Color.FromArgb(200, 100, 200, 255);
										((CBaseEntity)val4).Teleport(pos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
										((CBaseEntity)val4).DispatchSpawn();
										if (!_activeDummies.ContainsKey(captured))
										{
											_activeDummies[captured] = new List<CDynamicProp>();
										}
										_activeDummies[captured].Add(val4);
										captured.PrintToCenterAlert("\ud83e\ude86 假人诱饵已部署！");
										CDynamicProp capturedDummy = val4;
										new Timer(8f, (Action)delegate
										{
											try
											{
												if ((CEntityInstance)(object)capturedDummy != (CEntityInstance)null && ((CEntityInstance)capturedDummy).IsValid)
												{
													((CEntityInstance)capturedDummy).Remove();
												}
											}
											catch
											{
											}
											if (_activeDummies.TryGetValue(captured, out List<CDynamicProp> value2))
											{
												value2.Remove(capturedDummy);
											}
										}, (TimerFlags?)null);
									}
								}
							}, (TimerFlags?)null);
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
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0 && _nextGrenadeTime.TryGetValue(item, out var value) && num >= value)
			{
				_nextGrenadeTime[item] = num + _config.Dices.DecoyDummy.GrenadeInterval;
				item.GiveNamedItem("weapon_decoy");
				item.PrintToCenterAlert("\ud83e\ude86 补给了一颗诱饵弹！");
			}
		}
	}
}
