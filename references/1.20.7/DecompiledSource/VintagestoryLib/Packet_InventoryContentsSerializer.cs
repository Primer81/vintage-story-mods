public class Packet_InventoryContentsSerializer
{
	private const int field = 8;

	public static Packet_InventoryContents DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_InventoryContents instance = new Packet_InventoryContents();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_InventoryContents DeserializeBuffer(byte[] buffer, int length, Packet_InventoryContents instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_InventoryContents Deserialize(CitoMemoryStream stream, Packet_InventoryContents instance)
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
				instance.InventoryClass = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.InventoryId = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.ItemstacksAdd(Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_InventoryContents DeserializeLengthDelimited(CitoMemoryStream stream, Packet_InventoryContents instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_InventoryContents result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_InventoryContents instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.ClientId != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.InventoryClass != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InventoryClass));
		}
		if (instance.InventoryId != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InventoryId));
		}
		if (instance.Itemstacks != null)
		{
			for (int j = 0; j < instance.ItemstacksCount; j++)
			{
				Packet_ItemStack i4 = instance.Itemstacks[j];
				stream.WriteByte(34);
				CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
				Packet_ItemStackSerializer.Serialize(ms4, i4);
				int len = ms4.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms4.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_InventoryContents instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_InventoryContents instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
