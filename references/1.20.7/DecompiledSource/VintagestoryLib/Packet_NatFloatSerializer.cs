public class Packet_NatFloatSerializer
{
	private const int field = 8;

	public static Packet_NatFloat DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NatFloat instance = new Packet_NatFloat();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NatFloat DeserializeBuffer(byte[] buffer, int length, Packet_NatFloat instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NatFloat Deserialize(CitoMemoryStream stream, Packet_NatFloat instance)
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
				instance.Avg = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Var = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Dist = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_NatFloat DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NatFloat instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NatFloat result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NatFloat instance)
	{
		if (instance.Avg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Avg);
		}
		if (instance.Var != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Var);
		}
		if (instance.Dist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Dist);
		}
	}

	public static byte[] SerializeToBytes(Packet_NatFloat instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NatFloat instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
