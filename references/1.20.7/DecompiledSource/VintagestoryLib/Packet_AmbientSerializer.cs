public class Packet_AmbientSerializer
{
	private const int field = 8;

	public static Packet_Ambient DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Ambient instance = new Packet_Ambient();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Ambient DeserializeBuffer(byte[] buffer, int length, Packet_Ambient instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Ambient Deserialize(CitoMemoryStream stream, Packet_Ambient instance)
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

	public static Packet_Ambient DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Ambient instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Ambient result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Ambient instance)
	{
		if (instance.Data != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static byte[] SerializeToBytes(Packet_Ambient instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Ambient instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
