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

public class RepulsionField : DiceBlueprint
{
	private readonly HashSet<string> _projectileTypes = new HashSet<string> { "smokegrenade_projectile", "hegrenade_projectile", "molotov_projectile", "decoy_projectile", "flashbang_projectile" };

	private readonly Dictionary<uint, float> _trackedNades = new Dictionary<uint, float>();

	private const float MetersToUnits = 39.37f;

	public override string ClassName => "RepulsionField";

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

	public RepulsionField(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "Drone"))
			{
				DiceSynergy.AnnounceCombo(player, "无人防线", "斥力半径翻倍");
			}
			if (DiceSynergy.HasPartner(player, "MagneticPulse"))
			{
				DiceSynergy.AnnounceCombo(player, "禁区", "斥力场弹回投掷物+磁力脉冲缴械！完全封锁远程！");
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
		_trackedNades.Clear();
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count == 0 || !_projectileTypes.Contains(entity.DesignerName))
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			if (!(entity == (CEntityInstance)null) && entity.IsValid)
			{
				CBaseCSGrenadeProjectile val = new CBaseCSGrenadeProjectile(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid)
				{
					_trackedNades[((CEntityInstance)val).Index] = Server.CurrentTime + 10f;
				}
			}
		});
	}

	public void OnTick()
	{
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Expected O, but got Unknown
		float num = Server.CurrentTime;
		foreach (KeyValuePair<uint, float> item in _trackedNades.ToList())
		{
			if (num > item.Value)
			{
				_trackedNades.Remove(item.Key);
			}
		}
		if (_players.Count == 0 || Server.TickCount % 4 != 0)
		{
			return;
		}
		foreach (KeyValuePair<uint, float> item2 in _trackedNades.ToList())
		{
			CBaseCSGrenadeProjectile entityFromIndex = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>((int)item2.Key);
			if ((CEntityInstance)(object)entityFromIndex == (CEntityInstance)null || !((CEntityInstance)entityFromIndex).IsValid || ((CBaseEntity)entityFromIndex).AbsOrigin == null)
			{
				_trackedNades.Remove(item2.Key);
				continue;
			}
			foreach (CCSPlayerController player in _players)
			{
				float num2 = (DiceSynergy.HasPartner(player, "Drone") || DiceSynergy.HasPartner(player, "MagneticPulse")) ? (_config.Dices.RepulsionField.Radius * 2f) : _config.Dices.RepulsionField.Radius;
				float num3 = num2 * 39.37f;
				object obj;
				if (player == null)
				{
					obj = null;
				}
				else
				{
					CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
					if (playerPawn == null)
					{
						obj = null;
					}
					else
					{
						CCSPlayerPawn value = playerPawn.Value;
						obj = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
					}
				}
				if (obj != null)
				{
					float num4 = ((CBaseEntity)entityFromIndex).AbsOrigin.X - ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.X;
					float num5 = ((CBaseEntity)entityFromIndex).AbsOrigin.Y - ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Y;
					float num6 = ((CBaseEntity)entityFromIndex).AbsOrigin.Z - ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Z;
					float num7 = MathF.Sqrt(num4 * num4 + num5 * num5 + num6 * num6);
					if (num7 < num3 && (((CBaseEntity)entityFromIndex).TeamNum != ((CBaseEntity)player).TeamNum || ((CBaseEntity)entityFromIndex).TeamNum == 0))
					{
						float speedMultiplier = _config.Dices.RepulsionField.SpeedMultiplier;
						Vector val = new Vector((float?)((0f - ((CBaseEntity)entityFromIndex).AbsVelocity.X) * speedMultiplier), (float?)((0f - ((CBaseEntity)entityFromIndex).AbsVelocity.Y) * speedMultiplier), (float?)((0f - ((CBaseEntity)entityFromIndex).AbsVelocity.Z) * speedMultiplier * 0.5f));
						((CBaseEntity)entityFromIndex).Teleport((Vector)null, (QAngle)null, val);
						_trackedNades.Remove(item2.Key);
						break;
					}
				}
			}
		}
	}
}
