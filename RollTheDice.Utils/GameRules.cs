using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public static class GameRules
{
	public static CCSGameRules? _gameRules;

	private static IEnumerable<CCSTeam>? _teamManager;

	private static CCSGameRules? GetGameRule(bool forceRefresh = false)
	{
		if ((_gameRules == null) | forceRefresh)
		{
			CCSGameRulesProxy? obj = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault((CCSGameRulesProxy e) => (CEntityInstance)(object)e != (CEntityInstance)null && ((CEntityInstance)e).IsValid);
			_gameRules = ((obj != null) ? obj.GameRules : null);
		}
		return _gameRules;
	}

	public static void Refresh()
	{
		GetGameRule(forceRefresh: true);
	}

	public static object? Get(string rule, bool forceRefresh = false)
	{
		GetGameRule();
		PropertyInfo propertyInfo = ((object)_gameRules)?.GetType().GetProperty(rule);
		return ((object)propertyInfo != null && propertyInfo.CanRead) ? propertyInfo.GetValue(_gameRules) : null;
	}

	public static void SetRoundTime(float minutes)
	{
		GetGameRule();
		if (_gameRules != null)
		{
			_gameRules.RoundTime = (int)Math.Round(minutes * 60f);
		}
	}

	public static void TerminateRound(RoundEndReason reason, float delay = 0f)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		GetGameRule();
		if (_gameRules != null)
		{
			_gameRules.RoundsPlayedThisPhase = 1;
			_gameRules.ITotalRoundsPlayed = 1;
			_gameRules.TotalRoundsPlayed = 1;
			_gameRules.TerminateRound(delay, reason);
		}
	}

	public static void SetTeamScore(int score, CsTeam team)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between I4 and Unknown
		if (_teamManager == null)
		{
			_teamManager = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
			if (_teamManager == null)
			{
				return;
			}
		}
		foreach (CCSTeam item in _teamManager)
		{
			if (((CBaseEntity)item).TeamNum == (int)team)
			{
				((CTeam)item).Score = score;
				Utilities.SetStateChanged((CBaseEntity)(object)item, "CTeam", "m_iScore", 0);
				break;
			}
		}
	}
}
