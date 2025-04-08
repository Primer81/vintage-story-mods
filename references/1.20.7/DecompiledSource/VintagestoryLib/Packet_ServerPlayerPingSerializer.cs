public class Packet_ServerPlayerPingSerializer
{
	private const int field = 8;

	public static Packet_ServerPlayerPing DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerPlayerPing instance = new Packet_ServerPlayerPing();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerPlayerPing DeserializeBuffer(byte[] buffer, int length, Packet_ServerPlayerPing instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerPlayerPing Deserialize(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
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
				instance.ClientIdsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.PingsAdd(ProtocolParser.ReadUInt32(stream));
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

	public static Packet_ServerPlayerPing DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerPlayerPing instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerPlayerPing result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		if (instance.ClientIds != null)
		{
			for (int k = 0; k < instance.ClientIdsCount; k++)
			{
				int i1 = instance.ClientIds[k];
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, i1);
			}
		}
		if (instance.Pings != null)
		{
			for (int j = 0; j < instance.PingsCount; j++)
			{
				int i2 = instance.Pings[j];
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, i2);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerPlayerPing instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerPlayerPing instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
