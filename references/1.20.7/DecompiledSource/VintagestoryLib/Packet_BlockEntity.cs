public class Packet_BlockEntity
{
	public string Classname;

	public int PosX;

	public int PosY;

	public int PosZ;

	public byte[] Data;

	public const int ClassnameFieldID = 1;

	public const int PosXFieldID = 2;

	public const int PosYFieldID = 3;

	public const int PosZFieldID = 4;

	public const int DataFieldID = 5;

	public void SetClassname(string value)
	{
		Classname = value;
	}

	public void SetPosX(int value)
	{
		PosX = value;
	}

	public void SetPosY(int value)
	{
		PosY = value;
	}

	public void SetPosZ(int value)
	{
		PosZ = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
