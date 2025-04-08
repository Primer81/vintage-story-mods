public class Packet_RemoveBlockLightSerializer
{
	private const int field = 8;

	public static Packet_RemoveBlockLight DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RemoveBlockLight instance = new Packet_RemoveBlockLight();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_RemoveBlockLight DeserializeBuffer(byte[] buffer, int length, Packet_RemoveBlockLight instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RemoveBlockLight Deserialize(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
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
				instance.PosX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.PosY = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.PosZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.LightH = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.LightS = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.LightV = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_RemoveBlockLight DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RemoveBlockLight instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_RemoveBlockLight result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		if (instance.PosX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.LightH != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.LightH);
		}
		if (instance.LightS != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.LightS);
		}
		if (instance.LightV != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.LightV);
		}
	}

	public static byte[] SerializeToBytes(Packet_RemoveBlockLight instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RemoveBlockLight instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
