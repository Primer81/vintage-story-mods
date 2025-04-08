public class Packet_BulkEntityAttributes
{
	public Packet_EntityAttributes[] FullUpdates;

	public int FullUpdatesCount;

	public int FullUpdatesLength;

	public Packet_EntityAttributeUpdate[] PartialUpdates;

	public int PartialUpdatesCount;

	public int PartialUpdatesLength;

	public const int FullUpdatesFieldID = 1;

	public const int PartialUpdatesFieldID = 2;

	public Packet_EntityAttributes[] GetFullUpdates()
	{
		return FullUpdates;
	}

	public void SetFullUpdates(Packet_EntityAttributes[] value, int count, int length)
	{
		FullUpdates = value;
		FullUpdatesCount = count;
		FullUpdatesLength = length;
	}

	public void SetFullUpdates(Packet_EntityAttributes[] value)
	{
		FullUpdates = value;
		FullUpdatesCount = value.Length;
		FullUpdatesLength = value.Length;
	}

	public int GetFullUpdatesCount()
	{
		return FullUpdatesCount;
	}

	public void FullUpdatesAdd(Packet_EntityAttributes value)
	{
		if (FullUpdatesCount >= FullUpdatesLength)
		{
			if ((FullUpdatesLength *= 2) == 0)
			{
				FullUpdatesLength = 1;
			}
			Packet_EntityAttributes[] newArray = new Packet_EntityAttributes[FullUpdatesLength];
			for (int i = 0; i < FullUpdatesCount; i++)
			{
				newArray[i] = FullUpdates[i];
			}
			FullUpdates = newArray;
		}
		FullUpdates[FullUpdatesCount++] = value;
	}

	public Packet_EntityAttributeUpdate[] GetPartialUpdates()
	{
		return PartialUpdates;
	}

	public void SetPartialUpdates(Packet_EntityAttributeUpdate[] value, int count, int length)
	{
		PartialUpdates = value;
		PartialUpdatesCount = count;
		PartialUpdatesLength = length;
	}

	public void SetPartialUpdates(Packet_EntityAttributeUpdate[] value)
	{
		PartialUpdates = value;
		PartialUpdatesCount = value.Length;
		PartialUpdatesLength = value.Length;
	}

	public int GetPartialUpdatesCount()
	{
		return PartialUpdatesCount;
	}

	public void PartialUpdatesAdd(Packet_EntityAttributeUpdate value)
	{
		if (PartialUpdatesCount >= PartialUpdatesLength)
		{
			if ((PartialUpdatesLength *= 2) == 0)
			{
				PartialUpdatesLength = 1;
			}
			Packet_EntityAttributeUpdate[] newArray = new Packet_EntityAttributeUpdate[PartialUpdatesLength];
			for (int i = 0; i < PartialUpdatesCount; i++)
			{
				newArray[i] = PartialUpdates[i];
			}
			PartialUpdates = newArray;
		}
		PartialUpdates[PartialUpdatesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
