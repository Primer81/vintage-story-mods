public class Packet_ServerSetBlocksSerializer
{
	private const int field = 8;

	public static Packet_ServerSetBlocks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSetBlocks instance = new Packet_ServerSetBlocks();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSetBlocks DeserializeBuffer(byte[] buffer, int length, Packet_ServerSetBlocks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSetBlocks Deserialize(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
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
				instance.SetBlocks = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerSetBlocks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSetBlocks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSetBlocks result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		if (instance.SetBlocks != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, instance.SetBlocks);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSetBlocks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSetBlocks instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
