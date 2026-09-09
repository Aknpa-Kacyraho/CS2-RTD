using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Afterimage : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, List<(Vector Pos, QAngle Angles, Vector Velocity)>> _shadows = new Dictionary<CCSPlayerController, List<(Vector, QAngle, Vector)>>();

	private readonly Dictionary<CCSPlayerController, float> _nextRecordTime = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _recallCooldowns = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "Afterimage";

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
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public Afterimage(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_shadows[player] = new List<(Vector, QAngle, Vector)>();
			_nextRecordTime[player] = Server.CurrentTime + 1.5f;
			_recallCooldowns[player] = 0f;
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
		_shadows.Remove(player);
		_nextRecordTime.Remove(player);
		_recallCooldowns.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_shadows.Clear();
		_nextRecordTime.Clear();
		_recallCooldowns.Clear();
	}

	public void OnTick()
	{
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected O, but got Unknown
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (CCSPlayerController item4 in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)item4 == (CEntityInstance)null || !((CEntityInstance)item4).IsValid || !_nextRecordTime.TryGetValue(item4, out var value) || num < value)
				{
					continue;
				}
				CHandle<CCSPlayerPawn> playerPawn = item4.PlayerPawn;
				object obj;
				if (playerPawn == null)
				{
					obj = null;
				}
				else
				{
					CCSPlayerPawn value2 = playerPawn.Value;
					obj = ((value2 != null) ? ((CBaseEntity)value2).AbsOrigin : null);
				}
				if (obj == null)
				{
					continue;
				}
				CCSPlayerPawn value3 = item4.PlayerPawn.Value;
				if (((CBaseEntity)value3).LifeState == 0)
				{
					Vector item = new Vector((float?)((CBaseEntity)value3).AbsOrigin.X, (float?)((CBaseEntity)value3).AbsOrigin.Y, (float?)((CBaseEntity)value3).AbsOrigin.Z);
					QAngle item2 = new QAngle((float?)value3.EyeAngles.X, (float?)value3.EyeAngles.Y, (float?)value3.EyeAngles.Z);
					Vector item3 = ((((CBaseEntity)value3).AbsVelocity != null) ? new Vector((float?)((CBaseEntity)value3).AbsVelocity.X, (float?)((CBaseEntity)value3).AbsVelocity.Y, (float?)((CBaseEntity)value3).AbsVelocity.Z) : new Vector((float?)0f, (float?)0f, (float?)0f));
					_shadows[item4].Add((item, item2, item3));
					int maxShadows = _config.Dices.Afterimage.MaxShadows;
					while (_shadows[item4].Count > maxShadows)
					{
						_shadows[item4].RemoveAt(0);
					}
					float num2 = _config.Dices.Afterimage.RecordIntervalMin + (float)(_random.NextDouble() * (double)(_config.Dices.Afterimage.RecordIntervalMax - _config.Dices.Afterimage.RecordIntervalMin));
					_nextRecordTime[item4] = num + num2;
				}
			}
			catch
			{
			}
		}
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_players.Contains(player) || !((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if ((!_recallCooldowns.TryGetValue(player, out var value) || !(num < value)) && _shadows.TryGetValue(player, out List<(Vector, QAngle, Vector)> value2) && value2.Count != 0)
		{
			List<(Vector, QAngle, Vector)> list = value2;
			(Vector, QAngle, Vector) tuple = list[list.Count - 1];
			value2.RemoveAt(value2.Count - 1);
			CCSPlayerPawn value3 = player.PlayerPawn.Value;
			if (_config.Dices.Afterimage.SmokeOnRecall && ((CBaseEntity)value3).AbsOrigin != null)
			{
				SpawnPuffSmoke(((CBaseEntity)value3).AbsOrigin.X, ((CBaseEntity)value3).AbsOrigin.Y, ((CBaseEntity)value3).AbsOrigin.Z + 5f);
			}
			((CBaseEntity)value3).Teleport(tuple.Item1, tuple.Item2, tuple.Item3);
			if (_config.Dices.Afterimage.SmokeOnRecall)
			{
				SpawnPuffSmoke(tuple.Item1.X, tuple.Item1.Y, tuple.Item1.Z + 5f);
			}
			_recallCooldowns[player] = num + _config.Dices.Afterimage.RecallCooldown;
			player.PrintToCenterAlert("\ud83c\udf00 时空回溯!");
		}
	}

	private static void SpawnPuffSmoke(float x, float y, float z)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_00a5: Expected O, but got Unknown
		Vector val = new Vector((float?)x, (float?)y, (float?)z);
		CSmokeGrenadeProjectile smoke = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>("smokegrenade_projectile");
		if (!((CEntityInstance)(object)smoke != (CEntityInstance)null) || !((CEntityInstance)smoke).IsValid)
		{
			return;
		}
		((CBaseEntity)smoke).Teleport(val, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
		((CBaseEntity)smoke).DispatchSpawn();
		smoke.SmokeColor.X = 150f;
		smoke.SmokeColor.Y = 150f;
		smoke.SmokeColor.Z = 220f;
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
