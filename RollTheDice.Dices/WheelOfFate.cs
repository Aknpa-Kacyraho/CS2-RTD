using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class WheelOfFate : DiceBlueprint
{
	public readonly Random _random = new Random();

	// 本回合已复活次数（按 SteamID 记，跨死亡保留；回合结束 Reset 清空）。
	private readonly Dictionary<ulong, int> _reviveCount = new Dictionary<ulong, int>();

	public override string ClassName => "WheelOfFate";

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

	public WheelOfFate(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		RollTheDice.LogDebug(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName) + "\n");
	}

	public override void Add(CCSPlayerController player)
	{
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && !((CEntityInstance)(object)player.PlayerPawn?.Value == (CEntityInstance)null) && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			_players.Add(player);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			NotifyStatus(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
			if (DiceSynergy.HasPartner(player, "Fate"))
			{
				DiceSynergy.AnnounceCombo(player, "命运双生", "命运之轮复活率衰减减半（每次 -5%）！");
			}
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		if (reason != DiceRemoveReason.Death)
		{
			_players.Remove(player);
			_reviveCount.Remove(((CBasePlayerController)player).SteamID);
		}
	}

	public override void Reset()
	{
		_players.Clear();
		_reviveCount.Clear();
	}

	public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
	{
		CCSPlayerController victim = @event.Userid;
		if ((CEntityInstance)(object)victim == (CEntityInstance)null || !((CEntityInstance)victim).IsValid || !_players.Contains(victim))
		{
			return (HookResult)0;
		}
		CCSPlayerController obj = victim;
		CCSPlayerPawn victimPawn = ((obj == null) ? null : obj.PlayerPawn?.Value);
		if (victimPawn == null || !((CEntityInstance)victimPawn).IsValid || ((CBasePlayerPawn)victimPawn).WeaponServices == null)
		{
			return (HookResult)0;
		}
		var cfg = _config.Dices.WheelOfFate;
		ulong steamId = ((CBasePlayerController)victim).SteamID;
		int revivesUsed = (_reviveCount.TryGetValue(steamId, out int used) ? used : 0);
		float decay = cfg.ReviveDecay * (DiceSynergy.HasPartner(victim, "Fate") ? 0.5f : 1f);
		float reviveChance = MathF.Max(cfg.ReviveChanceMin, cfg.ReviveChance - decay * revivesUsed);
		RollTheDice.LogDebug($"[WheelOfFate] death: sid={steamId} used={revivesUsed} chance={reviveChance:F2}\n");
		if ((float)_random.NextDouble() >= reviveChance)
		{
			string value2 = _localizer["dice_WheelOfFate_failed"].Value;
			if (!string.IsNullOrEmpty(value2))
			{
				victim.PrintToCenterAlert(value2
					.Replace("{playerName}", ((CBasePlayerController)victim).PlayerName)
					.Replace("{chance}", MathF.Round(reviveChance * 100f).ToString("F0")));
			}
			return (HookResult)0;
		}
		_reviveCount[steamId] = revivesUsed + 1;
		// 50% 概率顺手拉一名阵亡队友（概率不随复活次数衰减）。
		CCSPlayerController teammate = null;
		if ((float)_random.NextDouble() < cfg.TeammateReviveChance)
		{
			teammate = PickDeadTeammate(victim);
		}
		CCSPlayerController capturedVictim = victim;
		CCSPlayerController capturedTeammate = teammate;
		Server.NextFrame((Action)delegate
		{
			Server.NextFrame((Action)delegate
			{
				// ★ 两帧后才执行：玩家可能已断开/换边 → 先验有效性再读 schema（否则原生崩）。
				TryRevive(capturedVictim, false);
				if (capturedTeammate != null)
				{
					TryRevive(capturedTeammate, true);
				}
			});
		});
		return (HookResult)0;
	}

	/// <summary>随机挑一名同队阵亡队友（没有则返回 null）。</summary>
	private CCSPlayerController PickDeadTeammate(CCSPlayerController victim)
	{
		try
		{
			List<CCSPlayerController> candidates = (from p in Utilities.GetPlayers()
				where (CEntityInstance)(object)p != (CEntityInstance)null && ((CEntityInstance)p).IsValid && !((CBasePlayerController)p).IsHLTV
					&& (CEntityInstance)(object)p != (CEntityInstance)(object)victim
					&& ((CBaseEntity)p).TeamNum == ((CBaseEntity)victim).TeamNum
					&& (CEntityInstance)(object)((CBasePlayerController)p).Pawn?.Value != (CEntityInstance)null
					&& ((CEntityInstance)((CBasePlayerController)p).Pawn.Value).IsValid
					&& ((CBaseEntity)((CBasePlayerController)p).Pawn.Value).LifeState != 0
				select p).ToList();
			if (candidates.Count == 0)
			{
				return null;
			}
			return candidates[_random.Next(candidates.Count)];
		}
		catch
		{
			return null;
		}
	}

	/// <summary>把阵亡玩家拉起来（等一帧再发装备，避免刚 Respawn 时 schema 还没就绪）。</summary>
	private void TryRevive(CCSPlayerController player, bool asTeammate)
	{
		if (player == null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		CCSPlayerController obj = player;
		CCSPlayerPawn pawn = ((obj == null) ? null : obj.PlayerPawn?.Value);
		if (pawn == null || !((CEntityInstance)pawn).IsValid || ((CBaseEntity)pawn).LifeState == 0)
		{
			return;
		}
		CCSPlayerController captured = player;
		player.Respawn();
		Server.NextFrame((Action)delegate
		{
			// ★ 再等一帧后仍需重新验有效性（断线/换边窗口）。
			ApplyReviveKit(captured, asTeammate);
		});
	}

	/// <summary>复活装备：全甲（甲+头盔）+ 专属换模 AK + 刀（CT 带拆弹器）。</summary>
	private void ApplyReviveKit(CCSPlayerController player, bool asTeammate)
	{
		if (player == null || !((CEntityInstance)player).IsValid)
		{
			return;
		}
		CCSPlayerController obj = player;
		CCSPlayerPawn pawn = ((obj == null) ? null : obj.PlayerPawn?.Value);
		if (pawn == null || !((CEntityInstance)pawn).IsValid || ((CBasePlayerPawn)pawn).ItemServices == null || ((CBaseEntity)pawn).LifeState != 0)
		{
			return;
		}
		player.RemoveWeapons();
		player.GiveNamedItem("weapon_knife");
		if ((int)player.Team == 3)
		{
			new CCSPlayer_ItemServices(((NativeObject)((CBasePlayerPawn)pawn).ItemServices).Handle).HasDefuser = true;
		}
		player.GiveNamedItem("item_assaultsuit");
		pawn.ArmorValue = 100;
		Utilities.SetStateChanged((CBaseEntity)(object)pawn, "CCSPlayerPawn", "m_ArmorValue", 0);
		// 普通 AK-47（不再使用自制模型换模）。
		player.GiveNamedItem("weapon_ak47");
		Vector absOrigin = ((CBaseEntity)pawn).AbsOrigin;
		if (absOrigin != null)
		{
			Effects.Play(new Vector(absOrigin.X, absOrigin.Y, absOrigin.Z + 40f), ParticlePaths.GoldHaloRaysRadiate, 1.2f);
		}
		Effects.SoundAll(_config.Dices.WheelOfFate.ReviveSound, _config.Dices.WheelOfFate.ReviveSoundVolume);
		if (asTeammate)
		{
			Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_WheelOfFate_teammate"].Value
				.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
			return;
		}
		int revivesUsed = (_reviveCount.TryGetValue(((CBasePlayerController)player).SteamID, out int used) ? used : 1);
		float decay = _config.Dices.WheelOfFate.ReviveDecay * (DiceSynergy.HasPartner(player, "Fate") ? 0.5f : 1f);
		float nextChance = MathF.Max(_config.Dices.WheelOfFate.ReviveChanceMin, _config.Dices.WheelOfFate.ReviveChance - decay * revivesUsed);
		player.PrintToCenterAlert($"☸ 命运之轮复活！下次成功率 {MathF.Round(nextChance * 100f):F0}%");
		if (Reincarnation.ComboWheelVictims.Remove(((CBasePlayerController)player).SteamID))
		{
			ulong comboSteamId = ((CBasePlayerController)player).SteamID;
			int comboMax = _config.Dices.Reincarnation.MaxStacks;
			int comboCur = (Reincarnation.PendingExtraDice.TryGetValue(comboSteamId, out var comboValue) ? comboValue : 0);
			if (comboCur < comboMax)
			{
				Reincarnation.PendingExtraDice[comboSteamId] = comboCur + 1;
				player.PrintToCenterAlert("\u267b 不死轮回！下回合额外骰子 +1！");
				Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + "\u267b 不死轮回联动！" + ((CBasePlayerController)player).PlayerName + " 复活成功，轮回层数 +1！");
			}
		}
		NotifyStatus(player, ClassName, new Dictionary<string, string> { 
		{
			"playerName",
			((CBasePlayerController)player).PlayerName
		} });
		Server.PrintToChatAll(" " + _localizer["command.prefix"].Value + _localizer["dice_WheelOfFate_broadcast"].Value.Replace("{playerName}", ((CBasePlayerController)player).PlayerName));
	}
}
