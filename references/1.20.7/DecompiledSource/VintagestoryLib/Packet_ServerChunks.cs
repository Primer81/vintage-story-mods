public class Packet_ServerChunks
{
	public Packet_ServerChunk[] Chunks;

	public int ChunksCount;

	public int ChunksLength;

	public const int ChunksFieldID = 1;

	public Packet_ServerChunk[] GetChunks()
	{
		return Chunks;
	}

	public void SetChunks(Packet_ServerChunk[] value, int count, int length)
	{
		Chunks = value;
		ChunksCount = count;
		ChunksLength = length;
	}

	public void SetChunks(Packet_ServerChunk[] value)
	{
		Chunks = value;
		ChunksCount = value.Length;
		ChunksLength = value.Length;
	}

	public int GetChunksCount()
	{
		return ChunksCount;
	}

	public void ChunksAdd(Packet_ServerChunk value)
	{
		if (ChunksCount >= ChunksLength)
		{
			if ((ChunksLength *= 2) == 0)
			{
				ChunksLength = 1;
			}
			Packet_ServerChunk[] newArray = new Packet_ServerChunk[ChunksLength];
			for (int i = 0; i < ChunksCount; i++)
			{
				newArray[i] = Chunks[i];
			}
			Chunks = newArray;
		}
		Chunks[ChunksCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
