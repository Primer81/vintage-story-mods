public class Packet_EntityPositionSerializer
{
	private const int field = 8;

	public static Packet_EntityPosition DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityPosition instance = new Packet_EntityPosition();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityPosition DeserializeBuffer(byte[] buffer, int length, Packet_EntityPosition instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityPosition Deserialize(CitoMemoryStream stream, Packet_EntityPosition instance)
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
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 16:
				instance.X = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.Y = ProtocolParser.ReadUInt64(stream);
				break;
			case 32:
				instance.Z = ProtocolParser.ReadUInt64(stream);
				break;
			case 40:
				instance.Yaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.Pitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.Roll = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.HeadYaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.HeadPitch = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.BodyYaw = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.Controls = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.Tick = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.PositionVersion = ProtocolParser.ReadUInt32(stream);
				break;
			case 112:
				instance.MotionX = ProtocolParser.ReadUInt64(stream);
				break;
			case 120:
				instance.MotionY = ProtocolParser.ReadUInt64(stream);
				break;
			case 128:
				instance.MotionZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 136:
				instance.Teleport = ProtocolParser.ReadBool(stream);
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

	public static Packet_EntityPosition DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityPosition instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityPosition result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityPosition instance)
	{
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.X != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.X);
		}
		if (instance.Y != 0L)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt64(stream, instance.Y);
		}
		if (instance.Z != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.Z);
		}
		if (instance.Yaw != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Yaw);
		}
		if (instance.Pitch != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Pitch);
		}
		if (instance.Roll != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.Roll);
		}
		if (instance.HeadYaw != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.HeadYaw);
		}
		if (instance.HeadPitch != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.HeadPitch);
		}
		if (instance.BodyYaw != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.BodyYaw);
		}
		if (instance.Controls != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.Controls);
		}
		if (instance.Tick != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Tick);
		}
		if (instance.PositionVersion != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.PositionVersion);
		}
		if (instance.MotionX != 0L)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt64(stream, instance.MotionX);
		}
		if (instance.MotionY != 0L)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteUInt64(stream, instance.MotionY);
		}
		if (instance.MotionZ != 0L)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt64(stream, instance.MotionZ);
		}
		if (instance.Teleport)
		{
			stream.WriteKey(17, 0);
			ProtocolParser.WriteBool(stream, instance.Teleport);
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityPosition instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityPosition instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
