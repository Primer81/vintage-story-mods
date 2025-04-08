public class Packet_WorldMetaDataSerializer
{
	private const int field = 8;

	public static Packet_WorldMetaData DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_WorldMetaData instance = new Packet_WorldMetaData();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_WorldMetaData DeserializeBuffer(byte[] buffer, int length, Packet_WorldMetaData instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_WorldMetaData Deserialize(CitoMemoryStream stream, Packet_WorldMetaData instance)
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
				instance.SunBrightness = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.BlockLightlevelsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 24:
				instance.SunLightlevelsAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 34:
				instance.WorldConfiguration = ProtocolParser.ReadBytes(stream);
				break;
			case 40:
				instance.SeaLevel = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_WorldMetaData DeserializeLengthDelimited(CitoMemoryStream stream, Packet_WorldMetaData instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_WorldMetaData result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_WorldMetaData instance)
	{
		if (instance.SunBrightness != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SunBrightness);
		}
		if (instance.BlockLightlevels != null)
		{
			for (int k = 0; k < instance.BlockLightlevelsCount; k++)
			{
				int i2 = instance.BlockLightlevels[k];
				stream.WriteByte(16);
				ProtocolParser.WriteUInt32(stream, i2);
			}
		}
		if (instance.SunLightlevels != null)
		{
			for (int j = 0; j < instance.SunLightlevelsCount; j++)
			{
				int i3 = instance.SunLightlevels[j];
				stream.WriteByte(24);
				ProtocolParser.WriteUInt32(stream, i3);
			}
		}
		if (instance.WorldConfiguration != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, instance.WorldConfiguration);
		}
		if (instance.SeaLevel != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.SeaLevel);
		}
	}

	public static byte[] SerializeToBytes(Packet_WorldMetaData instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_WorldMetaData instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
