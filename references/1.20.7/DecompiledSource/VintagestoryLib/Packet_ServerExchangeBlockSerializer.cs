public class Packet_ServerExchangeBlockSerializer
{
	private const int field = 8;

	public static Packet_ServerExchangeBlock DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerExchangeBlock instance = new Packet_ServerExchangeBlock();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerExchangeBlock DeserializeBuffer(byte[] buffer, int length, Packet_ServerExchangeBlock instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerExchangeBlock Deserialize(CitoMemoryStream stream, Packet_ServerExchangeBlock instance)
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
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
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

	public static Packet_ServerExchangeBlock DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerExchangeBlock instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerExchangeBlock result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerExchangeBlock instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.BlockType != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerExchangeBlock instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerExchangeBlock instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
