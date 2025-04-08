public class Packet_ClientBlockPlaceOrBreakSerializer
{
	private const int field = 8;

	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ClientBlockPlaceOrBreak instance = new Packet_ClientBlockPlaceOrBreak();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ClientBlockPlaceOrBreak DeserializeBuffer(byte[] buffer, int length, Packet_ClientBlockPlaceOrBreak instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ClientBlockPlaceOrBreak Deserialize(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
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
				instance.X = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.Y = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Z = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Mode = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.BlockType = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.OnBlockFace = ProtocolParser.ReadUInt32(stream);
				break;
			case 64:
				instance.HitX = ProtocolParser.ReadUInt64(stream);
				break;
			case 72:
				instance.HitY = ProtocolParser.ReadUInt64(stream);
				break;
			case 80:
				instance.HitZ = ProtocolParser.ReadUInt64(stream);
				break;
			case 88:
				instance.SelectionBoxIndex = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.DidOffset = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_ClientBlockPlaceOrBreak DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ClientBlockPlaceOrBreak result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		if (instance.X != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.X);
		}
		if (instance.Y != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Y);
		}
		if (instance.Z != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Z);
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
		if (instance.BlockType != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.BlockType);
		}
		if (instance.OnBlockFace != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.OnBlockFace);
		}
		if (instance.HitX != 0L)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt64(stream, instance.HitX);
		}
		if (instance.HitY != 0L)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt64(stream, instance.HitY);
		}
		if (instance.HitZ != 0L)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt64(stream, instance.HitZ);
		}
		if (instance.SelectionBoxIndex != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.SelectionBoxIndex);
		}
		if (instance.DidOffset != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.DidOffset);
		}
	}

	public static byte[] SerializeToBytes(Packet_ClientBlockPlaceOrBreak instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ClientBlockPlaceOrBreak instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
