public class Packet_PlayerModeSerializer
{
	private const int field = 8;

	public static Packet_PlayerMode DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_PlayerMode instance = new Packet_PlayerMode();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_PlayerMode DeserializeBuffer(byte[] buffer, int length, Packet_PlayerMode instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_PlayerMode Deserialize(CitoMemoryStream stream, Packet_PlayerMode instance)
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
				instance.PlayerUID = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.GameMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.MoveSpeed = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.FreeMove = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.NoClip = ProtocolParser.ReadUInt32(stream);
				break;
			case 48:
				instance.ViewDistance = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.PickingRange = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.FreeMovePlaneLock = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.ImmersiveFpMode = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.RenderMetaBlocks = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_PlayerMode DeserializeLengthDelimited(CitoMemoryStream stream, Packet_PlayerMode instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_PlayerMode result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_PlayerMode instance)
	{
		if (instance.PlayerUID != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.PlayerUID));
		}
		if (instance.GameMode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.GameMode);
		}
		if (instance.MoveSpeed != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.MoveSpeed);
		}
		if (instance.FreeMove != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.FreeMove);
		}
		if (instance.NoClip != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.NoClip);
		}
		if (instance.ViewDistance != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.ViewDistance);
		}
		if (instance.PickingRange != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.PickingRange);
		}
		if (instance.FreeMovePlaneLock != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.FreeMovePlaneLock);
		}
		if (instance.ImmersiveFpMode != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.ImmersiveFpMode);
		}
		if (instance.RenderMetaBlocks != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.RenderMetaBlocks);
		}
	}

	public static byte[] SerializeToBytes(Packet_PlayerMode instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_PlayerMode instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
