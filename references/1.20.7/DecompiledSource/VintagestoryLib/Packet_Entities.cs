public class Packet_Entities
{
	public Packet_Entity[] Entities;

	public int EntitiesCount;

	public int EntitiesLength;

	public const int EntitiesFieldID = 1;

	public Packet_Entity[] GetEntities()
	{
		return Entities;
	}

	public void SetEntities(Packet_Entity[] value, int count, int length)
	{
		Entities = value;
		EntitiesCount = count;
		EntitiesLength = length;
	}

	public void SetEntities(Packet_Entity[] value)
	{
		Entities = value;
		EntitiesCount = value.Length;
		EntitiesLength = value.Length;
	}

	public int GetEntitiesCount()
	{
		return EntitiesCount;
	}

	public void EntitiesAdd(Packet_Entity value)
	{
		if (EntitiesCount >= EntitiesLength)
		{
			if ((EntitiesLength *= 2) == 0)
			{
				EntitiesLength = 1;
			}
			Packet_Entity[] newArray = new Packet_Entity[EntitiesLength];
			for (int i = 0; i < EntitiesCount; i++)
			{
				newArray[i] = Entities[i];
			}
			Entities = newArray;
		}
		Entities[EntitiesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
