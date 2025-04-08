public class Packet_BlockEntitySerializer
{
	private const int field = 8;

	public static Packet_BlockEntity DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockEntity instance = new Packet_BlockEntity();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockEntity DeserializeBuffer(byte[] buffer, int length, Packet_BlockEntity instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockEntity Deserialize(CitoMemoryStream stream, Packet_BlockEntity instance)
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
				instance.Classname = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.PosX = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.PosY = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.PosZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
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

	public static Packet_BlockEntity DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockEntity instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockEntity result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockEntity instance)
	{
		if (instance.Classname != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Classname));
		}
		if (instance.PosX != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.Data != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, instance.Data);
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockEntity instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockEntity instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
