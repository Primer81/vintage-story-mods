public class Packet_NutritionPropertiesSerializer
{
	private const int field = 8;

	public static Packet_NutritionProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NutritionProperties instance = new Packet_NutritionProperties();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NutritionProperties DeserializeBuffer(byte[] buffer, int length, Packet_NutritionProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NutritionProperties Deserialize(CitoMemoryStream stream, Packet_NutritionProperties instance)
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
				instance.FoodCategory = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Saturation = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Health = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				instance.EatenStack = ProtocolParser.ReadBytes(stream);
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

	public static Packet_NutritionProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NutritionProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NutritionProperties result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NutritionProperties instance)
	{
		if (instance.FoodCategory != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.FoodCategory);
		}
		if (instance.Saturation != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Saturation);
		}
		if (instance.Health != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Health);
		}
		if (instance.EatenStack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.EatenStack);
		}
	}

	public static byte[] SerializeToBytes(Packet_NutritionProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NutritionProperties instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
