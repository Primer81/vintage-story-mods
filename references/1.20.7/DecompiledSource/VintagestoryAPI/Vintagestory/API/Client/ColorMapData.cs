namespace Vintagestory.API.Client;

public struct ColorMapData
{
	public int Value;

	public byte SeasonMapIndex => (byte)Value;

	public byte ClimateMapIndex => (byte)((uint)(Value >> 8) & 0xFu);

	public byte Temperature => (byte)(Value >> 16);

	public byte Rainfall => (byte)(Value >> 24);

	public byte FrostableBit => (byte)((uint)(Value >> 12) & 1u);

	public ColorMapData(int value)
	{
		Value = value;
	}

	public ColorMapData(byte seasonMapIndex, byte climateMapIndex, byte temperature, byte rainFall, bool frostable)
	{
		Value = seasonMapIndex | ((climateMapIndex & 0xF) << 8) | (temperature << 16) | (rainFall << 24) | (frostable ? 4096 : 0);
	}

	public ColorMapData(int seasonMapIndex, int climateMapIndex, int temperature, int rainFall, bool frostable)
	{
		Value = seasonMapIndex | ((climateMapIndex & 0xF) << 8) | (temperature << 16) | (rainFall << 24) | (frostable ? 4096 : 0);
	}

	public static int FromValues(byte seasonMapIndex, byte climateMapIndex, byte temperature, byte rainFall, bool frostable)
	{
		return seasonMapIndex | ((climateMapIndex & 0xF) << 8) | (temperature << 16) | (rainFall << 24) | (frostable ? 4096 : 0);
	}

	public static int FromValues(byte seasonMapIndex, byte climateMapIndex, byte temperature, byte rainFall, bool frostable, int extraColorBits)
	{
		return seasonMapIndex | ((climateMapIndex & 0xF) << 8) | (temperature << 16) | (rainFall << 24) | (frostable ? 4096 : 0) | ((extraColorBits & 7) << 13);
	}
}
