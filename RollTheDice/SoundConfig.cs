using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice;

public class SoundConfig
{
	[JsonPropertyName("dice_sound")]
	public string DiceRollSound { get; set; } = "sounds/ui/coin_pickup_01.vsnd";

	[JsonPropertyName("play_on_command_only")]
	public bool PlayOnCommandOnly { get; set; } = false;

	[JsonPropertyName("volume")]
	public float Volume { get; set; } = 0.5f;

	/// <summary>
	/// 抽到传说 / combo dice 时全服广播的专属音效（dice 类名 → 音效路径）。
	/// 路径用运行期 <c>sounds/....vsnd</c>（素材随 Aknpa_packs 工坊 addon 分发）；缺省值按主题猜的，可直接改。
	/// </summary>
	[JsonPropertyName("legendary_sounds")]
	public Dictionary<string, string> LegendarySounds { get; set; } = new Dictionary<string, string>
	{
		// 传说
		{ "Awakener", "sounds/touhou/powerup.vsnd" },
		{ "Cthulhu", "sounds/zr/mother_scream.vsnd" },
		{ "Fate", "sounds/touhou/cardget.vsnd" },
		{ "FourHorsemen", "sounds/zombies/specialspawn.vsnd" },
		{ "God", "sounds/touhou/public/spell_call.vsnd" },
		{ "Ragnarok", "sounds/touhou/xrole/remilia/spear_throw.vsnd" },
		{ "WheelOfFate", "sounds/touhou/timeout.vsnd" },
		{ "SwordSaint", "sounds/touhou/xrole/youmu/slash_glow.vsnd" },
		{ "FinalJudgment", "sounds/touhou/bullet/explode4.vsnd" },
		{ "WolfKing", "sounds/touhou/public/wolf.vsnd" },
		{ "World", "sounds/touhou/xrole/sakuya/the_world.vsnd" },
		// combo
		{ "DeathKnightComplete", "sounds/touhou/bullet/frost.vsnd" },
		{ "FireDragon", "sounds/touhou/bullet/fire2.vsnd" },
		{ "IceDragon", "sounds/touhou/bullet/ice_explode_big.vsnd" },
		{ "Phoenix", "sounds/touhou/extend.vsnd" },
		{ "BeyondHeaven", "sounds/touhou/public/lit_power.vsnd" },
		{ "RadarStation", "sounds/touhou/xrole/koishi/brain_wave.vsnd" },
	};
}
