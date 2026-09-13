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

public class Drone : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, CDynamicProp> _droneProps = new Dictionary<CCSPlayerController, CDynamicProp>();

	private readonly Dictionary<CCSPlayerController, CBeam> _tetherBeams = new Dictionary<CCSPlayerController, CBeam>();

	private readonly Dictionary<CCSPlayerController, float> _orbitAngles = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _lastFireTime = new Dictionary<CCSPlayerController, float>();

	private readonly List<CBeam> _shotBeams = new List<CBeam>();

	public override string ClassName => "Drone";

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

	public Drone(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "RepulsionField"))
			{
				DiceSynergy.AnnounceCombo(player, "无人防线", "斥力半径翻倍");
			}
			if (DiceSynergy.HasPartner(player, "Satellite"))
			{
				DiceSynergy.AnnounceCombo(player, "天网", "无人机伤害+50%！卫星浮空！");
			}
			_orbitAngles[player] = (float)(_random.NextDouble() * 3.1415927410125732 * 2.0);
			_lastFireTime[player] = 0f;
			SpawnDrone(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DestroyDrone(player);
		_players.Remove(player);
		_orbitAngles.Remove(player);
		_lastFireTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DestroyDrone(item);
		}
		_players.Clear();
		_orbitAngles.Clear();
		_lastFireTime.Clear();
	}

	public override void Destroy()
	{
		foreach (CBeam item in _shotBeams.ToList())
		{
			if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid)
			{
				((CEntityInstance)item).Remove();
			}
		}
		_shotBeams.Clear();
		Reset();
	}

	private void SpawnDrone(CCSPlayerController player)
	{
		DestroyDrone(player);
		CDynamicProp val = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return;
		}
		((CBaseModelEntity)val).SetModel("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
		((CBaseModelEntity)val).Collision.SolidType = (SolidType_t)6;
		CBodyComponent cBodyComponent = ((CBaseEntity)val).CBodyComponent;
		object obj;
		if (cBodyComponent == null)
		{
			obj = null;
		}
		else
		{
			CGameSceneNode sceneNode = cBodyComponent.SceneNode;
			if (sceneNode == null)
			{
				obj = null;
			}
			else
			{
				CEntityInstance owner = sceneNode.Owner;
				obj = ((owner != null) ? owner.Entity : null);
			}
		}
		if (obj != null)
		{
			((CBaseEntity)val).CBodyComponent.SceneNode.Owner.Entity.Flags &= 4294967291u;
		}
		((CBaseEntity)val).DispatchSpawn();
		((CBaseModelEntity)val).Render = Color.FromArgb(255, 100, 220, 255);
		Utilities.SetStateChanged((CBaseEntity)(object)val, "CBaseModelEntity", "m_clrRender", 0);
		CBodyComponent cBodyComponent2 = ((CBaseEntity)val).CBodyComponent;
		object obj2;
		if (cBodyComponent2 == null)
		{
			obj2 = null;
		}
		else
		{
			CGameSceneNode sceneNode2 = cBodyComponent2.SceneNode;
			obj2 = ((sceneNode2 != null) ? sceneNode2.GetSkeletonInstance() : null);
		}
		CSkeletonInstance val2 = (CSkeletonInstance)obj2;
		if (val2 != null)
		{
			((CGameSceneNode)val2).Scale = 0.5f;
		}
		_droneProps[player] = val;
		CBeam val3 = Utilities.CreateEntityByName<CBeam>("beam");
		if ((CEntityInstance)(object)val3 != (CEntityInstance)null)
		{
			((CBaseModelEntity)val3).Render = Color.FromArgb(80, 80, 200, 255);
			val3.Width = 0.8f;
			((CBaseEntity)val3).DispatchSpawn();
			_tetherBeams[player] = val3;
		}
	}

	private void DestroyDrone(CCSPlayerController player)
	{
		if (_droneProps.TryGetValue(player, out CDynamicProp value))
		{
			if ((CEntityInstance)(object)value != (CEntityInstance)null && ((CEntityInstance)value).IsValid)
			{
				((CEntityInstance)value).Remove();
			}
			_droneProps.Remove(player);
		}
		if (_tetherBeams.TryGetValue(player, out CBeam value2))
		{
			if ((CEntityInstance)(object)value2 != (CEntityInstance)null && ((CEntityInstance)value2).IsValid)
			{
				((CEntityInstance)value2).Remove();
			}
			_tetherBeams.Remove(player);
		}
	}

	public void OnTick()
	{
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Expected O, but got Unknown
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Expected O, but got Unknown
		//IL_02d8: Expected O, but got Unknown
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Expected O, but got Unknown
		//IL_038c: Expected O, but got Unknown
		//IL_038c: Expected O, but got Unknown
		//IL_05c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f1: Expected O, but got Unknown
		//IL_05f1: Expected O, but got Unknown
		float num = Server.CurrentTime;
		for (int num2 = _shotBeams.Count - 1; num2 >= 0; num2--)
		{
			CBeam val = _shotBeams[num2];
			if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid)
			{
				_shotBeams.RemoveAt(num2);
			}
			else
			{
				val.Width -= Server.TickInterval * 8f;
				if (val.Width <= 0.1f)
				{
					((CEntityInstance)val).Remove();
					_shotBeams.RemoveAt(num2);
				}
			}
		}
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
				{
					continue;
				}
				CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
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
				if (obj == null || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0 || !_droneProps.TryGetValue(player, out CDynamicProp value2) || (CEntityInstance)(object)value2 == (CEntityInstance)null || !((CEntityInstance)value2).IsValid)
				{
					continue;
				}
				CCSPlayerPawn value3 = player.PlayerPawn.Value;
				Vector absOrigin = ((CBaseEntity)value3).AbsOrigin;
				float orbitRadius = _config.Dices.Drone.OrbitRadius;
				float orbitHeight = _config.Dices.Drone.OrbitHeight;
				float orbitSpeed = _config.Dices.Drone.OrbitSpeed;
				float num3 = _orbitAngles[player] + orbitSpeed * Server.TickInterval;
				_orbitAngles[player] = num3;
				Vector val2 = new Vector((float?)(absOrigin.X + MathF.Cos(num3) * orbitRadius), (float?)(absOrigin.Y + MathF.Sin(num3) * orbitRadius), (float?)(absOrigin.Z + orbitHeight));
				((CBaseEntity)value2).Teleport(val2, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
				if (_tetherBeams.TryGetValue(player, out CBeam value4) && (CEntityInstance)(object)value4 != (CEntityInstance)null && ((CEntityInstance)value4).IsValid)
				{
					((CBaseEntity)value4).Teleport(new Vector((float?)absOrigin.X, (float?)absOrigin.Y, (float?)(absOrigin.Z + 64f)), new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
					value4.EndPos.X = val2.X;
					value4.EndPos.Y = val2.Y;
					value4.EndPos.Z = val2.Z;
				}
				float fireRate = _config.Dices.Drone.FireRate;
				if (!_lastFireTime.TryGetValue(player, out var value5) || num - value5 < fireRate)
				{
					continue;
				}
				float attackRange = _config.Dices.Drone.AttackRange;
				CCSPlayerController val3 = null;
				float num4 = attackRange;
				foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0 && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).AbsOrigin != null
					select p)
				{
					float num5 = ((CBaseEntity)((CBasePlayerController)item).Pawn.Value).AbsOrigin.X - val2.X;
					float num6 = ((CBaseEntity)((CBasePlayerController)item).Pawn.Value).AbsOrigin.Y - val2.Y;
					float num7 = ((CBaseEntity)((CBasePlayerController)item).Pawn.Value).AbsOrigin.Z - val2.Z;
					float num8 = MathF.Sqrt(num5 * num5 + num6 * num6 + num7 * num7);
					if (num8 < num4)
					{
						num4 = num8;
						val3 = item;
					}
				}
				if (!((CEntityInstance)(object)val3 != (CEntityInstance)null))
				{
					continue;
				}
				_lastFireTime[player] = num;
				Vector absOrigin2 = ((CBaseEntity)((CBasePlayerController)val3).Pawn.Value).AbsOrigin;
				CBeam val4 = Utilities.CreateEntityByName<CBeam>("beam");
				if ((CEntityInstance)(object)val4 != (CEntityInstance)null)
				{
					((CBaseModelEntity)val4).Render = Color.FromArgb(255, 255, 60, 60);
					val4.Width = 2f;
					((CBaseEntity)val4).Teleport(val2, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
					val4.EndPos.X = absOrigin2.X;
					val4.EndPos.Y = absOrigin2.Y;
					val4.EndPos.Z = absOrigin2.Z;
					((CBaseEntity)val4).DispatchSpawn();
					_shotBeams.Add(val4);
				}
			int num9 = _random.Next(_config.Dices.Drone.DamageMin, _config.Dices.Drone.DamageMax + 1);
			if (DiceSynergy.HasPartner(player, "RepulsionField") || DiceSynergy.HasPartner(player, "Satellite"))
			{
				num9 = (int)float.Round((float)num9 * 1.5f);
			}
				CCSPlayerPawn value6 = val3.PlayerPawn.Value;
				((CBaseEntity)value6).Health -= num9;
				Utilities.SetStateChanged((CBaseEntity)(object)value6, "CBaseEntity", "m_iHealth", 0);
				if (((CBaseEntity)value6).Health <= 0)
				{
					if (!val3.IsBot && !((CBasePlayerController)val3).IsHLTV)
					{
						((CBasePlayerPawn)value6).CommitSuicide(false, true);
					}
					else
					{
						try
						{
							((CBasePlayerPawn)value6).CommitSuicide(false, true);
						}
						catch
						{
							((CBaseEntity)value6).Health = 0;
							Utilities.SetStateChanged((CBaseEntity)(object)value6, "CBaseEntity", "m_iHealth", 0);
						}
					}
				}
				player.PrintToCenterAlert($"\ud83d\udd2b -{num9} → {((CBasePlayerController)val3).PlayerName}");
			}
			catch
			{
			}
		}
	}
}
