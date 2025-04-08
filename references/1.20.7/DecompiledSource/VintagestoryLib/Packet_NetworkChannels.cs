public class Packet_NetworkChannels
{
	public int[] ChannelIds;

	public int ChannelIdsCount;

	public int ChannelIdsLength;

	public string[] ChannelNames;

	public int ChannelNamesCount;

	public int ChannelNamesLength;

	public int[] ChannelUdpIds;

	public int ChannelUdpIdsCount;

	public int ChannelUdpIdsLength;

	public string[] ChannelUdpNames;

	public int ChannelUdpNamesCount;

	public int ChannelUdpNamesLength;

	public const int ChannelIdsFieldID = 1;

	public const int ChannelNamesFieldID = 2;

	public const int ChannelUdpIdsFieldID = 3;

	public const int ChannelUdpNamesFieldID = 4;

	public int[] GetChannelIds()
	{
		return ChannelIds;
	}

	public void SetChannelIds(int[] value, int count, int length)
	{
		ChannelIds = value;
		ChannelIdsCount = count;
		ChannelIdsLength = length;
	}

	public void SetChannelIds(int[] value)
	{
		ChannelIds = value;
		ChannelIdsCount = value.Length;
		ChannelIdsLength = value.Length;
	}

	public int GetChannelIdsCount()
	{
		return ChannelIdsCount;
	}

	public void ChannelIdsAdd(int value)
	{
		if (ChannelIdsCount >= ChannelIdsLength)
		{
			if ((ChannelIdsLength *= 2) == 0)
			{
				ChannelIdsLength = 1;
			}
			int[] newArray = new int[ChannelIdsLength];
			for (int i = 0; i < ChannelIdsCount; i++)
			{
				newArray[i] = ChannelIds[i];
			}
			ChannelIds = newArray;
		}
		ChannelIds[ChannelIdsCount++] = value;
	}

	public string[] GetChannelNames()
	{
		return ChannelNames;
	}

	public void SetChannelNames(string[] value, int count, int length)
	{
		ChannelNames = value;
		ChannelNamesCount = count;
		ChannelNamesLength = length;
	}

	public void SetChannelNames(string[] value)
	{
		ChannelNames = value;
		ChannelNamesCount = value.Length;
		ChannelNamesLength = value.Length;
	}

	public int GetChannelNamesCount()
	{
		return ChannelNamesCount;
	}

	public void ChannelNamesAdd(string value)
	{
		if (ChannelNamesCount >= ChannelNamesLength)
		{
			if ((ChannelNamesLength *= 2) == 0)
			{
				ChannelNamesLength = 1;
			}
			string[] newArray = new string[ChannelNamesLength];
			for (int i = 0; i < ChannelNamesCount; i++)
			{
				newArray[i] = ChannelNames[i];
			}
			ChannelNames = newArray;
		}
		ChannelNames[ChannelNamesCount++] = value;
	}

	public int[] GetChannelUdpIds()
	{
		return ChannelUdpIds;
	}

	public void SetChannelUdpIds(int[] value, int count, int length)
	{
		ChannelUdpIds = value;
		ChannelUdpIdsCount = count;
		ChannelUdpIdsLength = length;
	}

	public void SetChannelUdpIds(int[] value)
	{
		ChannelUdpIds = value;
		ChannelUdpIdsCount = value.Length;
		ChannelUdpIdsLength = value.Length;
	}

	public int GetChannelUdpIdsCount()
	{
		return ChannelUdpIdsCount;
	}

	public void ChannelUdpIdsAdd(int value)
	{
		if (ChannelUdpIdsCount >= ChannelUdpIdsLength)
		{
			if ((ChannelUdpIdsLength *= 2) == 0)
			{
				ChannelUdpIdsLength = 1;
			}
			int[] newArray = new int[ChannelUdpIdsLength];
			for (int i = 0; i < ChannelUdpIdsCount; i++)
			{
				newArray[i] = ChannelUdpIds[i];
			}
			ChannelUdpIds = newArray;
		}
		ChannelUdpIds[ChannelUdpIdsCount++] = value;
	}

	public string[] GetChannelUdpNames()
	{
		return ChannelUdpNames;
	}

	public void SetChannelUdpNames(string[] value, int count, int length)
	{
		ChannelUdpNames = value;
		ChannelUdpNamesCount = count;
		ChannelUdpNamesLength = length;
	}

	public void SetChannelUdpNames(string[] value)
	{
		ChannelUdpNames = value;
		ChannelUdpNamesCount = value.Length;
		ChannelUdpNamesLength = value.Length;
	}

	public int GetChannelUdpNamesCount()
	{
		return ChannelUdpNamesCount;
	}

	public void ChannelUdpNamesAdd(string value)
	{
		if (ChannelUdpNamesCount >= ChannelUdpNamesLength)
		{
			if ((ChannelUdpNamesLength *= 2) == 0)
			{
				ChannelUdpNamesLength = 1;
			}
			string[] newArray = new string[ChannelUdpNamesLength];
			for (int i = 0; i < ChannelUdpNamesCount; i++)
			{
				newArray[i] = ChannelUdpNames[i];
			}
			ChannelUdpNames = newArray;
		}
		ChannelUdpNames[ChannelUdpNamesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
