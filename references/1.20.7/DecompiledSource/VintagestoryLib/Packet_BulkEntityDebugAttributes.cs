public class Packet_BulkEntityDebugAttributes
{
	public Packet_EntityAttributes[] FullUpdates;

	public int FullUpdatesCount;

	public int FullUpdatesLength;

	public const int FullUpdatesFieldID = 1;

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

	internal void InitializeValues()
	{
	}
}
