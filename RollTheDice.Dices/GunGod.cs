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

public class GunGod : DiceBlueprint
{
	private static readonly HashSet<string> _grenadeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile", "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile" };

	public override string ClassName => "GunGod";

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

	public GunGod(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			float reduction = (DiceSynergy.HasPartner(player, "NoRecoil") ? 0.75f : 0.66f);
			DamageReductionManager.Register(player, "GunGod", reduction);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			if (DiceSynergy.HasPartner(player, "NoRecoil"))
			{
				DiceSynergy.AnnounceCombo(player, "完美枪械", "减伤提升至 75%，任意武器零扩散！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		_players.Remove(player);
		DamageReductionManager.Unregister(player, "GunGod");
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players)
		{
			DamageReductionManager.Unregister(item, "GunGod");
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		foreach (CCSPlayerController player in _players)
		{
			if (player == null || !player.IsValid)
			{
				continue;
			}
			float reduction = (DiceSynergy.HasPartner(player, "NoRecoil") ? 0.75f : 0.66f);
			DamageReductionManager.Register(player, "GunGod", reduction);
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || info.Damage <= 0f)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
		{
			return (HookResult)0;
		}
		CHandle<CBaseEntity> inflictor = info.Inflictor;
		object obj3;
		if (inflictor == null)
		{
			obj3 = null;
		}
		else
		{
			CBaseEntity value2 = inflictor.Value;
			obj3 = ((value2 != null) ? ((CEntityInstance)value2).DesignerName : null);
		}
		string text = (string)obj3;
		if (text != null && _grenadeTypes.Contains(text))
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		if (((uint)info.BitsDamageType & 8u) != 0)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		if (((uint)info.BitsDamageType & 4u) != 0)
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)1;
	}
}
