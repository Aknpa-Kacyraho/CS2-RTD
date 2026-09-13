using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class NoExplosivesConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("radius")]
	public float Radius { get; set; } = 600f;

	[JsonPropertyName("model_scale")]
	public float ModelScale { get; set; } = 1f;

	[JsonPropertyName("random_models")]
	public List<string> RandomModels { get; set; } = new List<string>
	{
		"models/food/fruits/banana01a.vmdl",
		"models/food/vegetables/onion01a.vmdl",
		"models/food/vegetables/pepper01a.vmdl",
		"models/food/vegetables/potato01a.vmdl",
		"models/food/vegetables/zucchini01a.vmdl"
	};
}