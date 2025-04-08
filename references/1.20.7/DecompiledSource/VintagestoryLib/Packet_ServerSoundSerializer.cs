public class Packet_ServerSoundSerializer
{
	private const int field = 8;

	public static Packet_ServerSound DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerSound instance = new Packet_ServerSound();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerSound DeserializeBuffer(byte[] buffer, int length, Packet_ServerSound instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerSound Deserialize(CitoMemoryStream stream, Packet_ServerSound instance)
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
				instance.Name = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Pitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Range = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.Volume = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.SoundType = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ServerSound DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerSound instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerSound result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerSound instance)
	{
		if (instance.Name != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Name));
		}
		if (instance.X != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Pitch != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Range != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Range);
		}
		if (instance.Volume != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Volume);
		}
		if (instance.SoundType != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.SoundType);
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerSound instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerSound instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
