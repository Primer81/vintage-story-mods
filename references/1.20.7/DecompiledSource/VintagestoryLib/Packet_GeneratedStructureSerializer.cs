public class Packet_GeneratedStructureSerializer
{
	private const int field = 8;

	public static Packet_GeneratedStructure DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_GeneratedStructure instance = new Packet_GeneratedStructure();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_GeneratedStructure DeserializeBuffer(byte[] buffer, int length, Packet_GeneratedStructure instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_GeneratedStructure Deserialize(CitoMemoryStream stream, Packet_GeneratedStructure instance)
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
				instance.X1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z1 = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.X2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Y2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Z2 = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.Code = ProtocolParser.ReadString(stream);
				break;
			case 66:
				instance.Group = ProtocolParser.ReadString(stream);
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

	public static Packet_GeneratedStructure DeserializeLengthDelimited(CitoMemoryStream stream, Packet_GeneratedStructure instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_GeneratedStructure result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_GeneratedStructure instance)
	{
		if (instance.X1 != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X1);
		}
		if (instance.Y1 != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y1);
		}
		if (instance.Z1 != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z1);
		}
		if (instance.X2 != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.X2);
		}
		if (instance.Y2 != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Y2);
		}
		if (instance.Z2 != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Z2);
		}
		if (instance.Code != null)
		{
			stream.WriteByte(58);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Code));
		}
		if (instance.Group != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Group));
		}
	}

	public static byte[] SerializeToBytes(Packet_GeneratedStructure instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_GeneratedStructure instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
