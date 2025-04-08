public class Packet_BlockDrop
{
	public int QuantityAvg;

	public int QuantityVar;

	public int QuantityDist;

	public byte[] DroppedStack;

	public int Tool;

	public const int QuantityAvgFieldID = 1;

	public const int QuantityVarFieldID = 2;

	public const int QuantityDistFieldID = 3;

	public const int DroppedStackFieldID = 4;

	public const int ToolFieldID = 5;

	public void SetQuantityAvg(int value)
	{
		QuantityAvg = value;
	}

	public void SetQuantityVar(int value)
	{
		QuantityVar = value;
	}

	public void SetQuantityDist(int value)
	{
		QuantityDist = value;
	}

	public void SetDroppedStack(byte[] value)
	{
		DroppedStack = value;
	}

	public void SetTool(int value)
	{
		Tool = value;
	}

	internal void InitializeValues()
	{
	}
}
