public class Packet_ServerPlayerPing
{
	public int[] ClientIds;

	public int ClientIdsCount;

	public int ClientIdsLength;

	public int[] Pings;

	public int PingsCount;

	public int PingsLength;

	public const int ClientIdsFieldID = 1;

	public const int PingsFieldID = 2;

	public int[] GetClientIds()
	{
		return ClientIds;
	}

	public void SetClientIds(int[] value, int count, int length)
	{
		ClientIds = value;
		ClientIdsCount = count;
		ClientIdsLength = length;
	}

	public void SetClientIds(int[] value)
	{
		ClientIds = value;
		ClientIdsCount = value.Length;
		ClientIdsLength = value.Length;
	}

	public int GetClientIdsCount()
	{
		return ClientIdsCount;
	}

	public void ClientIdsAdd(int value)
	{
		if (ClientIdsCount >= ClientIdsLength)
		{
			if ((ClientIdsLength *= 2) == 0)
			{
				ClientIdsLength = 1;
			}
			int[] newArray = new int[ClientIdsLength];
			for (int i = 0; i < ClientIdsCount; i++)
			{
				newArray[i] = ClientIds[i];
			}
			ClientIds = newArray;
		}
		ClientIds[ClientIdsCount++] = value;
	}

	public int[] GetPings()
	{
		return Pings;
	}

	public void SetPings(int[] value, int count, int length)
	{
		Pings = value;
		PingsCount = count;
		PingsLength = length;
	}

	public void SetPings(int[] value)
	{
		Pings = value;
		PingsCount = value.Length;
		PingsLength = value.Length;
	}

	public int GetPingsCount()
	{
		return PingsCount;
	}

	public void PingsAdd(int value)
	{
		if (PingsCount >= PingsLength)
		{
			if ((PingsLength *= 2) == 0)
			{
				PingsLength = 1;
			}
			int[] newArray = new int[PingsLength];
			for (int i = 0; i < PingsCount; i++)
			{
				newArray[i] = Pings[i];
			}
			Pings = newArray;
		}
		Pings[PingsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
