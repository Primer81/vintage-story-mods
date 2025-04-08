public class Packet_MoveKeyChangeSerializer
{
	private const int field = 8;

	public static Packet_MoveKeyChange DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MoveKeyChange instance = new Packet_MoveKeyChange();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_MoveKeyChange DeserializeBuffer(byte[] buffer, int length, Packet_MoveKeyChange instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MoveKeyChange Deserialize(CitoMemoryStream stream, Packet_MoveKeyChange instance)
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
				instance.Key = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Down = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_MoveKeyChange DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MoveKeyChange instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_MoveKeyChange result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_MoveKeyChange instance)
	{
		if (instance.Key != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key);
		}
		if (instance.Down != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Down);
		}
	}

	public static byte[] SerializeToBytes(Packet_MoveKeyChange instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MoveKeyChange instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
