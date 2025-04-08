public class Packet_BlockDamageSerializer
{
	private const int field = 8;

	public static Packet_BlockDamage DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockDamage instance = new Packet_BlockDamage();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockDamage DeserializeBuffer(byte[] buffer, int length, Packet_BlockDamage instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockDamage Deserialize(CitoMemoryStream stream, Packet_BlockDamage instance)
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
				instance.PosX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.PosY = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.PosZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Facing = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Damage = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlockDamage DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockDamage instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockDamage result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockDamage instance)
	{
		if (instance.PosX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.PosX);
		}
		if (instance.PosY != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.PosY);
		}
		if (instance.PosZ != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.PosZ);
		}
		if (instance.Facing != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Facing);
		}
		if (instance.Damage != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Damage);
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockDamage instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockDamage instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
