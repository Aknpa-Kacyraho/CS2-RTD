using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class IncreaseSpeed : DiceBlueprint
{
	public readonly Random _random = new Random();

	public readonly Dictionary<CCSPlayerController, float> _playerSpeed = new Dictionary<CCSPlayerController, float>();

	public override string ClassName => "IncreaseSpeed";

	public override List<string> Events
	{
		get
		{
			int num = 4;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "EventPlayerHurt";
			num2++;
			span[num2] = "EventPlayerFalldamage";
			num2++;
			span[num2] = "EventHostageFollows";
			num2++;
			span[num2] = "EventHostageStopsFollowing";
			return list;
		}
	}

	public IncreaseSpeed(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			float num = _random.NextSingle() * (_config.Dices.IncreaseSpeed.MaxSpeed - _config.Dices.IncreaseSpeed.MinSpeed) + _config.Dices.IncreaseSpeed.MinSpeed;
			_playerSpeed.Add(player, num);
			_players.Add(player);
			SpeedBonusManager.Register(player, "IncreaseSpeed", num - 1f);
			SetPlayerSpeed(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"percentageIncrease",
					Math.Round(((double)num - 1.0) * 100.0, 2).ToString()
				}
			});
		}
	}

	public override void Remove(CCSPlayerController? player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)player == (CEntityInstance)null)
		{
			return;
		}
		CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
		if (playerPawn != null)
		{
			CCSPlayerPawn value = playerPawn.Value;
			if (((value != null) ? new bool?(((CEntityInstance)value).IsValid) : ((bool?)null)) == false)
			{
				return;
			}
		}
		SpeedBonusManager.Unregister(player, "IncreaseSpeed");
		SetPlayerSpeed(player, force: true);
		_playerSpeed.Remove(player);
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_playerSpeed.Clear();
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		SetPlayerSpeed(@event.Userid);
		return (HookResult)0;
	}

	public HookResult EventPlayerFalldamage(EventPlayerFalldamage @event, GameEventInfo info)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		SetPlayerSpeed(@event.Userid);
		return (HookResult)0;
	}

	public HookResult EventHostageFollows(EventHostageFollows @event, GameEventInfo info)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!_config.Dices.IncreaseSpeed.ResetOnHostageRescue)
		{
			return (HookResult)0;
		}
		SpeedBonusManager.Unregister(@event.Userid, "IncreaseSpeed");
		SetPlayerSpeed(@event.Userid);
		return (HookResult)0;
	}

	public HookResult EventHostageStopsFollowing(EventHostageStopsFollowing @event, GameEventInfo info)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		SetPlayerSpeed(@event.Userid);
		return (HookResult)0;
	}

	private void SetPlayerSpeed(CCSPlayerController? player, float speed = -1f, bool force = false)
	{
		CCSPlayerController? obj = player;
		if (obj == null || !((CEntityInstance)obj).IsValid)
		{
			return;
		}
		float speedToApply = 1f + SpeedBonusManager.GetEffective(player, 100f);
		Server.NextFrame((Action)delegate
		{
			Server.NextFrame((Action)delegate
			{
				Server.NextFrame((Action)delegate
				{
					if (!((CEntityInstance)(object)player == (CEntityInstance)null))
					{
						CCSPlayerController? obj2 = player;
						if (obj2 == null || ((CEntityInstance)obj2).IsValid)
						{
							CCSPlayerController? obj3 = player;
							if (obj3 == null || obj3.PlayerPawn?.IsValid != false)
							{
								CCSPlayerController? obj4 = player;
								if (!((CEntityInstance)(object)((obj4 == null) ? null : obj4.PlayerPawn?.Value) == (CEntityInstance)null) && (_players.Contains(player) || force))
								{
									player.PlayerPawn.Value.VelocityModifier = speedToApply;
									Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
								}
							}
						}
					}
				});
			});
		});
	}
}
