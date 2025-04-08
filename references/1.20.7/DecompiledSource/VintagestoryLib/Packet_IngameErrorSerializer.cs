public class Packet_IngameErrorSerializer
{
	private const int field = 8;

	public static Packet_IngameError DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_IngameError instance = new Packet_IngameError();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_IngameError DeserializeBuffer(byte[] buffer, int length, Packet_IngameError instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_IngameError Deserialize(CitoMemoryStream stream, Packet_IngameError instance)
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
				instance.Message = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.LangParamsAdd(ProtocolParser.ReadString(stream));
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

	public static Packet_IngameError DeserializeLengthDelimited(CitoMemoryStream stream, Packet_IngameError instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_IngameError result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_IngameError instance)
	{
		if (instance.Code != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.Message != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Message));
		}
		if (instance.LangParams != null)
		{
			for (int j = 0; j < instance.LangParamsCount; j++)
			{
				string i3 = instance.LangParams[j];
				stream.WriteByte(26);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i3));
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_IngameError instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_IngameError instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
