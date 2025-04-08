public class Packet_GrindingProperties
{
	public byte[] GroundStack;

	public const int GroundStackFieldID = 1;

	public void SetGroundStack(byte[] value)
	{
		GroundStack = value;
	}

	internal void InitializeValues()
	{
	}
}
