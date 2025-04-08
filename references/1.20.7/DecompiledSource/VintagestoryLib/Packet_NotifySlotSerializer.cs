public class Packet_NotifySlotSerializer
{
	private const int field = 8;

	public static Packet_NotifySlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_NotifySlot instance = new Packet_NotifySlot();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_NotifySlot DeserializeBuffer(byte[] buffer, int length, Packet_NotifySlot instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_NotifySlot Deserialize(CitoMemoryStream stream, Packet_NotifySlot instance)
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
				instance.SlotId = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_NotifySlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_NotifySlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_NotifySlot result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_NotifySlot instance)
	{
		if (instance.InventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InventoryId));
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
	}

	public static byte[] SerializeToBytes(Packet_NotifySlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_NotifySlot instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
