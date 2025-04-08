using System;
using csogg;

namespace Vintagestory.Client.NoObf;

public class OggPage : Page
{
	[ThreadStatic]
	private static uint[] crc_lookup;

	private static uint crc_entry(uint index)
	{
		uint r = index << 24;
		for (int i = 0; i < 8; i++)
		{
			r = (((r & 0x80000000u) == 0) ? (r << 1) : ((r << 1) ^ 0x4C11DB7u));
		}
		return r & 0xFFFFFFFFu;
	}

	internal int version()
	{
		return header_base[header + 4] & 0xFF;
	}

	internal int continued()
	{
		return header_base[header + 5] & 1;
	}

	public new int bos()
	{
		return header_base[header + 5] & 2;
	}

	public new int eos()
	{
		return header_base[header + 5] & 4;
	}

	public new long granulepos()
	{
		return ((((((((((((((long)(header_base[header + 13] & 0xFF) << 8) | (uint)(header_base[header + 12] & 0xFF)) << 8) | (uint)(header_base[header + 11] & 0xFF)) << 8) | (uint)(header_base[header + 10] & 0xFF)) << 8) | (uint)(header_base[header + 9] & 0xFF)) << 8) | (uint)(header_base[header + 8] & 0xFF)) << 8) | (uint)(header_base[header + 7] & 0xFF)) << 8) | (uint)(header_base[header + 6] & 0xFF);
	}

	public new int serialno()
	{
		return (header_base[header + 14] & 0xFF) | ((header_base[header + 15] & 0xFF) << 8) | ((header_base[header + 16] & 0xFF) << 16) | ((header_base[header + 17] & 0xFF) << 24);
	}

	internal int pageno()
	{
		return (header_base[header + 18] & 0xFF) | ((header_base[header + 19] & 0xFF) << 8) | ((header_base[header + 20] & 0xFF) << 16) | ((header_base[header + 21] & 0xFF) << 24);
	}

	internal void checksum()
	{
		uint crc_reg = 0u;
		for (int j = 0; j < header_len; j++)
		{
			uint a = header_base[header + j] & 0xFFu;
			uint b = (crc_reg >> 24) & 0xFFu;
			crc_reg = (crc_reg << 8) ^ crc_lookup[a ^ b];
		}
		for (int i = 0; i < body_len; i++)
		{
			uint a = body_base[body + i] & 0xFFu;
			uint b = (crc_reg >> 24) & 0xFFu;
			crc_reg = (crc_reg << 8) ^ crc_lookup[a ^ b];
		}
		header_base[header + 22] = (byte)crc_reg;
		header_base[header + 23] = (byte)(crc_reg >> 8);
		header_base[header + 24] = (byte)(crc_reg >> 16);
		header_base[header + 25] = (byte)(crc_reg >> 24);
	}

	public OggPage()
	{
		if (crc_lookup == null)
		{
			crc_lookup = new uint[256];
			for (uint i = 0u; i < crc_lookup.Length; i++)
			{
				crc_lookup[i] = crc_entry(i);
			}
		}
	}
}
