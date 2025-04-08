public class Packet_StopMovementSerializer
{
	private const int field = 8;

	public static Packet_StopMovement DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_StopMovement instance = new Packet_StopMovement();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_StopMovement DeserializeBuffer(byte[] buffer, int length, Packet_StopMovement instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_StopMovement Deserialize(CitoMemoryStream stream, Packet_StopMovement instance)
	{
		instance.InitializeValues();
		while (true)
		{
			int keyInt = stream.ReadByte();
			if (((uint)keyInt & 0x80u) != 0)
			{
				keyInt = ProtocolParser.ReadKeyAsInt(keyInt, stream);
				if (((uint)keyInt & 0x4000u) != 0)
				{
					if (keyInt < 0)
					{
						break;
					}
					return null;
				}
			}
			if (keyInt == 0)
			{
				return null;
			}
			ProtocolParser.SkipKey(stream, Key.Create(keyInt));
		}
		return instance;
	}

	public static Packet_StopMovement DeserializeLengthDelimited(CitoMemoryStream stream, Packet_StopMovement instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_StopMovement result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_StopMovement instance)
	{
	}

	public static byte[] SerializeToBytes(Packet_StopMovement instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_StopMovement instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
