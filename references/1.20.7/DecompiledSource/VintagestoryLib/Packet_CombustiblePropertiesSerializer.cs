public class Packet_CombustiblePropertiesSerializer
{
	private const int field = 8;

	public static Packet_CombustibleProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CombustibleProperties instance = new Packet_CombustibleProperties();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CombustibleProperties DeserializeBuffer(byte[] buffer, int length, Packet_CombustibleProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CombustibleProperties Deserialize(CitoMemoryStream stream, Packet_CombustibleProperties instance)
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
				instance.BurnTemperature = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.BurnDuration = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.HeatResistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.MeltingPoint = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.MeltingDuration = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.SmeltedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 56:
				instance.SmeltedRatio = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.RequiresContainer = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.MeltingType = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.MaxTemperature = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CombustibleProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CombustibleProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CombustibleProperties result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CombustibleProperties instance)
	{
		if (instance.BurnTemperature != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.BurnTemperature);
		}
		if (instance.BurnDuration != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.BurnDuration);
		}
		if (instance.HeatResistance != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.HeatResistance);
		}
		if (instance.MeltingPoint != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.MeltingPoint);
		}
		if (instance.MeltingDuration != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.MeltingDuration);
		}
		if (instance.SmeltedStack != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, instance.SmeltedStack);
		}
		if (instance.SmeltedRatio != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SmeltedRatio);
		}
		if (instance.RequiresContainer != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.RequiresContainer);
		}
		if (instance.MeltingType != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.MeltingType);
		}
		if (instance.MaxTemperature != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.MaxTemperature);
		}
	}

	public static byte[] SerializeToBytes(Packet_CombustibleProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CombustibleProperties instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
