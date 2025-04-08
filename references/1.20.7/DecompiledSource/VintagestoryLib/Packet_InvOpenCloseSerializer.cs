public class Packet_InvOpenCloseSerializer
{
	private const int field = 8;

	public static Packet_InvOpenClose DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InvOpenClose instance = new Packet_InvOpenClose();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InvOpenClose DeserializeBuffer(byte[] buffer, int length, Packet_InvOpenClose instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InvOpenClose Deserialize(CitoMemoryStream stream, Packet_InvOpenClose instance)
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
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Opened = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_InvOpenClose DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InvOpenClose instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InvOpenClose result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InvOpenClose instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InventoryId));
		}
		if (instance.Opened != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Opened);
		}
	}

	public static byte[] SerializeToBytes(Packet_InvOpenClose instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InvOpenClose instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
