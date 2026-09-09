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

public class ToxicSmoke : DiceBlueprint
{
	private readonly HashSet<nint> _toxicSmokes = new HashSet<nint>();

	private readonly Dictionary<nint, CCSPlayerController> _smokeOwners = new Dictionary<nint, CCSPlayerController>();

	private float _lastDamageTime;

	public override string ClassName => "ToxicSmoke";

	public override List<string> Listeners
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnEntitySpawned";
			num2++;
			span[num2] = "OnTick";
			return list;
		}
	}

	public ToxicSmoke(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
	}

	public override void Reset()
	{
		_players.Clear();
		_toxicSmokes.Clear();
		_smokeOwners.Clear();
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0 || entity.DesignerName != "smokegrenade_projectile")
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			if (entity.IsValid)
			{
				CSmokeGrenadeProjectile val = new CSmokeGrenadeProjectile(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid)
				{
					CCSPlayerPawn val2 = ((CBaseGrenade)val).OriginalThrower?.Value;
					if (!((CEntityInstance)(object)val2 == (CEntityInstance)null) && ((CEntityInstance)val2).IsValid)
					{
						CHandle<CBasePlayerController> controller = ((CBasePlayerPawn)val2).Controller;
						object obj;
						if (controller == null)
						{
							obj = null;
						}
						else
						{
							CBasePlayerController value = controller.Value;
							obj = ((value != null) ? ((NativeObject)value).As<CCSPlayerController>() : null);
						}
						CCSPlayerController val3 = (CCSPlayerController)obj;
						if (!((CEntityInstance)(object)val3 == (CEntityInstance)null) && ((CEntityInstance)val3).IsValid && _players.Contains(val3))
						{
							_toxicSmokes.Add(((NativeEntity)val).Handle);
							_smokeOwners[((NativeEntity)val).Handle] = val3;
						}
					}
				}
			}
		});
	}

	public void OnTick()
	{
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected O, but got Unknown
		if (_toxicSmokes.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (num - _lastDamageTime < 1f)
		{
			return;
		}
		_lastDamageTime = num;
		List<nint> list = _toxicSmokes.Where(delegate(nint h)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return !((CEntityInstance)new CSmokeGrenadeProjectile((IntPtr)h)).IsValid;
		}).ToList();
		foreach (nint item in list)
		{
			_toxicSmokes.Remove(item);
			_smokeOwners.Remove(item);
		}
		int damagePerSecond = _config.Dices.ToxicSmoke.DamagePerSecond;
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null || !((CEntityInstance)player.PlayerPawn.Value).IsValid || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			CCSPlayerPawn value = player.PlayerPawn.Value;
			Vector absOrigin = ((CBaseEntity)value).AbsOrigin;
			if (absOrigin == null)
			{
				continue;
			}
			foreach (nint item2 in _toxicSmokes.ToList())
			{
				CSmokeGrenadeProjectile val = new CSmokeGrenadeProjectile((IntPtr)item2);
				if (!((CEntityInstance)val).IsValid || ((CBaseEntity)val).AbsOrigin == null)
				{
					continue;
				}
				float distance = Vectors.GetDistance(absOrigin, ((CBaseEntity)val).AbsOrigin);
				if (distance <= 200f)
				{
					((CBaseEntity)value).Health -= damagePerSecond;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)value).Health <= 0)
					{
						if (!player.IsBot && !((CBasePlayerController)player).IsHLTV)
						{
							((CBasePlayerPawn)value).CommitSuicide(false, true);
						}
						else
						{
							try
							{
								((CBasePlayerPawn)value).CommitSuicide(false, true);
							}
							catch
							{
								((CBaseEntity)value).Health = 0;
								Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
							}
						}
					}
				}
			}
		}
	}
}
