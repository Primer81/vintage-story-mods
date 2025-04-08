public class Packet_ServerLevelInitialize
{
	public int ServerChunkSize;

	public int ServerMapChunkSize;

	public int ServerMapRegionSize;

	public int MaxViewDistance;

	public const int ServerChunkSizeFieldID = 1;

	public const int ServerMapChunkSizeFieldID = 2;

	public const int ServerMapRegionSizeFieldID = 3;

	public const int MaxViewDistanceFieldID = 4;

	public void SetServerChunkSize(int value)
	{
		ServerChunkSize = value;
	}

	public void SetServerMapChunkSize(int value)
	{
		ServerMapChunkSize = value;
	}

	public void SetServerMapRegionSize(int value)
	{
		ServerMapRegionSize = value;
	}

	public void SetMaxViewDistance(int value)
	{
		MaxViewDistance = value;
	}

	internal void InitializeValues()
	{
	}
}
