public class Packet_CustomPacketSerializer
{
	private const int field = 8;

	public static Packet_CustomPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CustomPacket instance = new Packet_CustomPacket();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CustomPacket DeserializeBuffer(byte[] buffer, int length, Packet_CustomPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CustomPacket Deserialize(CitoMemoryStream stream, Packet_CustomPacket instance)
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
				instance.ChannelId = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.MessageId = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
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

	public static Packet_CustomPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CustomPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CustomPacket result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CustomPacket instance)
	{
		if (instance.ChannelId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChannelId);
		}
		if (instance.MessageId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.MessageId);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static byte[] SerializeToBytes(Packet_CustomPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CustomPacket instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
