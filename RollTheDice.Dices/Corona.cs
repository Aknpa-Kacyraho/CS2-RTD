using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices;

public class Corona : DiceBlueprint
{
	private float _roundStartTime;

	private readonly Dictionary<CCSPlayerController, bool> _coronated = new Dictionary<CCSPlayerController, bool>();

	private readonly Dictionary<ulong, (float EndTime, float LastTick)> _fireTargets = new Dictionary<ulong, (float, float)>();

	public override string ClassName => "Corona";

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
			span[num2] = "OnPlayerTakeDamagePre";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public Corona(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_coronated[player] = false;
			if (_roundStartTime == 0f)
			{
				_roundStartTime = Server.CurrentTime;
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
		_coronated.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_coronated.Clear();
		_roundStartTime = 0f;
		_fireTargets.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid || !_players.Contains(victim))
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		if (num - _roundStartTime < _config.Dices.Corona.Delay)
		{
			return (HookResult)0;
		}
		if (_coronated.TryGetValue(victim, out var value) & value)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn val = victim.PlayerPawn?.Value;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid)
		{
			return (HookResult)0;
		}
		string text = @event.Weapon ?? "";
		if (!text.Contains("inferno", StringComparison.OrdinalIgnoreCase) && !text.Contains("molotov", StringComparison.OrdinalIgnoreCase) && !text.Contains("hegrenade", StringComparison.OrdinalIgnoreCase))
		{
			return (HookResult)0;
		}
		Server.NextFrame((Action)delegate
		{
			if (!((CEntityInstance)(object)victim == (CEntityInstance)null) && ((CEntityInstance)victim).IsValid)
			{
				victim.Respawn();
				Server.NextFrame((Action)delegate
				{
					Server.NextFrame((Action)delegate
					{
						CCSPlayerPawn val2 = victim.PlayerPawn?.Value;
						if (val2 != null && ((CEntityInstance)val2).IsValid)
						{
							((CBaseEntity)val2).MaxHealth = _config.Dices.Corona.RespawnHP;
							((CBaseEntity)val2).Health = _config.Dices.Corona.RespawnHP;
							val2.ArmorValue = _config.Dices.Corona.RespawnArmor;
							Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iMaxHealth", 0);
							Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
							Utilities.SetStateChanged((CBaseEntity)(object)val2, "CCSPlayerPawn", "m_ArmorValue", 0);
							_coronated[victim] = true;
							victim.PrintToCenterAlert("☀\ufe0f 加冕太阳神！200HP+火焰附伤！");
							Server.PrintToChatAll($" {_localizer["command.prefix"].Value}☀\ufe0f {((CBasePlayerController)victim).PlayerName} 从火焰中重生，加冕为太阳神！");
						}
					});
				});
			}
		});
		return (HookResult)0;
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		if ((CEntityInstance)(object)entity == (CEntityInstance)null || !((CEntityInstance)entity).IsValid)
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> attacker = info.Attacker;
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			CBaseEntity value = attacker.Value;
			if (value == null)
			{
				obj = null;
			}
			else
			{
				CCSPlayerPawn obj2 = ((NativeObject)value).As<CCSPlayerPawn>();
				if (obj2 == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)obj2).Controller;
					if (controller == null)
					{
						obj = null;
					}
					else
					{
						CBasePlayerController value2 = controller.Value;
						obj = ((value2 != null) ? ((NativeObject)value2).As<CCSPlayerController>() : null);
					}
				}
			}
		}
		CCSPlayerController val = (CCSPlayerController)obj;
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_coronated.TryGetValue(val, out var value3) || !value3)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj3 = ((NativeObject)entity).As<CCSPlayerPawn>();
		object obj4;
		if (obj3 == null)
		{
			obj4 = null;
		}
		else
		{
			CHandle<CBasePlayerController> controller2 = ((CBasePlayerPawn)obj3).Controller;
			if (controller2 == null)
			{
				obj4 = null;
			}
			else
			{
				CBasePlayerController value4 = controller2.Value;
				obj4 = ((value4 != null) ? ((NativeObject)value4).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController val2 = (CCSPlayerController)obj4;
		if ((CEntityInstance)(object)val2 == (CEntityInstance)null || !((CEntityInstance)val2).IsValid || ((CBasePlayerController)val2).SteamID == ((CBasePlayerController)val).SteamID)
		{
			return (HookResult)0;
		}
		float num = Server.CurrentTime;
		_fireTargets[((CBasePlayerController)val2).SteamID] = (num + _config.Dices.Corona.FireDuration, num);
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_fireTargets.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		foreach (KeyValuePair<ulong, (float, float)> item in _fireTargets.ToList())
		{
			item.Deconstruct(out var key, out var value);
			(float, float) tuple = value;
			ulong steamId = key;
			var (num2, num3) = tuple;
			if (num >= num2)
			{
				_fireTargets.Remove(steamId);
			}
			else
			{
				if (num - num3 < 1f)
				{
					continue;
				}
				_fireTargets[steamId] = (num2, num);
				CCSPlayerController val = Utilities.GetPlayers().FirstOrDefault((CCSPlayerController p) => ((CBasePlayerController)p).SteamID == steamId);
				CCSPlayerPawn val2 = ((val == null) ? null : val.PlayerPawn?.Value);
				if (val2 == null || !((CEntityInstance)val2).IsValid || ((CBaseEntity)val2).LifeState != 0)
				{
					_fireTargets.Remove(steamId);
					continue;
				}
				((CBaseEntity)val2).Health -= _config.Dices.Corona.FireDps;
				Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
				if (((CBaseEntity)val2).Health > 0)
				{
					continue;
				}
				if (!val.IsBot && !((CBasePlayerController)val).IsHLTV)
				{
					((CBasePlayerPawn)val2).CommitSuicide(false, true);
					continue;
				}
				try
				{
					((CBasePlayerPawn)val2).CommitSuicide(false, true);
				}
				catch
				{
					((CBaseEntity)val2).Health = 0;
					Utilities.SetStateChanged((CBaseEntity)(object)val2, "CBaseEntity", "m_iHealth", 0);
				}
			}
		}
	}
}
