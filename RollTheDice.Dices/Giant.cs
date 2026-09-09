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

public class Giant : DiceBlueprint
{
	public override string ClassName => "Giant";

	public Giant(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
			float sizeScale = _config.Dices.Giant.SizeScale;
			((CGameSceneNode)((CBaseEntity)value2).CBodyComponent.SceneNode.GetSkeletonInstance()).Scale = sizeScale;
			((CEntityInstance)value2).AcceptInput("SetScale", (CEntityInstance)null, (CEntityInstance)null, sizeScale.ToString(), 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_CBodyComponent", 0);
			float healthMultiplier = _config.Dices.Giant.HealthMultiplier;
			StackingHealth.RegisterMultiplier(player, "Giant", healthMultiplier);
			((CBaseEntity)value2).Health = ((CBaseEntity)value2).MaxHealth;
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			if (DiceSynergy.HasPartner(player, "RoyalBarrier"))
			{
				value2.ArmorValue = Math.Max(value2.ArmorValue, 100);
				Utilities.SetStateChanged((CBaseEntity)(object)value2, "CCSPlayerPawn", "m_ArmorValue", 0);
				DiceSynergy.AnnounceCombo(player, "钢铁要塞", "巨人体魄撑起堡垒！+100 护甲");
			}
			_players.Add(player);
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
		StackingHealth.UnregisterMultiplier(player, "Giant");
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}
}
