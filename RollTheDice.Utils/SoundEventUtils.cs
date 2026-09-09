using System;
using System.Text;

namespace RollTheDice.Utils;

public static class SoundEventUtils
{
	public static uint GenerateSoundHash(string soundEventName)
	{
		return MurmurHash2(soundEventName, 1397900082u);
	}

	internal static uint MurmurHash2(string input, uint seed)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(input.ToLower());
		if (bytes == null)
		{
			return uint.MaxValue;
		}
		long num = bytes.Length;
		uint num2 = (uint)(num ^ seed);
		int num3 = 0;
		if (num >= 4)
		{
			uint num4 = (uint)(num >> 2);
			num -= 4 * num4;
			while (num4 != 0)
			{
				uint num5 = 1540483477 * BitConverter.ToUInt32(bytes, num3);
				num2 = (1540483477 * (num5 ^ (num5 >> 24))) ^ (1540483477 * num2);
				num3 += 4;
				num4--;
			}
		}
		int num6 = (int)num - 1;
		if (num6 != 0)
		{
			switch (num6 - 1)
			{
			default:
				num2 ^= num2 >> 13;
				return (1540483477 * num2) ^ (1540483477 * num2 >> 15);
			case 1:
				num2 ^= (uint)(bytes[num3 + 2] << 16);
				break;
			case 0:
				break;
			}
			num2 ^= (uint)(bytes[num3 + 1] << 8);
		}
		num2 = 1540483477 * (num2 ^ bytes[num3]);
		num2 ^= num2 >> 13;
		return (1540483477 * num2) ^ (1540483477 * num2 >> 15);
	}
}
