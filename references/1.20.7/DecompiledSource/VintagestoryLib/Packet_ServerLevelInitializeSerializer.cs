public class Packet_ServerLevelInitializeSerializer
{
	private const int field = 8;

	public static Packet_ServerLevelInitialize DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelInitialize instance = new Packet_ServerLevelInitialize();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerLevelInitialize DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelInitialize instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelInitialize Deserialize(CitoMemoryStream stream, Packet_ServerLevelInitialize instance)
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
				instance.ServerChunkSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ServerMapChunkSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.ServerMapRegionSize = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MaxViewDistance = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerLevelInitialize DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelInitialize instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerLevelInitialize result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelInitialize instance)
	{
		if (instance.ServerChunkSize != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ServerChunkSize);
		}
		if (instance.ServerMapChunkSize != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ServerMapChunkSize);
		}
		if (instance.ServerMapRegionSize != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ServerMapRegionSize);
		}
		if (instance.MaxViewDistance != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MaxViewDistance);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerLevelInitialize instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelInitialize instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
