public class Packet_HeldSoundSetSerializer
{
	private const int field = 8;

	public static Packet_HeldSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_HeldSoundSet instance = new Packet_HeldSoundSet();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_HeldSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_HeldSoundSet instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_HeldSoundSet Deserialize(CitoMemoryStream stream, Packet_HeldSoundSet instance)
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
				instance.Idle = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Equip = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Unequip = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.Attack = ProtocolParser.ReadString(stream);
				break;
			case 42:
				instance.InvPickup = ProtocolParser.ReadString(stream);
				break;
			case 50:
				instance.InvPlace = ProtocolParser.ReadString(stream);
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

	public static Packet_HeldSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_HeldSoundSet instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_HeldSoundSet result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_HeldSoundSet instance)
	{
		if (instance.Idle != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Idle));
		}
		if (instance.Equip != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Equip));
		}
		if (instance.Unequip != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Unequip));
		}
		if (instance.Attack != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Attack));
		}
		if (instance.InvPickup != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InvPickup));
		}
		if (instance.InvPlace != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.InvPlace));
		}
	}

	public static byte[] SerializeToBytes(Packet_HeldSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_HeldSoundSet instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
