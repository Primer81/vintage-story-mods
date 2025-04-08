public class Packet_ServerSetBlocks
{
	public byte[] SetBlocks;

	public const int SetBlocksFieldID = 1;

	public void SetSetBlocks(byte[] value)
	{
		SetBlocks = value;
	}

	internal void InitializeValues()
	{
	}
}
