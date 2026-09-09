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

public class GrenadeKing : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<uint, (float Multiplier, float RadiusMult, CCSPlayerController Owner)> _trackedNades = new Dictionary<uint, (float, float, CCSPlayerController)>();

	public override string ClassName => "GrenadeKing";

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
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public GrenadeKing(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Martyrdom");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "爆炸艺术家", "手雷伤害+50% 殉道爆炸范围翻倍");
			}
			float multiplierMin = _config.Dices.GrenadeKing.MultiplierMin;
			float multiplierMax = _config.Dices.GrenadeKing.MultiplierMax;
			float num = (float)Math.Round(_random.NextDouble() * (double)(multiplierMax - multiplierMin) + (double)multiplierMin, 2);
			NotifyPlayers(player, ClassName, new Dictionary<string, string>
			{
				{
					"playerName",
					((CBasePlayerController)player).PlayerName
				},
				{
					"multiplier",
					num.ToString()
				}
			});
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
		if (_players.Count == 0 || entity.DesignerName != "hegrenade_projectile")
		{
			return;
		}
		Server.NextFrame((Action)delegate
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			if (!(entity == (CEntityInstance)null) && entity.IsValid)
			{
				CHEGrenadeProjectile val = new CHEGrenadeProjectile(((NativeEntity)entity).Handle);
				if (((CEntityInstance)val).IsValid)
				{
					CCSPlayerPawn val2 = ((CBaseGrenade)val).Thrower?.Value;
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
							float multiplierMin = _config.Dices.GrenadeKing.MultiplierMin;
							float multiplierMax = _config.Dices.GrenadeKing.MultiplierMax;
							float num = (float)Math.Round(_random.NextDouble() * (double)(multiplierMax - multiplierMin) + (double)multiplierMin, 2);
							float num2 = 1f + (num - 1f) * 0.5f;
							if (DiceSynergy.HasPartner(val3, "Martyrdom"))
							{
								num += 0.5f;
								num2 *= 2f;
							}
							((CBaseGrenade)val).Damage *= num;
							((CBaseGrenade)val).DmgRadius *= num2;
							_trackedNades[((CEntityInstance)val).Index] = (num, num2, val3);
						}
					}
				}
			}
		});
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		if ((CEntityInstance)(object)info.Attacker?.Value == (CEntityInstance)null)
		{
			return (HookResult)0;
		}
		CCSPlayerPawn obj = ((NativeObject)info.Attacker.Value).As<CCSPlayerPawn>();
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
		if ((CEntityInstance)(object)info.Inflictor?.Value != (CEntityInstance)null && ((CEntityInstance)info.Inflictor.Value).DesignerName == "hegrenade_projectile" && _trackedNades.TryGetValue(((CEntityInstance)info.Inflictor.Value).Index, out (float, float, CCSPlayerController) value2))
		{
			info.Damage *= value2.Item1;
			return (HookResult)1;
		}
		return (HookResult)0;
	}
}
