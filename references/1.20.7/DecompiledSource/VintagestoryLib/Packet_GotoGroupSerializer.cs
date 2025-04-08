public class Packet_GotoGroupSerializer
{
	private const int field = 8;

	public static Packet_GotoGroup DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GotoGroup instance = new Packet_GotoGroup();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_GotoGroup DeserializeBuffer(byte[] buffer, int length, Packet_GotoGroup instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GotoGroup Deserialize(CitoMemoryStream stream, Packet_GotoGroup instance)
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
				instance.GroupId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_GotoGroup DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GotoGroup instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_GotoGroup result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_GotoGroup instance)
	{
		if (instance.GroupId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.GroupId);
		}
	}

	public static byte[] SerializeToBytes(Packet_GotoGroup instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GotoGroup instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
