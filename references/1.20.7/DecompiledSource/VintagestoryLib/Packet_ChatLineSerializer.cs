public class Packet_ChatLineSerializer
{
	private const int field = 8;

	public static Packet_ChatLine DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ChatLine instance = new Packet_ChatLine();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ChatLine DeserializeBuffer(byte[] buffer, int length, Packet_ChatLine instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ChatLine Deserialize(CitoMemoryStream stream, Packet_ChatLine instance)
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
				instance.Message = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Groupid = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.ChatType = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.Data = ProtocolParser.ReadString(stream);
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

	public static Packet_ChatLine DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ChatLine instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ChatLine result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ChatLine instance)
	{
		if (instance.Message != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Message));
		}
		if (instance.Groupid != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Groupid);
		}
		if (instance.ChatType != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.ChatType);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Data));
		}
	}

	public static byte[] SerializeToBytes(Packet_ChatLine instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ChatLine instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
