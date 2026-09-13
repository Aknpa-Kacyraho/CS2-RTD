using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

/// <summary>
/// 哑火 NoExplosives：持有者附近的敌人投掷的爆炸物会被替换成无害道具（光环判定，非随机点名）。
/// </summary>
public class NoExplosives : DiceBlueprint
{
	private readonly HashSet<string> _grenadeProjectiles = new HashSet<string> { "smokegrenade_projectile", "hegrenade_projectile", "molotov_projectile", "decoy_projectile", "flashbang_projectile" };

	private readonly Dictionary<nint, CCSPlayerController> _grenadesThrownByPlayers = new Dictionary<nint, CCSPlayerController>();

	public override string ClassName => "NoExplosives";

	public override List<string> Listeners => new List<string> { "OnEntitySpawned", "OnEntityTakeDamagePre" };

	public NoExplosives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (player == null || !player.IsValid || player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid)
		{
			return;
		}
		_players.Add(player);
		NotifyPlayers(player, ClassName, new Dictionary<string, string>
		{
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			}
		});
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
	}

	public override void Reset()
	{
		_players.Clear();
		_grenadesThrownByPlayers.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnEntitySpawned(CEntityInstance entity)
	{
		if (_players.Count != 0 && entity != null && _grenadeProjectiles.Contains(entity.DesignerName))
		{
			DiceNoExplosivesHandle(entity.Handle);
		}
	}

	public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		if (entity == null || !entity.IsValid || info.Inflictor == null || !info.Inflictor.IsValid || info.Inflictor.Value == null || !info.Inflictor.Value.IsValid || !_grenadesThrownByPlayers.ContainsKey(info.Inflictor.Value.Handle))
		{
			return HookResult.Continue;
		}
		nint handle = info.Inflictor.Value.Handle;
		info.Attacker.Raw = _grenadesThrownByPlayers[handle].Pawn.Raw;
		info.BitsDamageType = (DamageTypes_t)524288;
		return HookResult.Changed;
	}

	private void DiceNoExplosivesHandle(nint handle)
	{
		Server.NextFrame(delegate
		{
			if (handle == IntPtr.Zero)
			{
				return;
			}
			CBaseGrenade grenade = new CBaseGrenade((IntPtr)handle);
			if (!grenade.IsValid || grenade.Handle == IntPtr.Zero)
			{
				return;
			}
			Vector origin = ((CBaseEntity)grenade).AbsOrigin;
			CCSPlayerPawn thrower = grenade.OriginalThrower?.Value;
			if (origin == null || thrower == null || !thrower.IsValid || thrower.Controller?.Value == null)
			{
				return;
			}
			CCSPlayerController throwerController = thrower.Controller.Value.As<CCSPlayerController>();
			if (throwerController == null || !throwerController.IsValid || !IsDisabledByAura(throwerController, origin))
			{
				return;
			}
			NoExplosivesConfig cfg = _config.Dices.NoExplosives;
			if (cfg.RandomModels.Count != 0)
			{
				string model = cfg.RandomModels[new Random().Next(cfg.RandomModels.Count)];
				nint inflictorHandle = CreatePhysicsModel(model, cfg.ModelScale, origin, new QAngle(0f, 0f, 0f), new Vector(((CBaseEntity)grenade).Velocity.X, ((CBaseEntity)grenade).Velocity.Y, ((CBaseEntity)grenade).Velocity.Z));
				_grenadesThrownByPlayers.Add(inflictorHandle, throwerController);
				new Timer(10f, delegate
				{
					_grenadesThrownByPlayers.Remove(inflictorHandle);
				}, (TimerFlags?)null);
				((CBaseEntity)grenade).EmitSound("StopSoundEvents.StopAllExceptMusic", null, 1f, 0f);
				grenade.AcceptInput("Kill", null, null, "", 0);
				throwerController.PrintToCenterAlert("🚫 你在哑火力场内，爆炸物失效！");
			}
		});
	}

	private bool IsDisabledByAura(CCSPlayerController thrower, Vector position)
	{
		float radius = _config.Dices.NoExplosives.Radius;
		foreach (CCSPlayerController holder in _players.ToList())
		{
			if (holder == null || !holder.IsValid || ((CBaseEntity)holder).TeamNum == ((CBaseEntity)thrower).TeamNum)
			{
				continue;
			}
			CCSPlayerPawn pawn = holder.PlayerPawn?.Value;
			if (pawn == null || !pawn.IsValid || ((CBaseEntity)pawn).LifeState != 0)
			{
				continue;
			}
			Vector holderPos = ((CBaseEntity)pawn).AbsOrigin;
			if (holderPos != null && Vectors.GetDistance(holderPos, position) <= radius)
			{
				return true;
			}
		}
		return false;
	}

	private static nint CreatePhysicsModel(string model, float scale, Vector origin, QAngle angles, Vector velocity)
	{
		CPhysicsProp prop = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_multiplayer");
		if (prop == null || !prop.IsValid)
		{
			return 0;
		}
		((CBaseEntity)prop).Health = 10;
		((CBaseEntity)prop).MaxHealth = 10;
		CEntityKeyValues keyValues = new CEntityKeyValues();
		keyValues.SetFloat("modelscale", scale);
		((CBaseModelEntity)prop).SetModel(model);
		((CBaseEntity)prop).DispatchSpawn(keyValues);
		((CBaseEntity)prop).Teleport(origin, angles, velocity);
		return prop.Handle;
	}
}
