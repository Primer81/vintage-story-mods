public class Packet_EntityInteractionSerializer
{
	private const int field = 8;

	public static Packet_EntityInteraction DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_EntityInteraction instance = new Packet_EntityInteraction();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_EntityInteraction DeserializeBuffer(byte[] buffer, int length, Packet_EntityInteraction instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_EntityInteraction Deserialize(CitoMemoryStream stream, Packet_EntityInteraction instance)
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
			case 16:
				instance.EntityId = ProtocolParser.ReadUInt64(stream);
				break;
			case 24:
				instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.HitX = ProtocolParser.ReadUInt64(stream);
				break;
			case 40:
				instance.HitY = ProtocolParser.ReadUInt64(stream);
				break;
			case 48:
				instance.HitZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 56:
				instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_EntityInteraction DeserializeLengthDelimited(CitoMemoryStream stream, Packet_EntityInteraction instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_EntityInteraction result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_EntityInteraction instance)
	{
		if (instance.MouseButton != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.MouseButton);
		}
		if (instance.EntityId != 0L)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt64(stream, instance.EntityId);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
	}

	public static byte[] SerializeToBytes(Packet_EntityInteraction instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_EntityInteraction instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
