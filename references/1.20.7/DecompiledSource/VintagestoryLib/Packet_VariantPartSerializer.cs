public class Packet_VariantPartSerializer
{
	private const int field = 8;

	public static Packet_VariantPart DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_VariantPart instance = new Packet_VariantPart();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_VariantPart DeserializeBuffer(byte[] buffer, int length, Packet_VariantPart instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_VariantPart Deserialize(CitoMemoryStream stream, Packet_VariantPart instance)
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
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Value = ProtocolParser.ReadString(stream);
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

	public static Packet_VariantPart DeserializeLengthDelimited(CitoMemoryStream stream, Packet_VariantPart instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_VariantPart result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_VariantPart instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.Value != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Value));
		}
	}

	public static byte[] SerializeToBytes(Packet_VariantPart instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_VariantPart instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
