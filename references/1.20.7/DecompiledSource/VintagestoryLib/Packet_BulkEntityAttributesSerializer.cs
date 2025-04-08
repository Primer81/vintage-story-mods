public class Packet_BulkEntityAttributesSerializer
{
	private const int field = 8;

	public static Packet_BulkEntityAttributes DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BulkEntityAttributes instance = new Packet_BulkEntityAttributes();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BulkEntityAttributes DeserializeBuffer(byte[] buffer, int length, Packet_BulkEntityAttributes instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BulkEntityAttributes Deserialize(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
	{
		instance.InitializeValues();
		int keyInt;
		while (true)
		{
			keyInt = stream.ReadByte();
			if (((uint)keyInt & 0x80u) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if (((uint)keyInt & 0x4000u) != 0)
				{
					break;
				}
			}
			switch (keyInt)
			{
			case 0:
				return null;
			case 10:
				instance.FullUpdatesAdd(Packet_EntityAttributesSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.PartialUpdatesAdd(Packet_EntityAttributeUpdateSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			default:
				ProtocolParser.SkipKey(stream, Key.Create(keyInt));
				break;
			}
		}
		if (keyInt >= 0)
		{
			return null;
		}
		return instance;
	}

	public static Packet_BulkEntityAttributes DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BulkEntityAttributes instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BulkEntityAttributes result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.FullUpdates != null)
		{
			for (int k = 0; k < instance.FullUpdatesCount; k++)
			{
				Packet_EntityAttributes i1 = instance.FullUpdates[k];
				stream.WriteByte(10);
				CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
				Packet_EntityAttributesSerializer.Serialize(ms1, i1);
				int len2 = ms1.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms1.GetBuffer(), 0, len2);
			}
		}
		if (instance.PartialUpdates != null)
		{
			for (int j = 0; j < instance.PartialUpdatesCount; j++)
			{
				Packet_EntityAttributeUpdate i2 = instance.PartialUpdates[j];
				stream.WriteByte(18);
				CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
				Packet_EntityAttributeUpdateSerializer.Serialize(ms2, i2);
				int len = ms2.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms2.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_BulkEntityAttributes instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BulkEntityAttributes instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
