public class Packet_CubeSerializer
{
	private const int field = 8;

	public static Packet_Cube DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_Cube instance = new Packet_Cube();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_Cube DeserializeBuffer(byte[] buffer, int length, Packet_Cube instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_Cube Deserialize(CitoMemoryStream stream, Packet_Cube instance)
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
				instance.Minx = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Miny = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Minz = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Maxx = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Maxy = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Maxz = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_Cube DeserializeLengthDelimited(CitoMemoryStream stream, Packet_Cube instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_Cube result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_Cube instance)
	{
		if (instance.Minx != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.Minx);
		}
		if (instance.Miny != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Miny);
		}
		if (instance.Minz != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Minz);
		}
		if (instance.Maxx != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Maxx);
		}
		if (instance.Maxy != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Maxy);
		}
		if (instance.Maxz != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Maxz);
		}
	}

	public static byte[] SerializeToBytes(Packet_Cube instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_Cube instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
