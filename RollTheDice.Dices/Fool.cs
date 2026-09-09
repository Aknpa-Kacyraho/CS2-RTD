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

namespace RollTheDice.Dices;

public class Fool : DiceBlueprint
{
	private readonly Dictionary<ulong, float> _invulEndTime = new Dictionary<ulong, float>();

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	public override string ClassName => "Fool";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public Fool(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
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
		_players.Remove(player);
		_invulEndTime.Remove(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		foreach (ulong sid in _invulEndTime.Keys.ToList())
		{
			CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController x) => ((CBasePlayerController)x).SteamID == sid);
			CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
			if (val2 != null && ((CEntityInstance)val2).IsValid)
			{
				((CBaseEntity)val2).TakesDamage = true;
			}
		}
		_players.Clear();
		_invulEndTime.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj).Controller;
			if (controller == null)
			{
				obj2 = null;
			}
			else
			{
				CBasePlayerController value = controller.Value;
				obj2 = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj2;
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj3;
		if (attacker == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value2 = attacker.Value;
			if (value2 == null)
			{
				obj3 = null;
			}
			else
			{
				CCSPlayerPawn obj4 = ((NativeObject)value2).As<CCSPlayerPawn>();
				if (obj4 == null)
				{
					obj3 = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj4).Controller;
					if (controller2 == null)
					{
						obj3 = null;
					}
					else
					{
						CBasePlayerController value3 = controller2.Value;
						obj3 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj3;
		float num = Server.CurrentTime;
		if ((CEntityInstance)(object)val2 != (CEntityInstance)null && ((CEntityInstance)val2).IsValid && _players.Contains(val2) && _random.NextDouble() < (double)_config.Dices.Fool.AttackWhiffChance)
		{
			info.Damage = 0f;
		}
		if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid && _players.Contains(val) && info.Damage > 0f)
		{
			if (_invulEndTime.TryGetValue(((CBasePlayerController)val).SteamID, out var value4) && num >= value4)
			{
				CCSPlayerPawn val3 = val.PlayerPawn?.Value;
				if (val3 != null && ((CEntityInstance)val3).IsValid)
				{
					((CBaseEntity)val3).TakesDamage = true;
				}
				_invulEndTime.Remove(((CBasePlayerController)val).SteamID);
			}
			if (!_invulEndTime.ContainsKey(((CBasePlayerController)val).SteamID) && _random.NextDouble() < (double)_config.Dices.Fool.InvincibilityChance)
			{
				CCSPlayerPawn val4 = val.PlayerPawn?.Value;
				if (val4 != null && ((CEntityInstance)val4).IsValid)
				{
					((CBaseEntity)val4).TakesDamage = false;
					float invincibilitySeconds = _config.Dices.Fool.InvincibilitySeconds;
					_invulEndTime[((CBasePlayerController)val).SteamID] = num + invincibilitySeconds;
					val.PrintToCenterAlert($"\ud83c\udccf 愚者庇护！无敌{invincibilitySeconds:F0}秒！");
					CCSPlayerController capV = val;
					new Timer(invincibilitySeconds, (Action)delegate
					{
						CCSPlayerController obj5 = capV;
						CCSPlayerPawn val5 = ((obj5 == null) ? null : obj5.PlayerPawn?.Value);
						if (val5 != null && ((CEntityInstance)val5).IsValid)
						{
							((CBaseEntity)val5).TakesDamage = true;
						}
						_invulEndTime.Remove(((CBasePlayerController)capV).SteamID);
					}, (TimerFlags?)null);
				}
			}
			if (_invulEndTime.ContainsKey(((CBasePlayerController)val).SteamID))
			{
				info.Damage = 0f;
			}
		}
		return (info.Damage == 0f) ? HookResult.Changed : HookResult.Continue;
	}
}
