public class Packet_ActivateInventorySlotSerializer
{
	private const int field = 8;

	public static Packet_ActivateInventorySlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ActivateInventorySlot instance = new Packet_ActivateInventorySlot();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ActivateInventorySlot DeserializeBuffer(byte[] buffer, int length, Packet_ActivateInventorySlot instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ActivateInventorySlot Deserialize(CitoMemoryStream stream, Packet_ActivateInventorySlot instance)
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
				instance.MouseButton = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Modifiers = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.TargetInventoryId = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.TargetSlot = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.TargetLastChanged = ProtocolParser.ReadUInt64(stream);
				break;
			case 48:
				instance.TabIndex = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.Priority = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.Dir = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ActivateInventorySlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ActivateInventorySlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ActivateInventorySlot result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ActivateInventorySlot instance)
	{
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.Modifiers != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Modifiers);
		}
		if (instance.TargetInventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.TargetInventoryId));
		}
		if (instance.TargetSlot != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.TargetSlot);
		}
		if (instance.TargetLastChanged != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.TargetLastChanged);
		}
		if (instance.TabIndex != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.TabIndex);
		}
		if (instance.Priority != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Priority);
		}
		if (instance.Dir != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.Dir);
		}
	}

	public static byte[] SerializeToBytes(Packet_ActivateInventorySlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ActivateInventorySlot instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
