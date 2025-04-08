namespace Vintagestory.API.MathTools;

internal class SafeProxy
{
	private const uint Poly = 3988292384u;

	private static readonly uint[] _table;

	static SafeProxy()
	{
		_table = new uint[4096];
		for (uint i = 0u; i < 256; i++)
		{
			uint res = i;
			for (int t = 0; t < 16; t++)
			{
				for (int j = 0; j < 8; j++)
				{
					res = (((res & 1) == 1) ? (0xEDB88320u ^ (res >> 1)) : (res >> 1));
				}
				_table[t * 256 + i] = res;
			}
		}
	}

	public uint Append(uint crc, byte[] input, int offset, int length)
	{
		uint crcLocal = 0xFFFFFFFFu ^ crc;
		uint[] table = _table;
		while (length >= 16)
		{
			crcLocal = table[3840 + ((crcLocal ^ input[offset]) & 0xFF)] ^ table[3584 + (((crcLocal >> 8) ^ input[offset + 1]) & 0xFF)] ^ table[3328 + (((crcLocal >> 16) ^ input[offset + 2]) & 0xFF)] ^ table[3072 + (((crcLocal >> 24) ^ input[offset + 3]) & 0xFF)] ^ table[2816 + input[offset + 4]] ^ table[2560 + input[offset + 5]] ^ table[2304 + input[offset + 6]] ^ table[2048 + input[offset + 7]] ^ table[1792 + input[offset + 8]] ^ table[1536 + input[offset + 9]] ^ table[1280 + input[offset + 10]] ^ table[1024 + input[offset + 11]] ^ table[768 + input[offset + 12]] ^ table[512 + input[offset + 13]] ^ table[256 + input[offset + 14]] ^ table[input[offset + 15]];
			offset += 16;
			length -= 16;
		}
		while (--length >= 0)
		{
			crcLocal = table[(crcLocal ^ input[offset++]) & 0xFF] ^ (crcLocal >> 8);
		}
		return crcLocal ^ 0xFFFFFFFFu;
	}
}
