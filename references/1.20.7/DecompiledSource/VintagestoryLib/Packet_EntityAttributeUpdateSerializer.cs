public class Packet_EntityAttributeUpdateSerializer
{
	private const int field = 8;

	public static Packet_EntityAttributeUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityAttributeUpdate instance = new Packet_EntityAttributeUpdate();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityAttributeUpdate DeserializeBuffer(byte[] buffer, int length, Packet_EntityAttributeUpdate instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityAttributeUpdate Deserialize(CitoMemoryStream stream, Packet_EntityAttributeUpdate instance)
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
			case 8:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 18:
				instance.AttributesAdd(Packet_PartialAttributeSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_EntityAttributeUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityAttributeUpdate instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityAttributeUpdate result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityAttributeUpdate instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Attributes != null)
		{
			for (int j = 0; j < instance.AttributesCount; j++)
			{
				Packet_PartialAttribute i2 = instance.Attributes[j];
				stream.WriteByte(18);
				CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
				Packet_PartialAttributeSerializer.Serialize(ms2, i2);
				int len = ms2.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms2.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityAttributeUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityAttributeUpdate instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
