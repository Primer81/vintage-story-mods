public class Packet_ServerSetBlock
{
	public int X;

	public int Y;

	public int Z;

	public int BlockType;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int BlockTypeFieldID = 4;

	public void SetX(int value)
	{
		X = value;
	}

	public void SetY(int value)
	{
		Y = value;
	}

	public void SetZ(int value)
	{
		Z = value;
	}

	public void SetBlockType(int value)
	{
		BlockType = value;
	}

	internal void InitializeValues()
	{
	}
}
