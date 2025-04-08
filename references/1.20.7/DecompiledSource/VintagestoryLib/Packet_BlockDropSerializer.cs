public class Packet_BlockDropSerializer
{
	private const int field = 8;

	public static Packet_BlockDrop DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDrop instance = new Packet_BlockDrop();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockDrop DeserializeBuffer(byte[] buffer, int length, Packet_BlockDrop instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDrop Deserialize(CitoMemoryStream stream, Packet_BlockDrop instance)
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
				instance.QuantityAvg = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.QuantityVar = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.QuantityDist = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.DroppedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 40:
				instance.Tool = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlockDrop DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDrop instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockDrop result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDrop instance)
	{
		if (instance.QuantityAvg != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.QuantityAvg);
		}
		if (instance.QuantityVar != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.QuantityVar);
		}
		if (instance.QuantityDist != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.QuantityDist);
		}
		if (instance.DroppedStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.DroppedStack);
		}
		if (instance.Tool != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Tool);
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockDrop instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDrop instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
