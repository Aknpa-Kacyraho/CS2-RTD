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

public class WhiteHole : DiceBlueprint
{
	private bool _comboActive;

	private Vector? _whiteHolePos;

	private float _whiteHoleEndTime;

	private int _ownerTeam;

	private CParticleSystem? _particle;

	private CBeam? _beam;

	private bool _collapsePull;

	private float _collapseEndTime;

	private float _lastCollapseDamage;

	private static readonly string[] PushableEntities = new string[48]
	{
		"hegrenade_projectile", "flashbang_projectile", "smokegrenade_projectile", "molotov_projectile", "incendiarygrenade_projectile", "decoy_projectile", "weapon_c4", "weapon_ak47", "weapon_m4a1", "weapon_m4a1_silencer",
		"weapon_awp", "weapon_ssg08", "weapon_scar20", "weapon_g3sg1", "weapon_aug", "weapon_sg556", "weapon_galilar", "weapon_famas", "weapon_p90", "weapon_mp9",
		"weapon_mac10", "weapon_mp7", "weapon_mp5sd", "weapon_ump45", "weapon_bizon", "weapon_nova", "weapon_xm1014", "weapon_mag7", "weapon_sawedoff", "weapon_m249",
		"weapon_negev", "weapon_deagle", "weapon_elite", "weapon_fiveseven", "weapon_glock", "weapon_hkp2000", "weapon_p250", "weapon_tec9", "weapon_usp_silencer", "weapon_cz75a",
		"weapon_revolver", "weapon_taser", "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade", "weapon_molotov", "weapon_incgrenade", "weapon_decoy"
	};

	public override string ClassName => "WhiteHole";

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

	public WhiteHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "BlackHole");
		if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "坍缩", "死时坍缩10s→爆开！");
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
		CleanupWhiteHole();
		_whiteHolePos = null;
		_whiteHoleEndTime = 0f;
		_collapsePull = false;
		_collapseEndTime = 0f;
		_lastCollapseDamage = 0f;
	}

	public override void Destroy()
	{
		Reset();
	}

	private void CleanupWhiteHole()
	{
		if ((CEntityInstance)(object)_particle != (CEntityInstance)null && ((CEntityInstance)_particle).IsValid)
		{
			((CEntityInstance)_particle).Remove();
		}
		_particle = null;
		if ((CEntityInstance)(object)_beam != (CEntityInstance)null && ((CEntityInstance)_beam).IsValid)
		{
			((CEntityInstance)_beam).Remove();
		}
		_beam = null;
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
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
		CleanupWhiteHole();
		CCSPlayerPawn value2 = userid.PlayerPawn.Value;
		_whiteHolePos = new Vector((float?)((CBaseEntity)value2).AbsOrigin.X, (float?)((CBaseEntity)value2).AbsOrigin.Y, (float?)((CBaseEntity)value2).AbsOrigin.Z);
		_ownerTeam = ((CBaseEntity)userid).TeamNum;
		if (DiceSynergy.HasPartner(userid, "BlackHole"))
		{
			_collapsePull = true;
			_collapseEndTime = Server.CurrentTime + 10f;
			_lastCollapseDamage = Server.CurrentTime;
			SpawnVisual(_whiteHolePos);
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udca5 坍缩开始！10秒后爆开！");
		}
		else
		{
			_whiteHoleEndTime = Server.CurrentTime + _config.Dices.WhiteHole.Duration;
			SpawnVisual(_whiteHolePos);
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_WhiteHole_spawn"].Value.Replace("{playerName}", ((CBasePlayerController)userid).PlayerName));
		}
		return (HookResult)0;
	}

	private void SpawnVisual(Vector pos)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_008c: Expected O, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_0140: Expected O, but got Unknown
		_particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
		if ((CEntityInstance)(object)_particle != (CEntityInstance)null)
		{
			_particle.EffectName = "particles/ui/status_effects/speed_boost.vpcf";
			_particle.StartActive = true;
			((CBaseEntity)_particle).Teleport(pos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			((CBaseEntity)_particle).DispatchSpawn();
		}
		_beam = Utilities.CreateEntityByName<CBeam>("beam");
		if ((CEntityInstance)(object)_beam != (CEntityInstance)null)
		{
			((CBaseModelEntity)_beam).Render = Color.FromArgb(200, 255, 255, 255);
			_beam.Width = 12f;
			((CBaseEntity)_beam).Teleport(pos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
			((CBaseEntity)_beam).DispatchSpawn();
		}
	}

	public void OnTick()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_00c2: Expected O, but got Unknown
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Expected O, but got Unknown
		//IL_0671: Unknown result type (might be due to invalid IL or missing references)
		//IL_067b: Expected O, but got Unknown
		//IL_0b21: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b28: Expected O, but got Unknown
		//IL_0cc0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc7: Expected O, but got Unknown
		//IL_0c7f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c86: Expected O, but got Unknown
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Expected O, but got Unknown
		//IL_098b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0992: Expected O, but got Unknown
		if (_whiteHolePos == null)
		{
			return;
		}
		float num = Server.CurrentTime;
		if (_collapsePull)
		{
			if (num >= _collapseEndTime)
			{
				_collapsePull = false;
				Vector whiteHolePos = _whiteHolePos;
				CleanupWhiteHole();
				CBaseEntity val = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
				if ((CEntityInstance)(object)val != (CEntityInstance)null)
				{
					val.Teleport(whiteHolePos, new QAngle((float?)null, (float?)null, (float?)null), new Vector((float?)null, (float?)null, (float?)null));
					val.DispatchSpawn();
					((CEntityInstance)val).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
				}
				float num2 = 800f;
				float num3 = 640000f;
				foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
					select p)
				{
					CCSPlayerPawn value = item.PlayerPawn.Value;
					float num4 = ((CBaseEntity)value).AbsOrigin.X - whiteHolePos.X;
					float num5 = ((CBaseEntity)value).AbsOrigin.Y - whiteHolePos.Y;
					float num6 = ((CBaseEntity)value).AbsOrigin.Z - whiteHolePos.Z;
					float num7 = num4 * num4 + num5 * num5 + num6 * num6;
					if (num7 > num3 || num7 < 1f)
					{
						continue;
					}
					float num8 = MathF.Sqrt(num7);
					float num9 = num2 * (1f - num8 / 800f);
					float num10 = num4 / num8;
					float num11 = num5 / num8;
					float num12 = num6 / num8;
					((CBaseEntity)value).Teleport((Vector)null, (QAngle)null, new Vector((float?)(num10 * num9), (float?)(num11 * num9), (float?)(num12 * num9 + 200f)));
					((CBaseEntity)value).Health -= 50;
					Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)value).Health > 0)
					{
						continue;
					}
					if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
					{
						((CBasePlayerPawn)value).CommitSuicide(false, true);
						continue;
					}
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
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\ud83d\udca5 坍缩爆炸！");
				_whiteHolePos = null;
			}
			else
			{
				if (Server.TickCount % 4 != 0)
				{
					return;
				}
				Vector whiteHolePos2 = _whiteHolePos;
				float num13 = 500f;
				float num14 = num13 * num13;
				float num15 = Server.TickInterval * 4f;
				string[] pushableEntities = PushableEntities;
				foreach (string text in pushableEntities)
				{
					IEnumerable<CBaseEntity> enumerable = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(text);
					foreach (CBaseEntity item2 in enumerable)
					{
						if (((item2 != null) ? item2.AbsOrigin : null) == null || !((CEntityInstance)item2).IsValid)
						{
							continue;
						}
						if (text.StartsWith("weapon_"))
						{
							CBasePlayerWeapon val2 = ((NativeObject)item2).As<CBasePlayerWeapon>();
							if ((CEntityInstance)(object)((val2 == null) ? null : ((CBaseEntity)val2).OwnerEntity?.Value) != (CEntityInstance)null)
							{
								continue;
							}
						}
						float num17 = whiteHolePos2.X - item2.AbsOrigin.X;
						float num18 = whiteHolePos2.Y - item2.AbsOrigin.Y;
						float num19 = whiteHolePos2.Z - item2.AbsOrigin.Z;
						float num20 = num17 * num17 + num18 * num18 + num19 * num19;
						if (!(num20 > num14) && !(num20 < 1f))
						{
							float num21 = MathF.Sqrt(num20);
							float num22 = 300f * num15 * (1f - num21 / num13);
							float num23 = num17 / num21;
							float num24 = num18 / num21;
							float num25 = num19 / num21;
							item2.Teleport(new Vector((float?)(item2.AbsOrigin.X + num23 * num22), (float?)(item2.AbsOrigin.Y + num24 * num22), (float?)(item2.AbsOrigin.Z + num25 * num22 + 3f * num15)), item2.AbsRotation, item2.AbsVelocity);
						}
					}
				}
				if (!(num - _lastCollapseDamage >= 1f))
				{
					return;
				}
				_lastCollapseDamage = num;
				foreach (CCSPlayerController item3 in from p in Utilities.GetPlayers()
					where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
					select p)
				{
					CCSPlayerPawn value2 = item3.PlayerPawn.Value;
					float num26 = whiteHolePos2.X - ((CBaseEntity)value2).AbsOrigin.X;
					float num27 = whiteHolePos2.Y - ((CBaseEntity)value2).AbsOrigin.Y;
					float num28 = whiteHolePos2.Z - ((CBaseEntity)value2).AbsOrigin.Z;
					float num29 = num26 * num26 + num27 * num27 + num28 * num28;
					if (num29 > num14 || num29 < 1f)
					{
						continue;
					}
					float num30 = MathF.Sqrt(num29);
					float num31 = num26 / num30;
					float num32 = num27 / num30;
					float num33 = num28 / num30;
					float num34 = 400f * (1f - num30 / num13);
					((CBaseEntity)value2).Teleport((Vector)null, (QAngle)null, new Vector((float?)(num31 * num34 / 0.064f), (float?)(num32 * num34 / 0.064f), (float?)(num33 * num34 / 0.064f)));
					((CBaseEntity)value2).Health -= 10;
					Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					if (((CBaseEntity)value2).Health > 0)
					{
						continue;
					}
					if (!item3.IsBot && !((CBasePlayerController)item3).IsHLTV)
					{
						((CBasePlayerPawn)value2).CommitSuicide(false, true);
						continue;
					}
					try
					{
						((CBasePlayerPawn)value2).CommitSuicide(false, true);
					}
					catch
					{
						((CBaseEntity)value2).Health = 0;
						Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
					}
				}
			}
		}
		else if (num >= _whiteHoleEndTime)
		{
			CleanupWhiteHole();
			_whiteHolePos = null;
		}
		else
		{
			if (Server.TickCount % 4 != 0)
			{
				return;
			}
			Vector whiteHolePos3 = _whiteHolePos;
			float pushRadius = _config.Dices.WhiteHole.PushRadius;
			float num35 = pushRadius * pushRadius;
			float enemyPushStrength = _config.Dices.WhiteHole.EnemyPushStrength;
			float teammatePullStrength = _config.Dices.WhiteHole.TeammatePullStrength;
			float entityPushStrength = _config.Dices.WhiteHole.EntityPushStrength;
			float num36 = Server.TickInterval * 4f;
			string[] pushableEntities2 = PushableEntities;
			foreach (string text2 in pushableEntities2)
			{
				IEnumerable<CBaseEntity> enumerable2 = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(text2);
				foreach (CBaseEntity item4 in enumerable2)
				{
					if (((item4 != null) ? item4.AbsOrigin : null) == null || !((CEntityInstance)item4).IsValid)
					{
						continue;
					}
					if (text2.StartsWith("weapon_"))
					{
						CBasePlayerWeapon val3 = ((NativeObject)item4).As<CBasePlayerWeapon>();
						if ((CEntityInstance)(object)((val3 == null) ? null : ((CBaseEntity)val3).OwnerEntity?.Value) != (CEntityInstance)null)
						{
							continue;
						}
					}
					float num38 = item4.AbsOrigin.X - whiteHolePos3.X;
					float num39 = item4.AbsOrigin.Y - whiteHolePos3.Y;
					float num40 = item4.AbsOrigin.Z - whiteHolePos3.Z;
					float num41 = num38 * num38 + num39 * num39 + num40 * num40;
					if (!(num41 > num35) && !(num41 < 1f))
					{
						float num42 = MathF.Sqrt(num41);
						float num43 = entityPushStrength * num36 * (1f - num42 / pushRadius);
						if (num43 > num42)
						{
							num43 = num42;
						}
						float num44 = num38 / num42;
						float num45 = num39 / num42;
						float num46 = num40 / num42;
						Vector val4 = new Vector((float?)(item4.AbsOrigin.X + num44 * num43), (float?)(item4.AbsOrigin.Y + num45 * num43), (float?)(item4.AbsOrigin.Z + num46 * num43 + 3f * num36));
						item4.Teleport(val4, item4.AbsRotation, item4.AbsVelocity);
					}
				}
			}
			CPlantedC4 val5 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault((CPlantedC4 c) => ((CEntityInstance)c).IsValid && ((CBaseEntity)c).AbsOrigin != null);
			if ((CEntityInstance)(object)val5 != (CEntityInstance)null)
			{
				float num47 = ((CBaseEntity)val5).AbsOrigin.X - whiteHolePos3.X;
				float num48 = ((CBaseEntity)val5).AbsOrigin.Y - whiteHolePos3.Y;
				float num49 = ((CBaseEntity)val5).AbsOrigin.Z - whiteHolePos3.Z;
				float num50 = num47 * num47 + num48 * num48 + num49 * num49;
				if (num50 <= num35 && num50 >= 1f)
				{
					float num51 = MathF.Sqrt(num50);
					float num52 = entityPushStrength * num36 * (1f - num51 / pushRadius);
					if (num52 > num51)
					{
						num52 = num51;
					}
					float num53 = num47 / num51;
					float num54 = num48 / num51;
					float num55 = num49 / num51;
					Vector val6 = new Vector((float?)(((CBaseEntity)val5).AbsOrigin.X + num53 * num52), (float?)(((CBaseEntity)val5).AbsOrigin.Y + num54 * num52), (float?)(((CBaseEntity)val5).AbsOrigin.Z + num55 * num52 + 3f * num36));
					((CBaseEntity)val5).Teleport(val6, ((CBaseEntity)val5).AbsRotation, ((CBaseEntity)val5).AbsVelocity);
				}
			}
			foreach (CCSPlayerController item5 in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
				select p)
			{
				CCSPlayerPawn value3 = item5.PlayerPawn.Value;
				Vector absOrigin = ((CBaseEntity)value3).AbsOrigin;
				float num56 = absOrigin.X - whiteHolePos3.X;
				float num57 = absOrigin.Y - whiteHolePos3.Y;
				float num58 = absOrigin.Z - whiteHolePos3.Z;
				float num59 = num56 * num56 + num57 * num57 + num58 * num58;
				if (!(num59 > num35) && !(num59 < 1f))
				{
					float num60 = MathF.Sqrt(num59);
					float num61 = num56 / num60;
					float num62 = num57 / num60;
					float num63 = num58 / num60;
					bool flag = ((CBaseEntity)item5).TeamNum != _ownerTeam;
					float num64 = (flag ? enemyPushStrength : teammatePullStrength);
					float num65 = num64 * num36 * (1f - num60 / pushRadius);
					if (flag)
					{
						Vector val7 = new Vector((float?)(num61 * num65 / num36), (float?)(num62 * num65 / num36), (float?)(num63 * num65 / num36));
						((CBaseEntity)value3).Teleport((Vector)null, (QAngle)null, val7);
					}
					else
					{
						Vector val8 = new Vector((float?)((0f - num61) * num65 / num36), (float?)((0f - num62) * num65 / num36), (float?)((0f - num63) * num65 / num36));
						((CBaseEntity)value3).Teleport((Vector)null, (QAngle)null, val8);
					}
				}
			}
		}
	}
}
