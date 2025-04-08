public class Packet_BlockEntities
{
	public Packet_BlockEntity[] BlockEntitites;

	public int BlockEntititesCount;

	public int BlockEntititesLength;

	public const int BlockEntititesFieldID = 1;

	public Packet_BlockEntity[] GetBlockEntitites()
	{
		return BlockEntitites;
	}

	public void SetBlockEntitites(Packet_BlockEntity[] value, int count, int length)
	{
		BlockEntitites = value;
		BlockEntititesCount = count;
		BlockEntititesLength = length;
	}

	public void SetBlockEntitites(Packet_BlockEntity[] value)
	{
		BlockEntitites = value;
		BlockEntititesCount = value.Length;
		BlockEntititesLength = value.Length;
	}

	public int GetBlockEntititesCount()
	{
		return BlockEntititesCount;
	}

	public void BlockEntititesAdd(Packet_BlockEntity value)
	{
		if (BlockEntititesCount >= BlockEntititesLength)
		{
			if ((BlockEntititesLength *= 2) == 0)
			{
				BlockEntititesLength = 1;
			}
			Packet_BlockEntity[] newArray = new Packet_BlockEntity[BlockEntititesLength];
			for (int i = 0; i < BlockEntititesCount; i++)
			{
				newArray[i] = BlockEntitites[i];
			}
			BlockEntitites = newArray;
		}
		BlockEntitites[BlockEntititesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
