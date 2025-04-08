public class Packet_ServerLevelProgressSerializer
{
	private const int field = 8;

	public static Packet_ServerLevelProgress DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerLevelProgress instance = new Packet_ServerLevelProgress();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerLevelProgress DeserializeBuffer(byte[] buffer, int length, Packet_ServerLevelProgress instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerLevelProgress Deserialize(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
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
			case 16:
				instance.PercentComplete = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				instance.Status = ProtocolParser.ReadString(stream);
				break;
			case 32:
				instance.PercentCompleteSubitem = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerLevelProgress DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerLevelProgress instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerLevelProgress result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		if (instance.PercentComplete != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PercentComplete);
		}
		if (instance.Status != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Status));
		}
		if (instance.PercentCompleteSubitem != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PercentCompleteSubitem);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerLevelProgress instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerLevelProgress instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
