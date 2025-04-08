public class Packet_EntityBoundingBox
{
	public int SizeX;

	public int SizeY;

	public int SizeZ;

	public const int SizeXFieldID = 1;

	public const int SizeYFieldID = 2;

	public const int SizeZFieldID = 3;

	public void SetSizeX(int value)
	{
		SizeX = value;
	}

	public void SetSizeY(int value)
	{
		SizeY = value;
	}

	public void SetSizeZ(int value)
	{
		SizeZ = value;
	}

	internal void InitializeValues()
	{
	}
}
