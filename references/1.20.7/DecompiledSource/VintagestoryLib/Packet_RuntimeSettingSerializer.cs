public class Packet_RuntimeSettingSerializer
{
	private const int field = 8;

	public static Packet_RuntimeSetting DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_RuntimeSetting instance = new Packet_RuntimeSetting();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_RuntimeSetting DeserializeBuffer(byte[] buffer, int length, Packet_RuntimeSetting instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_RuntimeSetting Deserialize(CitoMemoryStream stream, Packet_RuntimeSetting instance)
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
				instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ItemCollectMode = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_RuntimeSetting DeserializeLengthDelimited(CitoMemoryStream stream, Packet_RuntimeSetting instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_RuntimeSetting result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_RuntimeSetting instance)
	{
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.ItemCollectMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ItemCollectMode);
		}
	}

	public static byte[] SerializeToBytes(Packet_RuntimeSetting instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_RuntimeSetting instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
