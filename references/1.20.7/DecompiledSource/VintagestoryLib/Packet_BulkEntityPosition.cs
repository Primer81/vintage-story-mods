public class Packet_BulkEntityPosition
{
	public Packet_EntityPosition[] EntityPositions;

	public int EntityPositionsCount;

	public int EntityPositionsLength;

	public const int EntityPositionsFieldID = 1;

	public Packet_EntityPosition[] GetEntityPositions()
	{
		return EntityPositions;
	}

	public void SetEntityPositions(Packet_EntityPosition[] value, int count, int length)
	{
		EntityPositions = value;
		EntityPositionsCount = count;
		EntityPositionsLength = length;
	}

	public void SetEntityPositions(Packet_EntityPosition[] value)
	{
		EntityPositions = value;
		EntityPositionsCount = value.Length;
		EntityPositionsLength = value.Length;
	}

	public int GetEntityPositionsCount()
	{
		return EntityPositionsCount;
	}

	public void EntityPositionsAdd(Packet_EntityPosition value)
	{
		if (EntityPositionsCount >= EntityPositionsLength)
		{
			if ((EntityPositionsLength *= 2) == 0)
			{
				EntityPositionsLength = 1;
			}
			Packet_EntityPosition[] newArray = new Packet_EntityPosition[EntityPositionsLength];
			for (int i = 0; i < EntityPositionsCount; i++)
			{
				newArray[i] = EntityPositions[i];
			}
			EntityPositions = newArray;
		}
		EntityPositions[EntityPositionsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
