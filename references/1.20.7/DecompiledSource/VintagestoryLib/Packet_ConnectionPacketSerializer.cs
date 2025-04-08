public class Packet_ConnectionPacketSerializer
{
	private const int field = 8;

	public static Packet_ConnectionPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ConnectionPacket instance = new Packet_ConnectionPacket();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ConnectionPacket DeserializeBuffer(byte[] buffer, int length, Packet_ConnectionPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ConnectionPacket Deserialize(CitoMemoryStream stream, Packet_ConnectionPacket instance)
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
				instance.LoginToken = ProtocolParser.ReadString(stream);
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

	public static Packet_ConnectionPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ConnectionPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ConnectionPacket result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ConnectionPacket instance)
	{
		if (instance.LoginToken != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.LoginToken));
		}
	}

	public static byte[] SerializeToBytes(Packet_ConnectionPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ConnectionPacket instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
