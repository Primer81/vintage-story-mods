public class Packet_ClientPingReplySerializer
{
	private const int field = 8;

	public static Packet_ClientPingReply DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientPingReply instance = new Packet_ClientPingReply();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientPingReply DeserializeBuffer(byte[] buffer, int length, Packet_ClientPingReply instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientPingReply Deserialize(CitoMemoryStream stream, Packet_ClientPingReply instance)
	{
		instance.InitializeValues();
		while (true)
		{
			int keyInt = stream.ReadByte();
			if (((uint)keyInt & 0x80u) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if (((uint)keyInt & 0x4000u) != 0)
				{
					if (keyInt < 0)
					{
						break;
					}
					return null;
				}
			}
			if (keyInt == 0)
			{
				return null;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		return instance;
	}

	public static Packet_ClientPingReply DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientPingReply instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientPingReply result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientPingReply instance)
	{
	}

	public static byte[] SerializeToBytes(Packet_ClientPingReply instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientPingReply instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
