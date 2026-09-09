using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class Mosquito : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	public override string ClassName => "Mosquito";

	public Mosquito(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
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
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CBodyComponent cBodyComponent = ((CBaseEntity)value).CBodyComponent;
				if (cBodyComponent == null)
				{
					obj = null;
				}
				else
				{
					CGameSceneNode sceneNode = cBodyComponent.SceneNode;
					obj = ((sceneNode != null) ? sceneNode.GetSkeletonInstance() : null);
				}
			}
		}
		if (obj != null)
		{
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value2).MaxHealth;
			float sizeScale = _config.Dices.Mosquito.SizeScale;
			((CGameSceneNode)((CBaseEntity)value2).CBodyComponent.SceneNode.GetSkeletonInstance()).Scale = sizeScale;
			((CEntityInstance)value2).AcceptInput("SetScale", (CEntityInstance)null, (CEntityInstance)null, sizeScale.ToString(), 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_CBodyComponent", 0);
			int health = _config.Dices.Mosquito.Health;
			((CBaseEntity)value2).MaxHealth = health;
			((CBaseEntity)value2).Health = health;
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "PlayAsChicken");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "迷你鸡神", "蚊子HP翻倍 鸡神HP翻倍");
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
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			CHandle<CBasePlayerPawn> pawn = ((CBasePlayerController)player).Pawn;
			if (pawn == null)
			{
				obj = null;
			}
			else
			{
				CBasePlayerPawn value = pawn.Value;
				if (value == null)
				{
					obj = null;
				}
				else
				{
					CBodyComponent cBodyComponent = ((CBaseEntity)value).CBodyComponent;
					obj = ((cBodyComponent != null) ? cBodyComponent.SceneNode : null);
				}
			}
		}
		if (obj != null && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			((CGameSceneNode)((CBaseEntity)((CBasePlayerController)player).Pawn.Value).CBodyComponent.SceneNode.GetSkeletonInstance()).Scale = 1f;
			((CEntityInstance)((CBasePlayerController)player).Pawn.Value).AcceptInput("SetScale", (CEntityInstance)null, (CEntityInstance)null, "1", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)player).Pawn.Value, "CBaseEntity", "m_CBodyComponent", 0);
		}
		if (_originalMaxHealth.TryGetValue(player, out var value2))
		{
			if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
			{
				((CBaseEntity)player.PlayerPawn.Value).MaxHealth = value2;
				((CBaseEntity)player.PlayerPawn.Value).Health = Math.Min(((CBaseEntity)player.PlayerPawn.Value).Health, value2);
				Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth", 0);
				Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
			}
			_originalMaxHealth.Remove(player);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}
}
