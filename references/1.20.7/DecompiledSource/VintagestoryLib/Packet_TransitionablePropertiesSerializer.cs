public class Packet_TransitionablePropertiesSerializer
{
	private const int field = 8;

	public static Packet_TransitionableProperties DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_TransitionableProperties instance = new Packet_TransitionableProperties();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_TransitionableProperties DeserializeBuffer(byte[] buffer, int length, Packet_TransitionableProperties instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_TransitionableProperties Deserialize(CitoMemoryStream stream, Packet_TransitionableProperties instance)
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
				if (instance.FreshHours == null)
				{
					instance.FreshHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.FreshHours);
				}
				break;
			case 18:
				if (instance.TransitionHours == null)
				{
					instance.TransitionHours = Packet_NatFloatSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_NatFloatSerializer.DeserializeLengthDelimited(stream, instance.TransitionHours);
				}
				break;
			case 26:
				instance.TransitionedStack = ProtocolParser.ReadBytes(stream);
				break;
			case 32:
				instance.TransitionRatio = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Type = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_TransitionableProperties DeserializeLengthDelimited(CitoMemoryStream stream, Packet_TransitionableProperties instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_TransitionableProperties result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_TransitionableProperties instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.FreshHours != null)
		{
			stream.WriteByte(10);
			CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
			Packet_NatFloatSerializer.Serialize(ms1, instance.FreshHours);
			int len2 = ms1.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms1.GetBuffer(), 0, len2);
		}
		if (instance.TransitionHours != null)
		{
			stream.WriteByte(18);
			CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
			Packet_NatFloatSerializer.Serialize(ms2, instance.TransitionHours);
			int len = ms2.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms2.GetBuffer(), 0, len);
		}
		if (instance.TransitionedStack != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, instance.TransitionedStack);
		}
		if (instance.TransitionRatio != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TransitionRatio);
		}
		if (instance.Type != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Type);
		}
	}

	public static byte[] SerializeToBytes(Packet_TransitionableProperties instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_TransitionableProperties instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
