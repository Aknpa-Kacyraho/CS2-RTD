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

public class ShadowWarrior : DiceBlueprint
{
	private bool _comboActive;

	private readonly HashSet<CCSPlayerController> _cloaked = new HashSet<CCSPlayerController>();

	private readonly Dictionary<CCSPlayerController, CDynamicProp> _cloneProps = new Dictionary<CCSPlayerController, CDynamicProp>();

	private readonly Dictionary<CCSPlayerController, float> _cloneUntil = new Dictionary<CCSPlayerController, float>();

	private readonly Dictionary<CCSPlayerController, float> _cloneCdUntil = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "ShadowWarrior";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			index++;
			span[index] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public ShadowWarrior(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseModelEntity)value).Render = Color.FromArgb(20, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			_cloaked.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Hermit");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "暗影行者", "隐者+影者！隐身透明度更低，走路无脚步声！");
			}
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			player.PrintToCenterAlert("\ud83d\udc65 影者！几乎完全隐身！");
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		_cloaked.Remove(player);
		RemoveClone(player);
		if ((CEntityInstance)(object)player.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}

	private void RemoveClone(CCSPlayerController player)
	{
		if (_cloneProps.TryGetValue(player, out CDynamicProp prop) && (CEntityInstance)(object)prop != (CEntityInstance)null)
		{
			Entities.RemoveEntity((CBaseEntity?)(object)prop);
		}
		_cloneProps.Remove(player);
		_cloneUntil.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)item.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				((CBaseModelEntity)item.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
			}
			RemoveClone(item);
		}
		_players.Clear();
		_cloaked.Clear();
		_cloneProps.Clear();
		_cloneUntil.Clear();
		_cloneCdUntil.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		if (!((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) || ((Enum)released).HasFlag((Enum)(object)(PlayerButtons)32) || (CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || player.IsBot || ((CBasePlayerController)player).IsHLTV || !_players.Contains(player))
		{
			return;
		}
		float num = Server.CurrentTime;
		if (_cloneCdUntil.TryGetValue(player, out float cd) && num < cd)
		{
			return;
		}
		CCSPlayerPawn pawn = player.PlayerPawn?.Value;
		if ((CEntityInstance)(object)pawn == (CEntityInstance)null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).LifeState != 0 || ((CBaseEntity)pawn).AbsOrigin == null)
		{
			return;
		}
		if (_cloneProps.ContainsKey(player))
		{
			RemoveClone(player);
		}
		float cloneDistance = _config.Dices.ShadowWarrior.CloneDistance;
		float yaw = pawn.V_angle.Y;
		Vector fwd = Entities.GetForwardVector(new QAngle((float?)0f, (float?)yaw, (float?)0f));
		Vector pos = new Vector((float?)(((CBaseEntity)pawn).AbsOrigin.X + fwd.X * cloneDistance), (float?)(((CBaseEntity)pawn).AbsOrigin.Y + fwd.Y * cloneDistance), (float?)((CBaseEntity)pawn).AbsOrigin.Z);
		CDynamicProp prop = Entities.CreatePropEntity(pos, new QAngle((float?)0f, (float?)yaw, (float?)0f), Entities.GetModel((CBaseEntity)(object)pawn), 1f, (CEntityInstance?)null);
		if ((CEntityInstance)(object)prop == (CEntityInstance)null || !((CEntityInstance)prop).IsValid)
		{
			return;
		}
		((CBaseModelEntity)prop).Render = Color.FromArgb(_config.Dices.ShadowWarrior.Alpha, 255, 255, 255);
		Utilities.SetStateChanged((CBaseEntity)(object)prop, "CBaseModelEntity", "m_clrRender", 0);
		_cloneProps[player] = prop;
		_cloneUntil[player] = num + _config.Dices.ShadowWarrior.CloneLifetime;
		_cloneCdUntil[player] = num + _config.Dices.ShadowWarrior.Cooldown;
		player.PrintToCenterAlert($"\ud83d\udc65 影分身！持续{_config.Dices.ShadowWarrior.CloneLifetime:F0}s，CD {_config.Dices.ShadowWarrior.Cooldown:F0}s");
	}

	private void UpdateClones(float now)
	{
		foreach (CCSPlayerController item in _cloneUntil.Keys.ToList())
		{
			if (!_cloneUntil.TryGetValue(item, out float end))
			{
				continue;
			}
			if (now >= end || !_cloneProps.TryGetValue(item, out CDynamicProp prop) || (CEntityInstance)(object)prop == (CEntityInstance)null || !((CEntityInstance)prop).IsValid)
			{
				RemoveClone(item);
				continue;
			}
			CCSPlayerPawn pawn = item.PlayerPawn?.Value;
			if ((CEntityInstance)(object)pawn == (CEntityInstance)null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).AbsOrigin == null || ((CBaseEntity)prop).AbsOrigin == null)
			{
				continue;
			}
			Vector fwd = Entities.GetForwardVector(new QAngle((float?)0f, (float?)pawn.V_angle.Y, (float?)0f));
			float step = 300f * Server.TickInterval;
			Vector npos = new Vector((float?)(((CBaseEntity)prop).AbsOrigin.X + fwd.X * step), (float?)(((CBaseEntity)prop).AbsOrigin.Y + fwd.Y * step), (float?)((CBaseEntity)prop).AbsOrigin.Z);
			((CBaseEntity)prop).Teleport(npos, ((CBaseEntity)prop).AbsRotation, new Vector((float?)null, (float?)null, (float?)null));
		}
	}

	public void OnTick()
	{
		if (_players.Count == 0)
		{
			return;
		}
		foreach (CCSPlayerController item in _players.ToList())
		{
			if (!((CEntityInstance)(object)item == (CEntityInstance)null) && ((CEntityInstance)item).IsValid && !((CEntityInstance)(object)item.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)item.PlayerPawn.Value).IsValid && ((CBaseEntity)item.PlayerPawn.Value).LifeState == 0)
			{
				CCSPlayerPawn value = item.PlayerPawn.Value;
				int cloakAlpha = (DiceSynergy.HasPartner(item, "Hermit") ? 10 : 25);
				if (((CBaseModelEntity)value).Render.A != cloakAlpha)
				{
					((CBaseModelEntity)value).Render = Color.FromArgb(cloakAlpha, 255, 255, 255);
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
				}
			}
		}
		UpdateClones(Server.CurrentTime);
	}
}
