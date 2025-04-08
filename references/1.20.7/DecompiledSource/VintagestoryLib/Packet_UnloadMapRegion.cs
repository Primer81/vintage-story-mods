public class Packet_UnloadMapRegion
{
	public int RegionX;

	public int RegionZ;

	public const int RegionXFieldID = 1;

	public const int RegionZFieldID = 2;

	public void SetRegionX(int value)
	{
		RegionX = value;
	}

	public void SetRegionZ(int value)
	{
		RegionZ = value;
	}

	internal void InitializeValues()
	{
	}
}
