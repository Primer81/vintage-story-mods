public class Packet_IntMapSerializer
{
	private const int field = 8;

	public static Packet_IntMap DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntMap instance = new Packet_IntMap();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IntMap DeserializeBuffer(byte[] buffer, int length, Packet_IntMap instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntMap Deserialize(CitoMemoryStream stream, Packet_IntMap instance)
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
				instance.DataAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.Size = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.TopLeftPadding = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.BottomRightPadding = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_IntMap DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntMap instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IntMap result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IntMap instance)
	{
		if (instance.Data != null)
		{
			for (int j = 0; j < instance.DataCount; j++)
			{
				int i1 = instance.Data[j];
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, i1);
			}
		}
		if (instance.Size != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Size);
		}
		if (instance.TopLeftPadding != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TopLeftPadding);
		}
		if (instance.BottomRightPadding != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.BottomRightPadding);
		}
	}

	public static byte[] SerializeToBytes(Packet_IntMap instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntMap instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
