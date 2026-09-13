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

public class Fireball : DiceBlueprint
{
	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private bool _comboActive;

	public override string ClassName => "Fireball";

	public override List<string> Events
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "EventMolotovDetonate";
			return list;
		}
	}

	public Fireball(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "FireLord");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "焚天灭地", "火球术+炎魔！爆炸范围50%↑，伤害50%↑");
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

	public HookResult EventMolotovDetonate(EventMolotovDetonate @event, GameEventInfo info)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		if (_players.Count == 0)
		{
			return (HookResult)0;
		}
		CCSPlayerController userid = @event.Userid;
		if ((CEntityInstance)(object)userid == (CEntityInstance)null || !((CEntityInstance)userid).IsValid || !_players.Contains(userid))
		{
			return (HookResult)0;
		}
		Vector origin = new Vector((float?)@event.X, (float?)@event.Y, (float?)@event.Z);
		float num = (DiceSynergy.HasPartner(userid, "FireLord") ? 1.5f : 1f);
		float num2 = 350f * _config.Dices.Fireball.RadiusMultiplier * num;
		int num3 = (int)float.Round((float)_random.Next(_config.Dices.Fireball.DamageMin, _config.Dices.Fireball.DamageMax + 1) * num);
		Server.NextFrame((Action)delegate
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Expected O, but got Unknown
			//IL_006a: Expected O, but got Unknown
			CBaseEntity val = Utilities.CreateEntityByName<CBaseEntity>("env_explosion");
			if ((CEntityInstance)(object)val != (CEntityInstance)null)
			{
				val.Teleport(origin, new QAngle((float?)0f, (float?)0f, (float?)0f), new Vector((float?)0f, (float?)0f, (float?)0f));
				val.DispatchSpawn();
				((CEntityInstance)val).AcceptInput("Explode", (CEntityInstance)null, (CEntityInstance)null, "", 0);
			}
		});
		foreach (CCSPlayerController item in from p in Utilities.GetPlayers()
			where ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV && (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null && ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState == 0 && ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).AbsOrigin != null
			select p)
		{
			float distance = Vectors.GetDistance(origin, ((CBaseEntity)((CBasePlayerController)item).Pawn.Value).AbsOrigin);
			if (!(distance <= num2))
			{
				continue;
			}
			float num4 = 1f - distance / num2;
			int num5 = Math.Max(5, (int)((float)num3 * num4));
			((CBaseEntity)((CBasePlayerController)item).Pawn.Value).Health -= num5;
			Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)item).Pawn.Value, "CBaseEntity", "m_iHealth", 0);
			item.PrintToCenterAlert($"\ud83d\udd25 火球术 -{num5}!");
			if (((CBaseEntity)((CBasePlayerController)item).Pawn.Value).Health > 0)
			{
				continue;
			}
			if (!item.IsBot && !((CBasePlayerController)item).IsHLTV)
			{
				((CBasePlayerController)item).Pawn.Value.CommitSuicide(false, true);
				continue;
			}
			try
			{
				((CBasePlayerController)item).Pawn.Value.CommitSuicide(false, true);
			}
			catch
			{
				((CBaseEntity)((CBasePlayerController)item).Pawn.Value).Health = 0;
				Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)item).Pawn.Value, "CBaseEntity", "m_iHealth", 0);
			}
		}
		return (HookResult)0;
	}
}
