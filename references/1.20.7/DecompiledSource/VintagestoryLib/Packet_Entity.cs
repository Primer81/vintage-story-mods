public class Packet_Entity
{
	public string EntityType;

	public long EntityId;

	public int SimulationRange;

	public byte[] Data;

	public const int EntityTypeFieldID = 1;

	public const int EntityIdFieldID = 2;

	public const int SimulationRangeFieldID = 3;

	public const int DataFieldID = 4;

	public void SetEntityType(string value)
	{
		EntityType = value;
	}

	public void SetEntityId(long value)
	{
		EntityId = value;
	}

	public void SetSimulationRange(int value)
	{
		SimulationRange = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
