public class Packet_UnloadMapRegionSerializer
{
	private const int field = 8;

	public static Packet_UnloadMapRegion DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadMapRegion instance = new Packet_UnloadMapRegion();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UnloadMapRegion DeserializeBuffer(byte[] buffer, int length, Packet_UnloadMapRegion instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadMapRegion Deserialize(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
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
				instance.RegionX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.RegionZ = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_UnloadMapRegion DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadMapRegion instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UnloadMapRegion result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		if (instance.RegionX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.RegionX);
		}
		if (instance.RegionZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.RegionZ);
		}
	}

	public static byte[] SerializeToBytes(Packet_UnloadMapRegion instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadMapRegion instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
