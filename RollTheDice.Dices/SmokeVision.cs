using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class SmokeVision : DiceBlueprint
{
	private bool _comboActive;

	private readonly List<CSmokeGrenadeProjectile> _activeSmokes = new List<CSmokeGrenadeProjectile>();

	private readonly Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)>> _enemyGlows = new Dictionary<CCSPlayerController, Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>>();

	public override string ClassName => "SmokeVision";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventSmokegrenadeDetonate";
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
			span[num2] = "OnTick";
			num2++;
			span[num2] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public SmokeVision(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "SmokeBomb");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "烟雾掌控", "烟雾穿透加速");
			}
			_enemyGlows[player] = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (_enemyGlows.TryGetValue(player, out Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> value))
		{
			foreach (var value2 in value.Values)
			{
				GlowUtil.RemoveGlow((CBaseEntity?)(object)value2.Item1, (CBaseEntity?)(object)value2.Item2);
			}
			value.Clear();
		}
		_players.Remove(player);
		_enemyGlows.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_enemyGlows.Clear();
		_activeSmokes.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		CSmokeGrenadeProjectile val = Utilities.FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>("smokegrenade_projectile").FirstOrDefault((CSmokeGrenadeProjectile s) => ((CEntityInstance)s).IsValid && ((CBaseEntity)s).AbsOrigin != null);
		if ((CEntityInstance)(object)val != (CEntityInstance)null)
		{
			_activeSmokes.Add(val);
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		if (_players.Count == 0 || Server.TickCount % 8 != 0)
		{
			return;
		}
		_activeSmokes.RemoveAll((CSmokeGrenadeProjectile s) => (CEntityInstance)(object)s == (CEntityInstance)null || !((CEntityInstance)s).IsValid || ((CBaseEntity)s).AbsOrigin == null);
		Color color = Color.FromArgb(_config.Dices.SmokeVision.GlowColorTRed, _config.Dices.SmokeVision.GlowColorTGreen, _config.Dices.SmokeVision.GlowColorTBlue);
		Color color2 = Color.FromArgb(_config.Dices.SmokeVision.GlowColorCTRed, _config.Dices.SmokeVision.GlowColorCTGreen, _config.Dices.SmokeVision.GlowColorCTBlue);
		foreach (CCSPlayerController player in _players.ToList())
		{
			if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid || ((CBaseEntity)((CBasePlayerController)player).Pawn.Value).LifeState != 0)
			{
				continue;
			}
			if (!_enemyGlows.ContainsKey(player))
			{
				_enemyGlows[player] = new Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)>();
			}
			Dictionary<CCSPlayerController, (CDynamicProp, CDynamicProp)> dictionary = _enemyGlows[player];
			HashSet<CCSPlayerController> hashSet = new HashSet<CCSPlayerController>();
			foreach (CCSPlayerController enemy in from p in Utilities.GetPlayers()
				where (CEntityInstance)(object)p != (CEntityInstance)(object)player && ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0 && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum
				select p)
			{
				if (!_activeSmokes.Any((CSmokeGrenadeProjectile smoke) => ((CBaseEntity)smoke).AbsOrigin != null && ((CBaseEntity)((CBasePlayerController)enemy).Pawn.Value).AbsOrigin != null && Vectors.GetDistance(((CBaseEntity)smoke).AbsOrigin, ((CBaseEntity)((CBasePlayerController)enemy).Pawn.Value).AbsOrigin) < 150f))
				{
					continue;
				}
				hashSet.Add(enemy);
				if (!dictionary.ContainsKey(enemy))
				{
					(CDynamicProp, CDynamicProp) value = GlowUtil.CreateGlow((CBaseEntity)(object)((CBasePlayerController)enemy).Pawn.Value, (((CBaseEntity)enemy).TeamNum == 2) ? color : color2);
					if ((CEntityInstance)(object)value.Item1 != (CEntityInstance)null && (CEntityInstance)(object)value.Item2 != (CEntityInstance)null)
					{
						dictionary[enemy] = value;
					}
				}
			}
			foreach (KeyValuePair<CCSPlayerController, (CDynamicProp, CDynamicProp)> item in dictionary.ToList())
			{
				if (!hashSet.Contains(item.Key))
				{
					GlowUtil.RemoveGlow((CBaseEntity?)(object)item.Value.Item1, (CBaseEntity?)(object)item.Value.Item2);
					dictionary.Remove(item.Key);
				}
			}
			if (DiceSynergy.HasPartner(player, "SmokeBomb") && _activeSmokes.Count > 0)
			{
				bool flag = _activeSmokes.Any((CSmokeGrenadeProjectile s) => ((s != null) ? ((CBaseEntity)s).AbsOrigin : null) != null && ((CBaseEntity)((CBasePlayerController)player).Pawn.Value).AbsOrigin != null && Vectors.GetDistance(((CBaseEntity)s).AbsOrigin, ((CBaseEntity)((CBasePlayerController)player).Pawn.Value).AbsOrigin) < 400f);
				player.PlayerPawn.Value.VelocityModifier = (flag ? 1.5f : 1f);
				Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
			}
		}
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0 || _activeSmokes.Count == 0)
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
		if ((CEntityInstance)(object)val == (CEntityInstance)null || !((CEntityInstance)val).IsValid || !_players.Contains(val))
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
				CBasePlayerController value3 = controller2.Value;
				obj4 = ((value3 != null) ? ((NativeObject)value3).As<CCSPlayerController>() : null);
			}
		}
		CCSPlayerController victim = (CCSPlayerController)obj4;
		if (!((CEntityInstance)(object)victim == (CEntityInstance)null) && ((CEntityInstance)victim).IsValid)
		{
			CHandle<CCSPlayerPawn> playerPawn = victim.PlayerPawn;
			object obj5;
			if (playerPawn == null)
			{
				obj5 = null;
			}
			else
			{
				CCSPlayerPawn value4 = playerPawn.Value;
				obj5 = ((value4 != null) ? ((CBaseEntity)value4).AbsOrigin : null);
			}
			if (obj5 != null)
			{
				if (_activeSmokes.Any((CSmokeGrenadeProjectile s) => ((s != null) ? ((CBaseEntity)s).AbsOrigin : null) != null && Vectors.GetDistance(((CBaseEntity)s).AbsOrigin, ((CBaseEntity)victim.PlayerPawn.Value).AbsOrigin) < 150f))
				{
					info.Damage = (int)(info.Damage * 1.3f);
					return (HookResult)1;
				}
				return (HookResult)0;
			}
		}
		return (HookResult)0;
	}
}
