public class Packet_UnloadServerChunkSerializer
{
	private const int field = 8;

	public static Packet_UnloadServerChunk DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UnloadServerChunk instance = new Packet_UnloadServerChunk();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UnloadServerChunk DeserializeBuffer(byte[] buffer, int length, Packet_UnloadServerChunk instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UnloadServerChunk Deserialize(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
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
				instance.XAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 16:
				instance.YAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.ZAdd(ProtocolParser.ReadUInt32(stream));
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

	public static Packet_UnloadServerChunk DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UnloadServerChunk instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UnloadServerChunk result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		if (instance.X != null)
		{
			for (int l = 0; l < instance.XCount; l++)
			{
				int i1 = instance.X[l];
				stream.WriteByte(8);
				ProtocolParser.WriteUInt32(stream, i1);
			}
		}
		if (instance.Y != null)
		{
			for (int k = 0; k < instance.YCount; k++)
			{
				int i2 = instance.Y[k];
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, i2);
			}
		}
		if (instance.Z != null)
		{
			for (int j = 0; j < instance.ZCount; j++)
			{
				int i3 = instance.Z[j];
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, i3);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_UnloadServerChunk instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UnloadServerChunk instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
