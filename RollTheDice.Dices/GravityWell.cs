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

public class GravityWell : DiceBlueprint
{
	private bool _comboActive;

	internal static readonly List<(Vector Pos, float ExpireTime, string PlayerName, bool Combo)> ActiveWells = new List<(Vector, float, string, bool)>();

	private int _tickCounter;

	public override string ClassName => "GravityWell";

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

	public GravityWell(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
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
				DiceSynergy.AnnounceCombo(player, "引力深渊", "引力持续翻倍");
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
		ActiveWells.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !_players.Contains(userid))
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
		float num = Server.CurrentTime;
		float duration = _config.Dices.GravityWell.Duration;
		Vector absOrigin = ((CBaseEntity)userid.PlayerPawn.Value).AbsOrigin;
		bool combo = DiceSynergy.HasPartner(userid, "BlackHole");
		ActiveWells.Add((absOrigin, num + duration, ((CBasePlayerController)userid).PlayerName, combo));
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_GravityWell_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)userid).PlayerName));
		return (HookResult)0;
	}

	public void OnTick()
	{
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Expected O, but got Unknown
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ef: Expected O, but got Unknown
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		if ((_players.Count == 0 && ActiveWells.Count == 0) || ActiveWells.Count == 0)
		{
			return;
		}
		_tickCounter++;
		if (_tickCounter % 13 != 0)
		{
			return;
		}
		float num = Server.CurrentTime;
		float pullRadius = _config.Dices.GravityWell.PullRadius;
		for (int num3 = ActiveWells.Count - 1; num3 >= 0; num3--)
		{
			if (num >= ActiveWells[num3].ExpireTime)
			{
				ActiveWells.RemoveAt(num3);
			}
		}
		if (ActiveWells.Count == 0)
		{
			return;
		}
		CBaseEntity[] first = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("prop_physics_multiplayer").Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hegrenade_projectile")).Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("flashbang_projectile"))
			.Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("smokegrenade_projectile"))
			.Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("molotov_projectile"))
			.Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("incendiarygrenade_projectile"))
			.Concat(Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("decoy_projectile"))
			.ToArray();
		CBaseEntity[] second = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_ak47").Take(50).ToArray();
		foreach (var activeWell in ActiveWells)
		{
			Vector item = activeWell.Pos;
			float num2 = (activeWell.Combo ? (_config.Dices.GravityWell.PullStrength * 2f) : _config.Dices.GravityWell.PullStrength);
			float num4 = pullRadius * pullRadius;
			IEnumerable<CBaseEntity> enumerable = first.Concat(second);
			foreach (CBaseEntity item2 in enumerable)
			{
				if (((item2 != null) ? item2.AbsOrigin : null) == null || !((CEntityInstance)item2).IsValid)
				{
					continue;
				}
				float num5 = item.X - item2.AbsOrigin.X;
				float num6 = item.Y - item2.AbsOrigin.Y;
				float num7 = item.Z - item2.AbsOrigin.Z;
				float num8 = num5 * num5 + num6 * num6 + num7 * num7;
				if (!(num8 > num4) && !(num8 < 1f))
				{
					float num9 = MathF.Sqrt(num8);
					float num10 = num2 * 0.2f * (pullRadius / (num9 + 50f));
					if (num10 > num9)
					{
						num10 = num9;
					}
					float num11 = num5 / num9;
					float num12 = num6 / num9;
					float num13 = num7 / num9;
					Vector val = new Vector((float?)(item2.AbsOrigin.X + num11 * num10), (float?)(item2.AbsOrigin.Y + num12 * num10), (float?)(item2.AbsOrigin.Z + num13 * num10 + 1f));
					QAngle val2 = (QAngle)(((object)item2.AbsRotation) ?? ((object)new QAngle((float?)0f, (float?)0f, (float?)0f)));
					Vector val3 = (Vector)(((object)item2.AbsVelocity) ?? ((object)new Vector((float?)0f, (float?)0f, (float?)0f)));
					item2.Teleport(val, val2, val3);
				}
			}
			foreach (CCSPlayerController item3 in from p in Utilities.GetPlayers()
				where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)p.PlayerPawn?.Value != (CEntityInstance)null && ((CEntityInstance)p.PlayerPawn.Value).IsValid && ((CBaseEntity)p.PlayerPawn.Value).LifeState == 0 && ((CBaseEntity)p.PlayerPawn.Value).AbsOrigin != null
				select p)
			{
				CCSPlayerPawn value = item3.PlayerPawn.Value;
				Vector absOrigin = ((CBaseEntity)value).AbsOrigin;
				float num14 = item.X - absOrigin.X;
				float num15 = item.Y - absOrigin.Y;
				float num16 = item.Z - absOrigin.Z;
				float num17 = num14 * num14 + num15 * num15 + num16 * num16;
				if (!(num17 > num4) && !(num17 < 1f))
				{
					float num18 = MathF.Sqrt(num17);
					float num19 = num2 * 0.06f * (pullRadius / (num18 + 50f));
					if (num19 > num18)
					{
						num19 = num18;
					}
					float num20 = num14 / num18;
					float num21 = num15 / num18;
					float num22 = num16 / num18;
					Vector val4 = new Vector((float?)(num20 * num19 / 0.2f), (float?)(num21 * num19 / 0.2f), (float?)(num22 * num19 / 0.2f));
					((CBaseEntity)value).Teleport((Vector)null, (QAngle)null, val4);
				}
			}
		}
	}
}
