public class Packet_ServerMapChunkSerializer
{
	private const int field = 8;

	public static Packet_ServerMapChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerMapChunk instance = new Packet_ServerMapChunk();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerMapChunk DeserializeBuffer(byte[] buffer, int length, Packet_ServerMapChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerMapChunk Deserialize(CitoMemoryStream stream, Packet_ServerMapChunk instance)
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
				instance.ChunkX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ChunkZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Ymax = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
				instance.RainHeightMap = ProtocolParser.ReadBytes(stream);
				break;
			case 58:
				instance.TerrainHeightMap = ProtocolParser.ReadBytes(stream);
				break;
			case 50:
				instance.Structures = ProtocolParser.ReadBytes(stream);
				break;
			case 66:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
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

	public static Packet_ServerMapChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerMapChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerMapChunk result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerMapChunk instance)
	{
		if (instance.ChunkX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ChunkX);
		}
		if (instance.ChunkZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ChunkZ);
		}
		if (instance.Ymax != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Ymax);
		}
		if (instance.RainHeightMap != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.RainHeightMap);
		}
		if (instance.TerrainHeightMap != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteBytes(stream, instance.TerrainHeightMap);
		}
		if (instance.Structures != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.Structures);
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerMapChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerMapChunk instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
