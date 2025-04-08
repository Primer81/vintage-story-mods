public class Packet_EntitySerializer
{
	private const int field = 8;

	public static Packet_Entity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Entity instance = new Packet_Entity();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Entity DeserializeBuffer(byte[] buffer, int length, Packet_Entity instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Entity Deserialize(CitoMemoryStream stream, Packet_Entity instance)
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
				instance.EntityType = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.SimulationRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.Data = ProtocolParser.ReadBytes(stream);
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

	public static Packet_Entity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Entity instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Entity result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Entity instance)
	{
		if (instance.EntityType != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.EntityType));
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.SimulationRange != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SimulationRange);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static byte[] SerializeToBytes(Packet_Entity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Entity instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
