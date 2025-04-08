public class Packet_IntStringSerializer
{
	private const int field = 8;

	public static Packet_IntString DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IntString instance = new Packet_IntString();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IntString DeserializeBuffer(byte[] buffer, int length, Packet_IntString instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IntString Deserialize(CitoMemoryStream stream, Packet_IntString instance)
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
				instance.Key_ = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.Value_ = ProtocolParser.ReadString(stream);
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

	public static Packet_IntString DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IntString instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IntString result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IntString instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
		if (instance.Value_ != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Value_));
		}
	}

	public static byte[] SerializeToBytes(Packet_IntString instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IntString instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
