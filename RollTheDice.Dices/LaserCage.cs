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

public class LaserCage : DiceBlueprint
{
	private bool _comboActive;

	private readonly Dictionary<CCSPlayerController, List<CBeam>> _activeBeams = new Dictionary<CCSPlayerController, List<CBeam>>();

	private readonly Dictionary<CCSPlayerController, float> _lastDamageTime = new Dictionary<CCSPlayerController, float>();

	private static readonly HashSet<string> PistolWeapons = new HashSet<string> { "weapon_glock", "weapon_usp_silencer", "weapon_p250", "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_tec9", "weapon_hkp2000", "weapon_cz75a", "weapon_revolver" };

	private float _rotationAngle;

	public override string ClassName => "LaserCage";

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

	public LaserCage(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			DamageReductionManager.Register(player, "LaserCage", 0.3f);
			_comboActive = DiceSynergy.HasPartner(player, "ThunderChain");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "雷光炼狱", "免疫手枪+30%枪械减伤，激光伤害翻倍！");
			}
			_lastDamageTime[player] = 0f;
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		DestroyBeams(player);
		DamageReductionManager.Unregister(player, "LaserCage");
		_players.Remove(player);
		_lastDamageTime.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			DestroyBeams(item);
			DamageReductionManager.Unregister(item, "LaserCage");
		}
		_players.Clear();
		_lastDamageTime.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	private void DestroyBeams(CCSPlayerController player)
	{
		if (!_activeBeams.TryGetValue(player, out List<CBeam> value))
		{
			return;
		}
		foreach (CBeam item in value)
		{
			if ((CEntityInstance)(object)item != (CEntityInstance)null && ((CEntityInstance)item).IsValid)
			{
				((CEntityInstance)item).Remove();
			}
		}
		value.Clear();
		_activeBeams.Remove(player);
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
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
		if (text != null && PistolWeapons.Contains(text))
		{
			info.Damage = 0f;
			return (HookResult)1;
		}
		return (HookResult)0;
	}

	public void OnTick()
	{
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Expected O, but got Unknown
		//IL_0293: Expected O, but got Unknown
		//IL_0293: Expected O, but got Unknown
		if (_players.Count == 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		_rotationAngle += 1.5f * Server.TickInterval;
		foreach (CCSPlayerController player in _players.ToList())
		{
			try
			{
				if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid)
				{
					continue;
				}
				CHandle<CCSPlayerPawn> playerPawn = player.PlayerPawn;
				object obj;
				if (playerPawn == null)
				{
					obj = null;
				}
				else
				{
					CCSPlayerPawn value = playerPawn.Value;
					obj = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
				}
				if (obj == null)
				{
					continue;
				}
				CCSPlayerPawn value2 = player.PlayerPawn.Value;
				if (((CBaseEntity)value2).LifeState != 0)
				{
					DestroyBeams(player);
					continue;
				}
				float radius = _config.Dices.LaserCage.Radius;
				int beamCount = _config.Dices.LaserCage.BeamCount;
				Vector absOrigin = ((CBaseEntity)value2).AbsOrigin;
				DestroyBeams(player);
				_activeBeams[player] = new List<CBeam>();
				for (int i = 0; i < beamCount; i++)
				{
					float num2 = _rotationAngle + (float)i * 2f * (float)Math.PI / (float)beamCount;
					float value3 = absOrigin.X + MathF.Cos(num2) * radius;
					float value4 = absOrigin.Y + MathF.Sin(num2) * radius;
					float num3 = absOrigin.X + MathF.Cos(num2 + 0.3f) * radius;
					float num4 = absOrigin.Y + MathF.Sin(num2 + 0.3f) * radius;
					CBeam val = Utilities.CreateEntityByName<CBeam>("beam");
					if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid)
					{
						((CBaseModelEntity)val).Render = Color.FromArgb(255, 255, 50, 50);
						val.Width = 1.5f;
						((CBaseEntity)val).Teleport(new Vector((float?)value3, (float?)value4, (float?)absOrigin.Z), new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
						val.EndPos.X = num3;
						val.EndPos.Y = num4;
						val.EndPos.Z = absOrigin.Z + 90f;
						((CBaseEntity)val).DispatchSpawn();
						_activeBeams[player].Add(val);
					}
				}
				float tickInterval = _config.Dices.LaserCage.TickInterval;
				if (_lastDamageTime.TryGetValue(player, out var value5) && num - value5 < tickInterval)
				{
					continue;
				}
				_lastDamageTime[player] = num;
				int num5 = (DiceSynergy.HasPartner(player, "ThunderChain") ? (_config.Dices.LaserCage.DamagePerTouch * 2) : _config.Dices.LaserCage.DamagePerTouch);
				foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && ((CBaseEntity)p).TeamNum != ((CBaseEntity)player).TeamNum && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
					select p)
				{
					float distance = Vectors.GetDistance(absOrigin, ((CBaseEntity)item.PlayerPawn.Value).AbsOrigin);
					if (!(distance <= radius + 40f))
					{
						continue;
					}
					CCSPlayerPawn value6 = item.PlayerPawn.Value;
					((CBaseEntity)value6).Health -= num5;
					Utilities.SetStateChanged((CBaseEntity)(object)value6, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)value6).Health > 0)
					{
						continue;
					}
					if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
					{
						((CBasePlayerPawn)value6).CommitSuicide(false, true);
						continue;
					}
					try
					{
						((CBasePlayerPawn)value6).CommitSuicide(false, true);
					}
					catch
					{
						((CBaseEntity)value6).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)value6, "CBaseEntity", "m_iHealth", 0);
					}
				}
			}
			catch
			{
				DestroyBeams(player);
			}
		}
	}
}
