public class Packet_EntityAttributeUpdate
{
	public long EntityId;

	public Packet_PartialAttribute[] Attributes;

	public int AttributesCount;

	public int AttributesLength;

	public const int EntityIdFieldID = 1;

	public const int AttributesFieldID = 2;

	public void SetEntityId(long value)
	{
		EntityId = value;
	}

	public Packet_PartialAttribute[] GetAttributes()
	{
		return Attributes;
	}

	public void SetAttributes(Packet_PartialAttribute[] value, int count, int length)
	{
		Attributes = value;
		AttributesCount = count;
		AttributesLength = length;
	}

	public void SetAttributes(Packet_PartialAttribute[] value)
	{
		Attributes = value;
		AttributesCount = value.Length;
		AttributesLength = value.Length;
	}

	public int GetAttributesCount()
	{
		return AttributesCount;
	}

	public void AttributesAdd(Packet_PartialAttribute value)
	{
		if (AttributesCount >= AttributesLength)
		{
			if ((AttributesLength *= 2) == 0)
			{
				AttributesLength = 1;
			}
			Packet_PartialAttribute[] newArray = new Packet_PartialAttribute[AttributesLength];
			for (int i = 0; i < AttributesCount; i++)
			{
				newArray[i] = Attributes[i];
			}
			Attributes = newArray;
		}
		Attributes[AttributesCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
