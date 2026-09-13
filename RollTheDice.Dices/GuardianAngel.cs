using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class GuardianAngel : DiceBlueprint
{
	private readonly HashSet<ulong> _hasAngel = new HashSet<ulong>();

	private readonly HashSet<nint> _processingSave = new HashSet<nint>();

	public override string ClassName => "GuardianAngel";

	public override List<string> Listeners
	{
		get
		{
			int num = 1;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = "OnPlayerTakeDamagePre";
			return list;
		}
	}

	public GuardianAngel(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			if (DiceSynergy.HasPartner(player, "Thorns"))
			{
				DiceSynergy.AnnounceCombo(player, "圣光荆棘", "圣光荆棘联动生效！");
			}
			if (DiceSynergy.HasPartner(player, "Nirvana"))
			{
				DiceSynergy.AnnounceCombo(player, "菲尼克斯", "菲尼克斯联动生效！");
			}
			_hasAngel.Add(((CBasePlayerController)player).SteamID);
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
		_hasAngel.Remove(((CBasePlayerController)player).SteamID);
	}

	public override void Reset()
	{
		_players.Clear();
		_hasAngel.Clear();
		_processingSave.Clear();
	}

	public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
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
		CCSPlayerController player = (CCSPlayerController)obj2;
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || !_hasAngel.Contains(((CBasePlayerController)player).SteamID))
		{
			return (HookResult)0;
		}
		if (_processingSave.Contains(((NativeEntity)entity).Handle))
		{
			return (HookResult)0;
		}
		int num = entity.Health - (int)float.Round(info.Damage);
		if (num > 0)
		{
			return (HookResult)0;
		}
		_processingSave.Add(((NativeEntity)entity).Handle);
		info.Damage = 0f;
		_hasAngel.Remove(((CBasePlayerController)player).SteamID);
		entity.TakesDamage = false;
		Server.NextFrame((Action)delegate
		{
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			_processingSave.Remove(((NativeEntity)entity).Handle);
			if (!((CEntityInstance)(object)entity == (CEntityInstance)null) && ((CEntityInstance)entity).IsValid)
			{
				int num2 = Math.Min((DiceSynergy.HasPartner(player, "Thorns") || DiceSynergy.HasPartner(player, "Nirvana")) ? (_config.Dices.GuardianAngel.RestoreHealth * 2) : _config.Dices.GuardianAngel.RestoreHealth, entity.MaxHealth);
				entity.Health = num2;
				entity.MaxHealth = Math.Max(num2, entity.MaxHealth);
				Utilities.SetStateChanged(entity, "CBaseEntity", "m_iHealth", 0);
				Utilities.SetStateChanged(entity, "CBaseEntity", "m_iMaxHealth", 0);
				new Timer(_config.Dices.GuardianAngel.InvincibilitySeconds, (Action)delegate
				{
					if ((CEntityInstance)(object)entity != (CEntityInstance)null && ((CEntityInstance)entity).IsValid)
					{
						entity.TakesDamage = true;
					}
				}, (TimerFlags?)null);
				if ((CEntityInstance)(object)player != (CEntityInstance)null && ((CEntityInstance)player).IsValid)
				{
					string text = _localizer["dice_GuardianAngel_saved"].Value.Replace("{hp}", num2.ToString());
					player.PrintToCenter(text);
					player.PrintToChat(_localizer["command.prefix"].Value + text);
				}
			}
		});
		return (HookResult)1;
	}
}
