using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class NukeLeak : DiceBlueprint
{
	private bool _comboActive;

	private float _detonationTime;

	private bool _detonated;

	public override string ClassName => "NukeLeak";

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

	public NukeLeak(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			return;
		}
		_players.Add(player);
		if (_detonationTime == 0f)
		{
			_comboActive = DiceSynergy.HasPartner(player, "Ragnarok");
			float num = (_comboActive ? (_config.Dices.NukeLeak.DetonationSeconds * 0.5f) : _config.Dices.NukeLeak.DetonationSeconds);
			_detonationTime = Server.CurrentTime + num;
			_detonated = false;
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "末日审判", "诸神黄昏+核泄漏！终焉加速降临！");
			}
		}
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_NukeLeak_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_detonationTime = 0f;
		_detonated = false;
	}

	public void OnTick()
	{
		if (_detonationTime == 0f || _detonated)
		{
			return;
		}
		float num = Server.CurrentTime;
		float num2 = _detonationTime - num;
		if (num2 <= 0f)
		{
			_detonated = true;
			float radius = _config.Dices.NukeLeak.DamageRadius;
			int damage = _config.Dices.NukeLeak.Damage;
			// 爆心 = 持有者所在位置；离开半径即可躲过。
			Vector? origin = null;
			foreach (CCSPlayerController holder in _players)
			{
				if (holder != null && holder.IsValid && holder.PlayerPawn?.Value != null && holder.PlayerPawn.Value.IsValid)
				{
					origin = holder.PlayerPawn.Value.AbsOrigin;
					if (origin != null)
					{
						break;
					}
				}
			}
			if (origin == null)
			{
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "☢ 核弹泄露哑火：持有者已不在场。");
				return;
			}
			foreach (CCSPlayerController player in Utilities.GetPlayers())
			{
				if (player == null || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.LifeState != 0)
				{
					continue;
				}
				// 持有者免疫；无敌窗口内也不受直扣血伤害。
				if (_players.Contains(player) || Invulnerability.IsInvulnerable(player))
				{
					continue;
				}
				CCSPlayerPawn pawn = player.PlayerPawn.Value;
				Vector? pos = pawn.AbsOrigin;
				if (pos == null || Vectors.GetDistance(origin, pos) > radius)
				{
					continue;
				}
				int newHp = pawn.Health - damage;
				if (newHp > 0)
				{
					pawn.Health = newHp;
					Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
					continue;
				}
				if (!player.IsBot && !player.IsHLTV)
				{
					pawn.CommitSuicide(false, true);
					continue;
				}
				try
				{
					pawn.CommitSuicide(false, true);
				}
				catch
				{
					pawn.Health = 0;
					Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth", 0);
				}
			}
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_NukeLeak_detonated"].Value);
		}
		else
		{
			if (Server.TickCount % 64 != 0)
			{
				return;
			}
			int num3 = (int)Math.Ceiling(num2);
			if (num3 != 60 && num3 != 30 && num3 != 15 && num3 > 10)
			{
				return;
			}
			foreach (CCSPlayerController player2 in _players)
			{
				if (player2 != null)
				{
					player2.PrintToCenterAlert($"☢ 核弹泄露！{num3}s");
				}
			}
		}
	}
}
