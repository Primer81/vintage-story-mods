public class Packet_EntityAttributes
{
	public long EntityId;

	public byte[] Data;

	public const int EntityIdFieldID = 1;

	public const int DataFieldID = 2;

	public void SetEntityId(long value)
	{
		EntityId = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
