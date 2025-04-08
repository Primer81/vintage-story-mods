public class Packet_MoveItemstackSerializer
{
	private const int field = 8;

	public static Packet_MoveItemstack DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MoveItemstack instance = new Packet_MoveItemstack();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_MoveItemstack DeserializeBuffer(byte[] buffer, int length, Packet_MoveItemstack instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MoveItemstack Deserialize(CitoMemoryStream stream, Packet_MoveItemstack instance)
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
				instance.Quantity = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.SourceLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 56:
				instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 64:
				instance.MouseButton = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.Modifiers = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.Priority = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.TabIndex = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_MoveItemstack DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MoveItemstack instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_MoveItemstack result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_MoveItemstack instance)
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
		if (instance.Quantity != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Quantity);
		}
		if (instance.SourceLastChanged != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.SourceLastChanged);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.Modifiers != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.Modifiers);
		}
		if (instance.Priority != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.Priority);
		}
		if (instance.TabIndex != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.TabIndex);
		}
	}

	public static byte[] SerializeToBytes(Packet_MoveItemstack instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MoveItemstack instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
