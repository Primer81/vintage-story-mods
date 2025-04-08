public class Packet_WorldMetaData
{
	public int SunBrightness;

	public int[] BlockLightlevels;

	public int BlockLightlevelsCount;

	public int BlockLightlevelsLength;

	public int[] SunLightlevels;

	public int SunLightlevelsCount;

	public int SunLightlevelsLength;

	public byte[] WorldConfiguration;

	public int SeaLevel;

	public const int SunBrightnessFieldID = 1;

	public const int BlockLightlevelsFieldID = 2;

	public const int SunLightlevelsFieldID = 3;

	public const int WorldConfigurationFieldID = 4;

	public const int SeaLevelFieldID = 5;

	public void SetSunBrightness(int value)
	{
		SunBrightness = value;
	}

	public int[] GetBlockLightlevels()
	{
		return BlockLightlevels;
	}

	public void SetBlockLightlevels(int[] value, int count, int length)
	{
		BlockLightlevels = value;
		BlockLightlevelsCount = count;
		BlockLightlevelsLength = length;
	}

	public void SetBlockLightlevels(int[] value)
	{
		BlockLightlevels = value;
		BlockLightlevelsCount = value.Length;
		BlockLightlevelsLength = value.Length;
	}

	public int GetBlockLightlevelsCount()
	{
		return BlockLightlevelsCount;
	}

	public void BlockLightlevelsAdd(int value)
	{
		if (BlockLightlevelsCount >= BlockLightlevelsLength)
		{
			if ((BlockLightlevelsLength *= 2) == 0)
			{
				BlockLightlevelsLength = 1;
			}
			int[] newArray = new int[BlockLightlevelsLength];
			for (int i = 0; i < BlockLightlevelsCount; i++)
			{
				newArray[i] = BlockLightlevels[i];
			}
			BlockLightlevels = newArray;
		}
		BlockLightlevels[BlockLightlevelsCount++] = value;
	}

	public int[] GetSunLightlevels()
	{
		return SunLightlevels;
	}

	public void SetSunLightlevels(int[] value, int count, int length)
	{
		SunLightlevels = value;
		SunLightlevelsCount = count;
		SunLightlevelsLength = length;
	}

	public void SetSunLightlevels(int[] value)
	{
		SunLightlevels = value;
		SunLightlevelsCount = value.Length;
		SunLightlevelsLength = value.Length;
	}

	public int GetSunLightlevelsCount()
	{
		return SunLightlevelsCount;
	}

	public void SunLightlevelsAdd(int value)
	{
		if (SunLightlevelsCount >= SunLightlevelsLength)
		{
			if ((SunLightlevelsLength *= 2) == 0)
			{
				SunLightlevelsLength = 1;
			}
			int[] newArray = new int[SunLightlevelsLength];
			for (int i = 0; i < SunLightlevelsCount; i++)
			{
				newArray[i] = SunLightlevels[i];
			}
			SunLightlevels = newArray;
		}
		SunLightlevels[SunLightlevelsCount++] = value;
	}

	public void SetWorldConfiguration(byte[] value)
	{
		WorldConfiguration = value;
	}

	public void SetSeaLevel(int value)
	{
		SeaLevel = value;
	}

	internal void InitializeValues()
	{
	}
}
