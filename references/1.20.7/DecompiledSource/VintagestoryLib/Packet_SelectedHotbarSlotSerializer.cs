public class Packet_SelectedHotbarSlotSerializer
{
	private const int field = 8;

	public static Packet_SelectedHotbarSlot DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_SelectedHotbarSlot instance = new Packet_SelectedHotbarSlot();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_SelectedHotbarSlot DeserializeBuffer(byte[] buffer, int length, Packet_SelectedHotbarSlot instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_SelectedHotbarSlot Deserialize(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
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
				instance.SlotNumber = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.ClientId = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				if (instance.Itemstack == null)
				{
					instance.Itemstack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.Itemstack);
				}
				break;
			case 34:
				if (instance.OffhandStack == null)
				{
					instance.OffhandStack = Packet_ItemStackSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ItemStackSerializer.DeserializeLengthDelimited(stream, instance.OffhandStack);
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

	public static Packet_SelectedHotbarSlot DeserializeLengthDelimited(CitoMemoryStream stream, Packet_SelectedHotbarSlot instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_SelectedHotbarSlot result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.SlotNumber != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.SlotNumber);
		}
		if (instance.ClientId != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.ClientId);
		}
		if (instance.Itemstack != null)
		{
			stream.WriteByte(26);
			CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
			Packet_ItemStackSerializer.Serialize(ms3, instance.Itemstack);
			int len2 = ms3.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms3.GetBuffer(), 0, len2);
		}
		if (instance.OffhandStack != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
			Packet_ItemStackSerializer.Serialize(ms4, instance.OffhandStack);
			int len = ms4.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms4.GetBuffer(), 0, len);
		}
	}

	public static byte[] SerializeToBytes(Packet_SelectedHotbarSlot instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_SelectedHotbarSlot instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
