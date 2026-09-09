using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace RollTheDice.Configs;

public class NoExplosivesConfig
{
	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; } = true;

	[JsonPropertyName("swap_delay")]
	public float SwapDelay { get; set; } = 0.1f;

	[JsonPropertyName("model_scale")]
	public float ModelScale { get; set; } = 1f;

	public List<string> RandomModels { get; set; }

	public NoExplosivesConfig()
	{
		int num = 5;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = "models/food/fruits/banana01a.vmdl";
		num2++;
		span[num2] = "models/food/vegetables/onion01a.vmdl";
		num2++;
		span[num2] = "models/food/vegetables/pepper01a.vmdl";
		num2++;
		span[num2] = "models/food/vegetables/potato01a.vmdl";
		span[num2 + 1] = "models/food/vegetables/zucchini01a.vmdl";
		RandomModels = list;
	}
}
