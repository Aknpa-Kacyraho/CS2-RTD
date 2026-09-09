using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils;

public abstract class SoundParamsBase<T> where T : SoundParamsBase<T>
{
	protected const uint M_CONST = 1540483477u;

	protected readonly List<(string name, byte type, byte[] data)> _parameters = new List<(string, byte, byte[])>();

	protected abstract uint FieldNameHashSeed { get; }

	protected T AddParameter(string name, SoundEventFieldType fieldType, byte[] data)
	{
		_parameters.Add((name, (byte)fieldType, data));
		return (T)this;
	}

	public T WithBool(string name, bool value)
	{
		return AddParameter(name, SoundEventFieldType.Bool, BitConverter.GetBytes(value));
	}

	public T WithInt32(string name, int value)
	{
		return AddParameter(name, SoundEventFieldType.Int32, BitConverter.GetBytes(value));
	}

	public T WithUInt32(string name, uint value)
	{
		return AddParameter(name, SoundEventFieldType.UInt32, BitConverter.GetBytes(value));
	}

	public T WithUInt64(string name, ulong value)
	{
		return AddParameter(name, SoundEventFieldType.UInt64, BitConverter.GetBytes(value));
	}

	public T WithFloat(string name, float value)
	{
		return AddParameter(name, SoundEventFieldType.Float, BitConverter.GetBytes(value));
	}

	public T WithFloat3(string name, Vector value)
	{
		byte[] array = new byte[12];
		Buffer.BlockCopy(BitConverter.GetBytes(value.X), 0, array, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(value.Y), 0, array, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(value.Z), 0, array, 8, 4);
		return AddParameter(name, SoundEventFieldType.Float3, array);
	}

	public T Volume(float volume)
	{
		return WithFloat("volume", volume);
	}

	protected byte[] BuildPackedParams()
	{
		List<byte> list = new List<byte>();
		foreach (var parameter in _parameters)
		{
			string item = parameter.name;
			byte item2 = parameter.type;
			byte[] item3 = parameter.data;
			uint value = SoundEventUtils.MurmurHash2(item, FieldNameHashSeed);
			list.AddRange(BitConverter.GetBytes(value));
			list.Add(item2);
			list.Add((byte)item3.Length);
			list.Add(0);
			list.AddRange(item3);
		}
		return list.ToArray();
	}
}
