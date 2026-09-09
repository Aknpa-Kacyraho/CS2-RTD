using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public class SoundParams : SoundParamsBase<SoundParams>
{
	protected override uint FieldNameHashSeed => 3050960640u;

	public uint Guid { get; set; }

	public RecipientFilter? Recipients { get; set; }

	public void Send()
	{
		UserMessage val = UserMessage.FromId(210);
		val.SetUInt("soundevent_guid", Guid, (int?)null);
		val.SetBytes("packed_params", BuildPackedParams(), (int?)null);
		if (Recipients != null)
		{
			val.Recipients = Recipients;
		}
		val.Send();
	}
}
