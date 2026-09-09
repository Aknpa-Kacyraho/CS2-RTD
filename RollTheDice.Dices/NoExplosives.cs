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

public class NoExplosives : DiceBlueprint
{
	private readonly HashSet<string> _grenadeProjectiles = new HashSet<string> { "smokegrenade_projectile", "hegrenade_projectile", "molotov_projectile", "decoy_projectile", "flashbang_projectile" };

	public readonly Random _random = new Random();

	private readonly Dictionary<nint, CCSPlayerController> _grenadesThrownByPlayers = new Dictionary<nint, CCSPlayerController>();

	public override string ClassName => "NoExplosives";

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
			span[num2] = "OnEntityTakeDamagePre";
			return list;
		}
	}

	public NoExplosives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
		Server.NextFrame((Action)delegate
		{
			List<CCSPlayerController> list = (from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0
				select p).ToList();
			if (list.Count == 0)
			{
				return;
			}
			Random rng = new Random();
			int count = Math.Min(2, list.Count);
			List<CCSPlayerController> list2 = list.OrderBy((CCSPlayerController _) => rng.Next()).Take(count).ToList();
			foreach (CCSPlayerController item in list2)
			{
				if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid)
				{
					_players.Add(item);
					NotifyPlayers(item, ClassName, new Dictionary<string, string> { 
					{
						"playerName",
						((CBasePlayerController)item).PlayerName
					} });
				}
			}
		});
		NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
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
		if (_players.Count != 0 && _grenadeProjectiles.Contains(entity.DesignerName))
		{
			DiceNoExplosivesHandle(((NativeEntity)entity).Handle);
		}
	}

	public HookResult OnEntityTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		if (entity == null || !((CEntityInstance)entity).IsValid || info.Inflictor == null || !info.Inflictor.IsValid || info.Inflictor.Value == null || !((CEntityInstance)info.Inflictor.Value).IsValid || !_grenadesThrownByPlayers.ContainsKey(((NativeEntity)info.Inflictor.Value).Handle))
		{
			return (HookResult)0;
		}
		nint handle = ((NativeEntity)info.Inflictor.Value).Handle;
		info.Attacker.Raw = ((CBasePlayerController)_grenadesThrownByPlayers[handle]).Pawn.Raw;
		info.BitsDamageType = (DamageTypes_t)524288;
		return (HookResult)1;
	}

	private void DiceNoExplosivesHandle(nint handle)
	{
		Server.NextFrame((Action)delegate
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Expected O, but got Unknown
			//IL_018a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cc: Expected O, but got Unknown
			//IL_01cc: Expected O, but got Unknown
			//IL_0215: Unknown result type (might be due to invalid IL or missing references)
			if (handle != IntPtr.Zero)
			{
				CBaseGrenade val = new CBaseGrenade((IntPtr)handle);
				if (((CEntityInstance)val).IsValid && ((NativeEntity)val).Handle != (IntPtr)IntPtr.Zero && ((CBaseEntity)val).AbsOrigin != null)
				{
					CCSPlayerPawn val2 = val.OriginalThrower?.Value;
					if (!((CEntityInstance)(object)val2 == (CEntityInstance)null) && ((CEntityInstance)val2).IsValid && (CEntityInstance)(object)((CBasePlayerPawn)val2).Controller?.Value != (CEntityInstance)null && ((IEnumerable<CBasePlayerController>)_players).Contains(((CBasePlayerPawn)val2).Controller.Value))
					{
						if (_config.Dices.NoExplosives.RandomModels.Count != 0)
						{
							string model = _config.Dices.NoExplosives.RandomModels[new Random().Next(_config.Dices.NoExplosives.RandomModels.Count)];
							nint inflictorHandler = CreatePhysicsModel(model, _config.Dices.NoExplosives.ModelScale, ((CBaseEntity)val).AbsOrigin, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)((CBaseEntity)val).Velocity.X, (float?)((CBaseEntity)val).Velocity.Y, (float?)((CBaseEntity)val).Velocity.Z));
							_grenadesThrownByPlayers.Add(inflictorHandler, ((NativeObject)((CBasePlayerPawn)val2).Controller.Value).As<CCSPlayerController>());
							new Timer(10f, (Action)delegate
							{
								_grenadesThrownByPlayers.Remove(inflictorHandler);
							}, (TimerFlags?)null);
							((CBaseEntity)val).EmitSound("StopSoundEvents.StopAllExceptMusic", (RecipientFilter)null, 1f, 0f);
							((CEntityInstance)val).AcceptInput("Kill", (CEntityInstance)null, (CEntityInstance)null, "", 0);
						}
					}
				}
			}
		});
	}

	private static nint CreatePhysicsModel(string model, float scale, Vector origin, QAngle angles, Vector velocity)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		CPhysicsProp val = Utilities.CreateEntityByName<CPhysicsProp>("prop_physics_multiplayer");
		if ((CEntityInstance)(object)val == (CEntityInstance)null)
		{
			return 0;
		}
		((CBaseEntity)val).Health = 10;
		((CBaseEntity)val).MaxHealth = 10;
		CEntityKeyValues val2 = new CEntityKeyValues();
		val2.SetFloat("modelscale", scale);
		((CBaseModelEntity)val).SetModel(model);
		((CBaseEntity)val).DispatchSpawn(val2);
		((CBaseEntity)val).Teleport(origin, angles, velocity);
		return ((NativeEntity)val).Handle;
	}
}
