public class Packet_RemoveBlockLight
{
	public int PosX;

	public int PosY;

	public int PosZ;

	public int LightH;

	public int LightS;

	public int LightV;

	public const int PosXFieldID = 1;

	public const int PosYFieldID = 2;

	public const int PosZFieldID = 3;

	public const int LightHFieldID = 4;

	public const int LightSFieldID = 5;

	public const int LightVFieldID = 6;

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

	public void SetLightH(int value)
	{
		LightH = value;
	}

	public void SetLightS(int value)
	{
		LightS = value;
	}

	public void SetLightV(int value)
	{
		LightV = value;
	}

	internal void InitializeValues()
	{
	}
}
