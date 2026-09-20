using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class NukeLeakConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("detonation_seconds")]
	public float DetonationSeconds { get; set; } = 100f;

	/// <summary>引爆范围（以持有者位置为中心），半径外的玩家不受伤害。</summary>
	[JsonPropertyName("damage_radius")]
	public float DamageRadius { get; set; } = 1000f;

	/// <summary>范围内伤害（不再全服必死；范围内生命归零才死）。</summary>
	[JsonPropertyName("damage")]
	public int Damage { get; set; } = 200;
}
