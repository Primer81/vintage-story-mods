public class Packet_EntityPacketSerializer
{
	private const int field = 8;

	public static Packet_EntityPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityPacket instance = new Packet_EntityPacket();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityPacket DeserializeBuffer(byte[] buffer, int length, Packet_EntityPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityPacket Deserialize(CitoMemoryStream stream, Packet_EntityPacket instance)
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
			case 16:
				instance.Packetid = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
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

	public static Packet_EntityPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityPacket result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityPacket instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.Packetid != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Packetid);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityPacket instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
