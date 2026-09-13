using System;
using System.Collections.Generic;
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

public class Martyrdom : DiceBlueprint
{
	private bool _comboActive;

	private float _nextDetonateTime;

	public override string ClassName => "Martyrdom";

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

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnPlayerButtonsChanged";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public Martyrdom(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "GrenadeKing");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "爆炸艺术家", "殉道爆炸范围翻倍");
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
		_nextDetonateTime = 0f;
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count != 0 && !((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _players.Contains(player) && ((Enum)pressed).HasFlag((Enum)(object)(PlayerButtons)32) && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid && ((CBaseEntity)player.PlayerPawn.Value).LifeState == 0)
		{
			float num = Server.CurrentTime;
			if (!(num < _nextDetonateTime))
			{
				_nextDetonateTime = num + 30f;
				DoExplosion(player);
			}
		}
	}

	public void OnTick()
	{
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		DoExplosion(userid);
		return (HookResult)0;
	}

	private void DoExplosion(CCSPlayerController player)
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
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
		if (obj == null)
		{
			return;
		}
		Vector absOrigin = ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin;
		float num = (DiceSynergy.HasPartner(player, "GrenadeKing") ? (_config.Dices.Martyrdom.ExplosionRadius * 2f) : _config.Dices.Martyrdom.ExplosionRadius);
		int explosionDamage = _config.Dices.Martyrdom.ExplosionDamage;
		Vector expPos = absOrigin;
		float expRadius = num;
		int expDamage = explosionDamage;
		new Timer(0.5f, (Action)delegate
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Expected O, but got Unknown
			//IL_006a: Expected O, but got Unknown
			CBaseEntity val = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
			if ((CEntityInstance)(object)val != (CEntityInstance)null)
			{
				val.Teleport(expPos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
				val.DispatchSpawn();
				((CEntityInstance)val).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
			}
			foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
				select p)
			{
				float distance = Vectors.GetDistance(expPos, ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin);
				if (distance <= expRadius)
				{
					float num2 = ((((CBaseEntity)item).TeamNum == ((CBaseEntity)player).TeamNum) ? 0.5f : 1f);
					float num3 = 1f - distance / expRadius;
					int num4 = (int)float.Round((float)expDamage * num3 * num2);
					((CBaseEntity)item.PlayerPawn.Value).Health -= num4;
					Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
					item.PrintToCenterAlert($"\ud83d\udca5 -{num4} HP!");
					if (((CBaseEntity)item.PlayerPawn.Value).Health <= 0)
					{
						try
						{
							((CBasePlayerPawn)item.PlayerPawn.Value).CommitSuicide(false, true);
						}
						catch
						{
							((CBaseEntity)item.PlayerPawn.Value).Health = 0;
							Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseEntity", "m_iHealth", 0);
						}
					}
				}
			}
		}, (TimerFlags?)null);
		player.PrintToCenterAlert("\ud83d\udca5 殉爆！");
	}
}
