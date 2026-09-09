using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class ReturnToSender : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public CBaseEntity[] playerSpawnEntities = Array.Empty<CBaseEntity>();

	public CBaseEntity[] ctSpawnEntities = Array.Empty<CBaseEntity>();

	public CBaseEntity[] tSpawnEntities = Array.Empty<CBaseEntity>();

	public override string ClassName => "ReturnToSender";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventPlayerHurt";
			return list;
		}
	}

	public ReturnToSender(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Invalid comparison between Unknown and I4
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Invalid comparison between Unknown and I4
		CCSPlayerController userid = @event.Userid;
		CCSPlayerController attacker = @event.Attacker;
		if (userid == null || !((CEntityInstance)userid).IsValid || attacker == null || !((CEntityInstance)attacker).IsValid || !_players.Contains(attacker) || (CEntityInstance)(object)((CBasePlayerController)userid).Pawn.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		int minChance = _config.Dices.ReturnToSender.MinChance;
		int maxChance = _config.Dices.ReturnToSender.MaxChance;
		double num = (double)minChance + _random.NextDouble() * (double)(maxChance - minChance);
		double num2 = _random.NextDouble() * 100.0;
		if (num2 > num)
		{
			return (HookResult)0;
		}
		GetSpawnEntities();
		CsTeam team = userid.Team;
		if (((int)team - 2) <= 1)
		{
			CBaseEntity[] first = (((int)userid.Team == 3) ? ctSpawnEntities : tSpawnEntities);
			List<CBaseEntity> source = (from _ in first.Concat(playerSpawnEntities)
				orderby _random.Next()
				select _).ToList();
			CBaseEntity val = source.FirstOrDefault((CBaseEntity s) => !IsPlayerNearby(s.AbsOrigin));
			if ((CEntityInstance)(object)val != (CEntityInstance)null)
			{
				((CBaseEntity)((CBasePlayerController)userid).Pawn.Value).Teleport(val.AbsOrigin, (QAngle)null, (Vector)null);
				NotifyStatus(userid, ClassName, new Dictionary<string, string>());
			}
		}
		return (HookResult)0;
	}

	private void GetSpawnEntities()
	{
		if (playerSpawnEntities.Length == 0 && ctSpawnEntities.Length == 0 && tSpawnEntities.Length == 0)
		{
			playerSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_start").ToArray();
			ctSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_counterterrorist").ToArray();
			tSpawnEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_terrorist").ToArray();
		}
	}

	private bool IsPlayerNearby(Vector position)
	{
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
			select p)
		{
			if (Vectors.GetDistance(position, ((CBaseEntity)((CBasePlayerController)item).Pawn.Value).AbsOrigin) < 100f)
			{
				return true;
			}
		}
		return false;
	}
}
