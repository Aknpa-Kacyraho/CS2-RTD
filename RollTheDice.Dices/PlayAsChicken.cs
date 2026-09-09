using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices;

public class PlayAsChicken : DiceBlueprint
{
	private bool _comboActive;

	private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());

	private readonly Dictionary<CCSPlayerController, Dictionary<string, object>> _chickens = new Dictionary<CCSPlayerController, Dictionary<string, object>>();

	private readonly Dictionary<CCSPlayerController, int> _originalMaxHealth = new Dictionary<CCSPlayerController, int>();

	private readonly string _playersAsChickenModel = "models/chicken/chicken.vmdl";

	private readonly Dictionary<string, uint> _chickenSounds = new Dictionary<string, uint>
	{
		{
			"Chicken.Idle",
			SoundEventUtils.GenerateSoundHash("Chicken.Idle")
		},
		{
			"Chicken.Panic",
			SoundEventUtils.GenerateSoundHash("Chicken.Panic")
		}
	};

	public override string ClassName => "PlayAsChicken";

	public override List<string> Listeners
	{
		get
		{
			int num = 3;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = "OnTick";
			num2++;
			span[num2] = "CheckTransmit";
			num2++;
			span[num2] = "OnPlayerButtonsChanged";
			return list;
		}
	}

	public override Dictionary<int, HookMode> UserMessages => new Dictionary<int, HookMode> { 
	{
		208,
		(HookMode)0
	} };

	public PlayAsChicken(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
		: base(GlobalConfig, Config, Localizer)
	{
		Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
	}

	public override void Add(CCSPlayerController player)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		if ((CEntityInstance)(object)player == (CEntityInstance)null || !((CEntityInstance)player).IsValid || (CEntityInstance)(object)((CBasePlayerController)player).Pawn?.Value == (CEntityInstance)null || !((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			return;
		}
		CDynamicProp val = Entities.CreatePropEntity(((CBaseEntity)((CBasePlayerController)player).Pawn.Value).AbsOrigin, new QAngle((float?)0f, (float?)((CBasePlayerController)player).Pawn.Value.V_angle.Y, (float?)0f), _playersAsChickenModel, 5f, (CEntityInstance?)(object)((CBasePlayerController)player).Pawn.Value);
		if (!((CEntityInstance)(object)val == (CEntityInstance)null) && ((CEntityInstance)val).IsValid)
		{
			_players.Add(player);
			_comboActive = DiceSynergy.HasPartner(player, "Mosquito");
			if (_comboActive)
			{
				DiceSynergy.AnnounceCombo(player, "迷你鸡神", "鸡神HP翻倍+速度×2");
			}
			_chickens.Add(player, new Dictionary<string, object>());
			_chickens[player]["next_sound"] = (int)Server.CurrentTime;
			_chickens[player]["prop"] = val;
			SetPlayerVisibility(player, 0);
			CCSPlayerPawn value = player.PlayerPawn.Value;
			_originalMaxHealth[player] = ((CBaseEntity)value).MaxHealth;
			((CBaseEntity)value).MaxHealth = _config.Dices.PlayAsChicken.ChickenHp;
			((CBaseEntity)value).Health = _config.Dices.PlayAsChicken.ChickenHp;
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value, "CBaseEntity", "m_iMaxHealth", 0);
			SpeedBonusManager.Register(player, "PlayAsChicken", _config.Dices.PlayAsChicken.SpeedBonus);
			NotifyPlayers(player, ClassName, new Dictionary<string, string> { 
			{
				"playerName",
				((CBasePlayerController)player).PlayerName
			} });
		}
	}

	public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
	{
		SetPlayerVisibility(player, 255);
		if (_chickens.TryGetValue(player, out Dictionary<string, object> value0) && value0.TryGetValue("prop", out object prop) && prop is CDynamicProp chickenProp)
		{
			Entities.RemoveEntity((CBaseEntity?)(object)chickenProp);
		}
		SpeedBonusManager.Unregister(player, "PlayAsChicken");
		if (_originalMaxHealth.TryGetValue(player, out var value) && (CEntityInstance)(object)((player == null) ? null : player.PlayerPawn?.Value) != (CEntityInstance)null && ((CEntityInstance)player.PlayerPawn.Value).IsValid)
		{
			CCSPlayerPawn value2 = player.PlayerPawn.Value;
			((CBaseEntity)value2).MaxHealth = value;
			if (((CBaseEntity)value2).Health > ((CBaseEntity)value2).MaxHealth)
			{
				((CBaseEntity)value2).Health = ((CBaseEntity)value2).MaxHealth;
			}
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iHealth", 0);
			Utilities.SetStateChanged((CBaseEntity)(object)value2, "CBaseEntity", "m_iMaxHealth", 0);
		}
		_originalMaxHealth.Remove(player);
		_players.Remove(player);
		_chickens.Remove(player);
	}

	public override void Reset()
	{
		foreach (CCSPlayerController item in _players.ToList())
		{
			Remove(item);
		}
		_players.Clear();
		_chickens.Clear();
		_originalMaxHealth.Clear();
	}

	public override void Destroy()
	{
		Reset();
	}

	public void OnTick()
	{
		if (_chickens.Count == 0)
		{
			return;
		}
		Dictionary<CCSPlayerController, Dictionary<string, object>> dictionary = new Dictionary<CCSPlayerController, Dictionary<string, object>>(_chickens);
		foreach (KeyValuePair<CCSPlayerController, Dictionary<string, object>> item in dictionary)
		{
			if (!((CEntityInstance)(object)item.Key == (CEntityInstance)null) && item.Key.PlayerPawn != null && item.Key.PlayerPawn.IsValid && !((CEntityInstance)(object)item.Key.PlayerPawn.Value == (CEntityInstance)null) && ((CBaseEntity)item.Key.PlayerPawn.Value).LifeState == 0 && item.Value.ContainsKey("prop"))
			{
				float speedBonus = _config.Dices.PlayAsChicken.SpeedBonus;
				float effective = SpeedBonusManager.GetEffective(item.Key, speedBonus);
				item.Key.PlayerPawn.Value.VelocityModifier = 1f + effective;
				Utilities.SetStateChanged((CBaseEntity)(object)item.Key.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier", 0);
				if ((int)dictionary[item.Key]["next_sound"] <= (int)Server.CurrentTime)
				{
					((CBaseEntity)item.Key).EmitSound(_chickenSounds.ElementAt(_random.Next(_chickenSounds.Count)).Key, (RecipientFilter)null, 1f, 0f);
					_chickens[item.Key]["next_sound"] = (int)Server.CurrentTime + _random.Next(_config.Dices.PlayAsChicken.MinSoundWaitTime, _config.Dices.PlayAsChicken.MaxSoundWaitTime);
				}
			}
		}
	}

	public void CheckTransmit(CCheckTransmitInfoList infoList)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (_chickens.Count == 0)
		{
			return;
		}
		foreach (var (val, val2) in infoList)
		{
			if (!((CEntityInstance)(object)val2 == (CEntityInstance)null) && ((CEntityInstance)val2).IsValid && !val2.IsBot && _chickens.ContainsKey(val2))
			{
				CDynamicProp val3 = (CDynamicProp)_chickens[val2]["prop"];
				if (!((CEntityInstance)(object)val3 == (CEntityInstance)null) && ((CEntityInstance)val3).IsValid)
				{
					CFixedBitVecBase transmitEntities = val.TransmitEntities;
					transmitEntities.Remove((CEntityInstance)(object)val3);
				}
			}
		}
	}

	public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I8
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Invalid comparison between Unknown and I8
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Expected O, but got Unknown
		if (!((CEntityInstance)(object)player == (CEntityInstance)null) && ((CEntityInstance)player).IsValid && _chickens.ContainsKey(player) && player.PlayerPawn != null && player.PlayerPawn.IsValid && !((CEntityInstance)(object)player.PlayerPawn.Value == (CEntityInstance)null))
		{
			if ((long)pressed == 4)
			{
				((CBaseEntity)(CDynamicProp)_chickens[player]["prop"]).Teleport(new Vector((float?)((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Y, (float?)(((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Z - 18f)), (QAngle)null, (Vector)null);
			}
			else if ((long)released == 4)
			{
				((CBaseEntity)(CDynamicProp)_chickens[player]["prop"]).Teleport(new Vector((float?)((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.X, (float?)((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Y, (float?)((CBaseEntity)player.PlayerPawn.Value).AbsOrigin.Z), (QAngle)null, (Vector)null);
			}
		}
	}

	public HookResult HookUserMessage208(UserMessage um)
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		uint value = um.ReadUInt("soundevent_hash", (int?)null);
		uint guid = um.ReadUInt("soundevent_guid", (int?)null);
		if (!_chickenSounds.ContainsValue(value))
		{
			return (HookResult)0;
		}
		SoundParams soundParams = new SoundParams();
		soundParams.Guid = guid;
		soundParams.Recipients = um.Recipients;
		soundParams.Volume(_config.Dices.PlayAsChicken.SoundVolume).Send();
		return (HookResult)0;
	}

	private void SetPlayerVisibility(CCSPlayerController? player, int alpha)
	{
		if (!((CEntityInstance)(object)((player == null) ? null : ((CBasePlayerController)player).Pawn?.Value) == (CEntityInstance)null) && ((CEntityInstance)((CBasePlayerController)player).Pawn.Value).IsValid)
		{
			((CBaseModelEntity)((CBasePlayerController)player).Pawn.Value).Render = Color.FromArgb(alpha, 255, 255, 255);
			Utilities.SetStateChanged((CBaseEntity)(object)((CBasePlayerController)player).Pawn.Value, "CBaseModelEntity", "m_clrRender", 0);
		}
	}
}
