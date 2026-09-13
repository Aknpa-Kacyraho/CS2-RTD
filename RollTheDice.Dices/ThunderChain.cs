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

public class ThunderChain : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "ThunderChain";

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

	public ThunderChain(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "LaserCage");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "雷光炼狱", "雷霆+2次弹射");
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
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Expected O, but got Unknown
		CCSPlayerController attacker = @event.Attacker;
		CCSPlayerController userid = @event.Userid;
		if (!((CEntityInstance)(object)attacker == (CEntityInstance)null) && ((CEntityInstance)attacker).IsValid && !((CBasePlayerController)attacker).IsHLTV && _players.Contains(attacker) && !((CEntityInstance)(object)userid == (CEntityInstance)null) && ((CEntityInstance)userid).IsValid && !((CEntityInstance)(object)attacker == (CEntityInstance)(object)userid))
		{
			CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
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
			if (obj != null)
			{
				Vector val = new Vector((float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Y, (float?)(((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Z + 32f));
				float chainRange = _config.Dices.ThunderChain.ChainRange;
				int num = _random.Next(_config.Dices.ThunderChain.ChainMin, _config.Dices.ThunderChain.ChainMax + ((!DiceSynergy.HasPartner(attacker, "LaserCage")) ? 1 : 3));
				List<CCSPlayerController> list = new List<CCSPlayerController>();
				foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)attacker && ((CBaseEntity)p).TeamNum != ((CBaseEntity)attacker).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
					select p)
				{
					float distance = Vectors.GetDistance(val, ((CBaseEntity)item2.PlayerPawn.Value).AbsOrigin);
					if (distance <= chainRange * 2f)
					{
						list.Add(item2);
					}
				}
				if (list.Count == 0)
				{
					return (HookResult)0;
				}
				HashSet<CCSPlayerController> hashSet = new HashSet<CCSPlayerController>();
				List<(CCSPlayerController, int, Vector)> list2 = new List<(CCSPlayerController, int, Vector)>();
				Vector a = val;
				int num2 = _random.Next(_config.Dices.ThunderChain.InitialDamageMin, _config.Dices.ThunderChain.InitialDamageMax + 1);
				float damageDecay = _config.Dices.ThunderChain.DamageDecay;
				for (int num3 = 0; num3 < num; num3++)
				{
					CCSPlayerController val2 = null;
					float num4 = chainRange;
					foreach (CCSPlayerController item3 in list)
					{
						if (hashSet.Contains(item3))
						{
							continue;
						}
						CHandle<CCSPlayerPawn> playerPawn2 = item3.PlayerPawn;
						object obj2;
						if (playerPawn2 == null)
						{
							obj2 = null;
						}
						else
						{
							CCSPlayerPawn value2 = playerPawn2.Value;
							obj2 = ((value2 != null) ? ((CBaseEntity)value2).AbsOrigin : null);
						}
						if (obj2 != null)
						{
							float distance2 = Vectors.GetDistance(a, ((CBaseEntity)item3.PlayerPawn.Value).AbsOrigin);
							if (distance2 < num4)
							{
								num4 = distance2;
								val2 = item3;
							}
						}
					}
					if ((CEntityInstance)(object)val2 == (CEntityInstance)null)
					{
						break;
					}
					hashSet.Add(val2);
					int item = Math.Max(10, (int)((float)num2 * MathF.Pow(1f - damageDecay, num3)));
					Vector val3 = new Vector((float?)((CBaseEntity)val2.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)val2.PlayerPawn.Value).AbsOrigin.Y, (float?)(((CBaseEntity)val2.PlayerPawn.Value).AbsOrigin.Z + 32f));
					list2.Add((val2, item, val3));
					a = val3;
				}
				if (list2.Count == 0)
				{
					return (HookResult)0;
				}
				Vector val4 = val;
				int count = list2.Count;
				for (int num5 = 0; num5 < count; num5++)
				{
					float num6 = (float)num5 * 0.3f;
					(CCSPlayerController, int, Vector) tuple = list2[num5];
					CCSPlayerController target = tuple.Item1;
					int dmg = tuple.Item2;
					Vector hitPos = tuple.Item3;
					Vector fromPos = val4;
					val4 = hitPos;
					Action applyDamage = delegate
					{
						if (!((CEntityInstance)(object)target == (CEntityInstance)null) && ((CEntityInstance)target).IsValid && !((CEntityInstance)(object)target.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)target.PlayerPawn.Value).IsValid && ((CBaseEntity)target.PlayerPawn.Value).LifeState == 0)
						{
							CCSPlayerPawn value3 = target.PlayerPawn.Value;
							((CBaseEntity)value3).Health = Math.Max(0, ((CBaseEntity)value3).Health - dmg);
							Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
							target.PrintToCenterAlert($"⚡ 雷劫 -{dmg} HP!");
							if (((CBaseEntity)value3).Health <= 0)
							{
								if (target.IsBot || ((CBasePlayerController)target).IsHLTV)
								{
									try
									{
										((CBasePlayerPawn)value3).CommitSuicide(false, true);
										return;
									}
									catch
									{
										((CBaseEntity)value3).Health = 0;
										Utilities.SetStateChanged((CBaseEntity)(object)value3, "CBaseEntity", "m_iHealth", 0);
										return;
									}
								}
								((CBasePlayerPawn)value3).CommitSuicide(false, true);
							}
						}
					};
					if (num6 <= 0f)
					{
						Server.NextFrame((Action)delegate
						{
							applyDamage();
							try
							{
								SpawnLightningVisuals(fromPos, hitPos);
							}
							catch
							{
							}
						});
						continue;
					}
					new Timer(num6, (Action)delegate
					{
						applyDamage();
						try
						{
							SpawnLightningVisuals(fromPos, hitPos);
						}
						catch
						{
						}
					}, (TimerFlags?)null);
				}
				attacker.PrintToCenterAlert($"⚡ 雷劫连锁 x{list2.Count}!");
				return (HookResult)0;
			}
		}
		return (HookResult)0;
	}

	private static void SpawnLightningVisuals(Vector fromPos, Vector hitPos)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_00ba: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected O, but got Unknown
		//IL_01b8: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
		if ((CEntityInstance)(object)beam != (CEntityInstance)null && ((CEntityInstance)beam).IsValid)
		{
			((CBaseModelEntity)beam).Render = Color.FromArgb(255, 100, 200, 255);
			beam.Width = 3f;
			((CBaseEntity)beam).Teleport(fromPos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			beam.EndPos.X = hitPos.X;
			beam.EndPos.Y = hitPos.Y;
			beam.EndPos.Z = hitPos.Z;
			((CBaseEntity)beam).DispatchSpawn();
			new Timer(0.5f, (Action)delegate
			{
				if ((CEntityInstance)(object)beam != (CEntityInstance)null && ((CEntityInstance)beam).IsValid)
				{
					((CEntityInstance)beam).Remove();
				}
			}, (TimerFlags?)null);
		}
		CBaseEntity boom = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
		if (!((CEntityInstance)(object)boom != (CEntityInstance)null) || !((CEntityInstance)boom).IsValid)
		{
			return;
		}
		boom.Teleport(hitPos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
		boom.DispatchSpawn();
		((CEntityInstance)boom).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
		new Timer(0.5f, (Action)delegate
		{
			if ((CEntityInstance)(object)boom != (CEntityInstance)null && ((CEntityInstance)boom).IsValid)
			{
				((CEntityInstance)boom).AcceptInput("Kill", (CEntityInstance)null, (CEntityInstance)null, "", 0);
			}
		}, (TimerFlags?)null);
	}
}
