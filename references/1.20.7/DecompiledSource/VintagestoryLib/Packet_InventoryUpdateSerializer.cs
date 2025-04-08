public class Packet_InventoryUpdateSerializer
{
	private const int field = 8;

	public static Packet_InventoryUpdate DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryUpdate instance = new Packet_InventoryUpdate();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InventoryUpdate DeserializeBuffer(byte[] buffer, int length, Packet_InventoryUpdate instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryUpdate Deserialize(CitoMemoryStream stream, Packet_InventoryUpdate instance)
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
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 24:
				instance.SlotId = ProtocolParser.ReadUInt32(stream);
				break;
			case 34:
				if (instance.ItemStack == null)
				{
					instance.ItemStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.ItemStack);
				}
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

	public static Packet_InventoryUpdate DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryUpdate instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InventoryUpdate result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryUpdate instance)
	{
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InventoryId));
		}
		if (instance.SlotId != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.SlotId);
		}
		if (instance.ItemStack != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms4 = new CitoMemoryStream();
			Packet_ItemStackSerializer.Serialize(ms4, instance.ItemStack);
			int len = ms4.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms4.GetBuffer(), 0, len);
		}
	}

	public static byte[] SerializeToBytes(Packet_InventoryUpdate instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryUpdate instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
