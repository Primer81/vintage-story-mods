public class Packet_ServerEntityDrawBlockSerializer
{
	private const int field = 8;

	public static Packet_ServerEntityDrawBlock DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerEntityDrawBlock instance = new Packet_ServerEntityDrawBlock();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerEntityDrawBlock DeserializeBuffer(byte[] buffer, int length, Packet_ServerEntityDrawBlock instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerEntityDrawBlock Deserialize(CitoMemoryStream stream, Packet_ServerEntityDrawBlock instance)
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
				instance.BlockType = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerEntityDrawBlock DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerEntityDrawBlock instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerEntityDrawBlock result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerEntityDrawBlock instance)
	{
		if (instance.BlockType != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerEntityDrawBlock instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerEntityDrawBlock instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
