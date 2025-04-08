public class Packet_ClientSpecialKeySerializer
{
	private const int field = 8;

	public static Packet_ClientSpecialKey DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientSpecialKey instance = new Packet_ClientSpecialKey();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientSpecialKey DeserializeBuffer(byte[] buffer, int length, Packet_ClientSpecialKey instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientSpecialKey Deserialize(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
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

	public static Packet_ClientSpecialKey DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientSpecialKey instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientSpecialKey result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		if (instance.Key_ != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Key_);
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientSpecialKey instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientSpecialKey instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
