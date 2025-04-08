public class Packet_EntitySpawn
{
	public Packet_Entity[] Entity;

	public int EntityCount;

	public int EntityLength;

	public const int EntityFieldID = 1;

	public Packet_Entity[] GetEntity()
	{
		return Entity;
	}

	public void SetEntity(Packet_Entity[] value, int count, int length)
	{
		Entity = value;
		EntityCount = count;
		EntityLength = length;
	}

	public void SetEntity(Packet_Entity[] value)
	{
		Entity = value;
		EntityCount = value.Length;
		EntityLength = value.Length;
	}

	public int GetEntityCount()
	{
		return EntityCount;
	}

	public void EntityAdd(Packet_Entity value)
	{
		if (EntityCount >= EntityLength)
		{
			if ((EntityLength *= 2) == 0)
			{
				EntityLength = 1;
			}
			Packet_Entity[] newArray = new Packet_Entity[EntityLength];
			for (int i = 0; i < EntityCount; i++)
			{
				newArray[i] = Entity[i];
			}
			Entity = newArray;
		}
		Entity[EntityCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
