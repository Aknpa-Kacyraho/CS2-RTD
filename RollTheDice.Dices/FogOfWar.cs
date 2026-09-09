using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class FogOfWar : DiceBlueprint
{
	private CFogController? _fogController;

	private CPlayerVisibility? _playerVisibility;

	private readonly HashSet<uint> _foggedPawnIndices = new HashSet<uint>();

	public override string ClassName => "FogOfWar";

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

	public FogOfWar(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			EnsureFogEntities();
			ApplyFogToAll();
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
		if (_players.Count == 0)
		{
			CleanupFog();
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_foggedPawnIndices.Clear();
		CleanupFog();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void EnsureFogEntities()
	{
		if ((CEntityInstance)(object)_fogController == (CEntityInstance)null || !((CEntityInstance)_fogController).IsValid)
		{
			_fogController = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
			if ((CEntityInstance)(object)_fogController == (CEntityInstance)null)
			{
				return;
			}
			((CBaseEntity)_fogController).DispatchSpawn();
			Color color = ColorTranslator.FromHtml(_config.Dices.FogOfWar.Color);
			if (color == Color.Empty)
			{
				color = Color.DarkOrange;
			}
			_fogController.Fog.Enable = true;
			_fogController.Fog.ColorPrimary = color;
			_fogController.Fog.Exponent = _config.Dices.FogOfWar.Exponent;
			_fogController.Fog.Maxdensity = _config.Dices.FogOfWar.Density;
			_fogController.Fog.End = _config.Dices.FogOfWar.EndDistance;
		}
		if (!((CEntityInstance)(object)_playerVisibility == (CEntityInstance)null) && ((CEntityInstance)_playerVisibility).IsValid)
		{
			return;
		}
		_playerVisibility = Utilities.FindAllEntitiesByDesignerName<CPlayerVisibility>("env_player_visibility").FirstOrDefault();
		if ((CEntityInstance)(object)_playerVisibility == (CEntityInstance)null)
		{
			_playerVisibility = Utilities.CreateEntityByName<CPlayerVisibility>("env_player_visibility");
			if ((CEntityInstance)(object)_playerVisibility != (CEntityInstance)null)
			{
				((CBaseEntity)_playerVisibility).DispatchSpawn();
			}
		}
		if ((CEntityInstance)(object)_playerVisibility != (CEntityInstance)null)
		{
			_playerVisibility.FogMaxDensityMultiplier = _config.Dices.FogOfWar.PlayerVisibility;
			Utilities.SetStateChanged((CBaseEntity)(object)_playerVisibility, "CPlayerVisibility", "m_flFogMaxDensityMultiplier", 0);
		}
	}

	private void ApplyFogToAll()
	{
		if ((CEntityInstance)(object)_fogController == (CEntityInstance)null || !((CEntityInstance)_fogController).IsValid)
		{
			return;
		}
		_foggedPawnIndices.Clear();
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			((CEntityInstance)item.PlayerPawn.Value).AcceptInput("SetFogController", (CEntityInstance)(object)_fogController, (CEntityInstance)(object)_fogController, "!activator", 0);
			_foggedPawnIndices.Add(((CEntityInstance)item.PlayerPawn.Value).Index);
		}
	}

	private void CleanupFog()
	{
		_foggedPawnIndices.Clear();
		if ((CEntityInstance)(object)_fogController != (CEntityInstance)null && ((CEntityInstance)_fogController).IsValid)
		{
			((CEntityInstance)_fogController).Remove();
		}
		_fogController = null;
		_playerVisibility = null;
	}

	public void OnTick()
	{
		if (_players.Count == 0 || Server.TickCount % 32 != 0)
		{
			return;
		}
		if ((CEntityInstance)(object)_fogController == (CEntityInstance)null || !((CEntityInstance)_fogController).IsValid)
		{
			EnsureFogEntities();
			ApplyFogToAll();
			return;
		}
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0
			select p)
		{
			uint index = ((CEntityInstance)item.PlayerPawn.Value).Index;
			if (!_foggedPawnIndices.Contains(index))
			{
				((CEntityInstance)item.PlayerPawn.Value).AcceptInput("SetFogController", (CEntityInstance)(object)_fogController, (CEntityInstance)(object)_fogController, "!activator", 0);
				_foggedPawnIndices.Add(index);
			}
		}
	}
}
