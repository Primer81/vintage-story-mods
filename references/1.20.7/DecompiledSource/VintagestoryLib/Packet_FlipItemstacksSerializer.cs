public class Packet_FlipItemstacksSerializer
{
	private const int field = 8;

	public static Packet_FlipItemstacks DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_FlipItemstacks instance = new Packet_FlipItemstacks();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_FlipItemstacks DeserializeBuffer(byte[] buffer, int length, Packet_FlipItemstacks instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_FlipItemstacks Deserialize(CitoMemoryStream stream, Packet_FlipItemstacks instance)
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
				instance.SourceInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.TargetInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.SourceSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.SourceLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 48:
				instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 56:
				instance.SourceTabIndex = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.TargetTabIndex = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_FlipItemstacks DeserializeLengthDelimited(CitoMemoryStream stream, Packet_FlipItemstacks instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_FlipItemstacks result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_FlipItemstacks instance)
	{
		if (instance.SourceInventoryId != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.SourceInventoryId));
		}
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.TargetInventoryId));
		}
		if (instance.SourceSlot != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SourceSlot);
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.SourceLastChanged != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.SourceLastChanged);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.SourceTabIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SourceTabIndex);
		}
		if (instance.TargetTabIndex != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.TargetTabIndex);
		}
	}

	public static byte[] SerializeToBytes(Packet_FlipItemstacks instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_FlipItemstacks instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
