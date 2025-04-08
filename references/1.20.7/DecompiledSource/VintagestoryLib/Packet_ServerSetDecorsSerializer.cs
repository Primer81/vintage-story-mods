public class Packet_ServerSetDecorsSerializer
{
	private const int field = 8;

	public static Packet_ServerSetDecors DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetDecors instance = new Packet_ServerSetDecors();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSetDecors DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetDecors instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetDecors Deserialize(CitoMemoryStream stream, Packet_ServerSetDecors instance)
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
				instance.SetDecors = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerSetDecors DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetDecors instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSetDecors result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetDecors instance)
	{
		if (instance.SetDecors != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.SetDecors);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSetDecors instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetDecors instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
