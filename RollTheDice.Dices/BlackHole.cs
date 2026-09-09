using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class BlackHole : DiceBlueprint
{
	private bool _comboActive;

	private bool _collapseCombo;

	private int _tickCounter;

	private static readonly string[] PullableEntities = new string[47]
	{
		"hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile", "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile", "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_awp",
		"weapon_ssg08", "weapon_scar20", "weapon_g3sg1", "weapon_aug", "weapon_sg556", "weapon_galilar", "weapon_famas", "weapon_p90", "weapon_mp9", "weapon_mac10",
		"weapon_mp7", "weapon_mp5sd", "weapon_ump45", "weapon_bizon", "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249", "weapon_negev",
		"weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_glock", "weapon_hkp2000", "weapon_p250", "weapon_tec9", "weapon_usp_silencer", "weapon_cz75a", "weapon_revolver",
		"weapon_taser", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy"
	};

	public override string ClassName => "BlackHole";

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
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnTick";
			return list;
		}
	}

	public BlackHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "GravityWell");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "引力深渊", "黑洞吸入玩家+武器，半径翻倍！");
			}
			_collapseCombo = DiceSynergy.HasPartner(player, "WhiteHole");
			if (_collapseCombo)
			{
				DiceSynergy.AnnounceCombo(player, "坍缩", "死时坍缩10s→爆开！");
			}
			CCSPlayerPawn value = player.PlayerPawn.Value;
			((CBaseModelEntity)value).Render = Color.FromArgb(255, 0, 0, 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseModelEntity", "m_clrRender", 0);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if ((CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			((CBaseModelEntity)player.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
		_players.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			if ((CEntityInstance)(object)((item == null) ? null : item.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)item.PlayerPawn.Value).IsValid)
			{
				((CBaseModelEntity)item.PlayerPawn.Value).Render = Color.FromArgb(255, 255, 255, 255);
				Utilities.SetStateChanged((CBaseEntity)(object)item.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender", 0);
			}
		}
		_players.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		if (!DiceSynergy.HasPartner(userid, "WhiteHole"))
		{
			return (HookResult)0;
		}
		CHandle<CCSPlayerPawn> playerPawn = userid.PlayerPawn;
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
			return (HookResult)0;
		}
		Vector deathPos = new Vector((float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin.Z);
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udca5 坍缩开始！10秒后黑洞爆开！");
		Server.NextFrame((Action)delegate
		{
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Expected O, but got Unknown
			//IL_00a3: Expected O, but got Unknown
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			CBeam val = Utilities.CreateEntityByName<CBeam>("beam");
			if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid)
			{
				((CBaseModelEntity)val).Render = Color.FromArgb(255, 100, 0, 200);
				val.Width = 4f;
				((CBaseEntity)val).Teleport(deathPos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
				val.EndPos.X = deathPos.X;
				val.EndPos.Y = deathPos.Y;
				val.EndPos.Z = deathPos.Z + 50f;
				((CBaseEntity)val).DispatchSpawn();
				CBeam capturedBeam = val;
				new Timer(9.5f, (Action)delegate
				{
					if ((CEntityInstance)(object)capturedBeam != (CEntityInstance)null && ((CEntityInstance)capturedBeam).IsValid)
					{
						((CEntityInstance)capturedBeam).Remove();
					}
				}, (TimerFlags?)null);
			}
		});
		Vector capPos = deathPos;
		new Timer(10f, (Action)delegate
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Expected O, but got Unknown
			//IL_0078: Expected O, but got Unknown
			CBaseEntity val = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
			if ((CEntityInstance)(object)val != (CEntityInstance)null && ((CEntityInstance)val).IsValid)
			{
				val.Teleport(capPos, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
				val.DispatchSpawn();
				((CEntityInstance)val).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udca5 坍缩！黑洞爆开了！");
			}
		}, (TimerFlags?)null);
		return (HookResult)0;
	}

	public void OnTick()
	{
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Expected O, but got Unknown
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Expected O, but got Unknown
		if (_players.Count == 0)
		{
			return;
		}
		_tickCounter++;
		if (_tickCounter % 16 != 0)
		{
			return;
		}
		float pullStrength = _config.Dices.BlackHole.PullStrength;
		float num2 = 0.25f;
		foreach (CCSPlayerController player in _players.ToList())
		{
			CCSPlayerController obj = player;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				CHandle<CCSPlayerPawn> playerPawn = obj.PlayerPawn;
				if (playerPawn == null)
				{
					obj2 = null;
				}
				else
				{
					CCSPlayerPawn value = playerPawn.Value;
					obj2 = ((value != null) ? ((CBaseEntity)value).AbsOrigin : null);
				}
			}
			if (obj2 == null || ((CBaseEntity)player.PlayerPawn.Value).LifeState != 0)
			{
				continue;
			}
			Vector absOrigin = ((CBaseEntity)player.PlayerPawn.Value).AbsOrigin;
			float num = (DiceSynergy.HasPartner(player, "GravityWell") ? (_config.Dices.BlackHole.PullRadius * 2f) : _config.Dices.BlackHole.PullRadius);
			float num3 = num * num;
			string[] pullableEntities = PullableEntities;
			foreach (string text in pullableEntities)
			{
				IEnumerable<CBaseEntity> enumerable = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(text);
				foreach (CBaseEntity item in enumerable)
				{
					if (((item != null) ? item.AbsOrigin : null) == null || !((CEntityInstance)item).IsValid)
					{
						continue;
					}
					if (text.StartsWith("weapon_"))
					{
						CBasePlayerWeapon val = ((NativeObject)item).As<CBasePlayerWeapon>();
						if ((CEntityInstance)(object)((val == null) ? null : ((CBaseEntity)val).OwnerEntity?.Value) != (CEntityInstance)null)
						{
							continue;
						}
					}
					float num4 = absOrigin.X - item.AbsOrigin.X;
					float num5 = absOrigin.Y - item.AbsOrigin.Y;
					float num6 = absOrigin.Z - item.AbsOrigin.Z;
					float num7 = num4 * num4 + num5 * num5 + num6 * num6;
					if (!(num7 > num3) && !(num7 < 1f))
					{
						float num8 = MathF.Sqrt(num7);
						float num9 = pullStrength * num2 * (1f - num8 / num);
						if (num9 > num8)
						{
							num9 = num8;
						}
						float num10 = num4 / num8;
						float num11 = num5 / num8;
						float num12 = num6 / num8;
						Vector val2 = new Vector((float?)(item.AbsOrigin.X + num10 * num9), (float?)(item.AbsOrigin.Y + num11 * num9), (float?)(item.AbsOrigin.Z + num12 * num9 + 3f * num2));
						item.Teleport(val2, item.AbsRotation, item.AbsVelocity);
					}
				}
			}
			foreach (CCSPlayerController item2 in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p != (CEntityInstance)(object)player && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
				select p)
			{
				Vector absOrigin2 = ((CBaseEntity)item2.PlayerPawn.Value).AbsOrigin;
				float num13 = absOrigin.X - absOrigin2.X;
				float num14 = absOrigin.Y - absOrigin2.Y;
				float num15 = absOrigin.Z - absOrigin2.Z;
				float num16 = num13 * num13 + num14 * num14 + num15 * num15;
				if (!(num16 > num3) && !(num16 < 1f))
				{
					float num17 = MathF.Sqrt(num16);
					float num18 = pullStrength * 0.3f * num2 * (1f - num17 / num);
					if (num18 > num17)
					{
						num18 = num17;
					}
					float num19 = num13 / num17;
					float num20 = num14 / num17;
					float num21 = num15 / num17;
					Vector val3 = new Vector((float?)(num19 * num18 / num2), (float?)(num20 * num18 / num2), (float?)(num21 * num18 / num2));
					((CBaseEntity)item2.PlayerPawn.Value).Teleport((Vector)null, (QAngle)null, val3);
					if (Server.TickCount % 128 == 0)
					{
						item2.PrintToCenterAlert("\ud83c\udf0c 被黑洞引力吸入！");
					}
				}
			}
		}
	}
}
