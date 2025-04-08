public class Packet_LoginTokenAnswerSerializer
{
	private const int field = 8;

	public static Packet_LoginTokenAnswer DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_LoginTokenAnswer instance = new Packet_LoginTokenAnswer();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_LoginTokenAnswer DeserializeBuffer(byte[] buffer, int length, Packet_LoginTokenAnswer instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_LoginTokenAnswer Deserialize(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
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
				instance.Token = ProtocolParser.ReadString(stream);
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

	public static Packet_LoginTokenAnswer DeserializeLengthDelimited(CitoMemoryStream stream, Packet_LoginTokenAnswer instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_LoginTokenAnswer result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		if (instance.Token != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Token));
		}
	}

	public static byte[] SerializeToBytes(Packet_LoginTokenAnswer instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_LoginTokenAnswer instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
