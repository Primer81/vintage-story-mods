public class Packet_BlockSoundSetSerializer
{
	private const int field = 8;

	public static Packet_BlockSoundSet DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlockSoundSet instance = new Packet_BlockSoundSet();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlockSoundSet DeserializeBuffer(byte[] buffer, int length, Packet_BlockSoundSet instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlockSoundSet Deserialize(CitoMemoryStream stream, Packet_BlockSoundSet instance)
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
				instance.Walk = ProtocolParser.ReadString(stream);
				break;
			case 18:
				instance.Break = ProtocolParser.ReadString(stream);
				break;
			case 26:
				instance.Place = ProtocolParser.ReadString(stream);
				break;
			case 34:
				instance.Hit = ProtocolParser.ReadString(stream);
				break;
			case 42:
				instance.Inside = ProtocolParser.ReadString(stream);
				break;
			case 50:
				instance.Ambient = ProtocolParser.ReadString(stream);
				break;
			case 72:
				instance.AmbientBlockCount = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.AmbientSoundType = ProtocolParser.ReadUInt32(stream);
				break;
			case 88:
				instance.AmbientMaxDistanceMerge = ProtocolParser.ReadUInt32(stream);
				break;
			case 56:
				instance.ByToolToolAdd(ProtocolParser.ReadUInt32(stream));
				break;
			case 66:
				instance.ByToolSoundAdd(DeserializeLengthDelimitedNew(stream));
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

	public static Packet_BlockSoundSet DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlockSoundSet instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlockSoundSet result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlockSoundSet instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Walk != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Walk));
		}
		if (instance.Break != null)
		{
			stream.WriteByte(18);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Break));
		}
		if (instance.Place != null)
		{
			stream.WriteByte(26);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Place));
		}
		if (instance.Hit != null)
		{
			stream.WriteByte(34);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Hit));
		}
		if (instance.Inside != null)
		{
			stream.WriteByte(42);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Inside));
		}
		if (instance.Ambient != null)
		{
			stream.WriteByte(50);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Ambient));
		}
		if (instance.AmbientBlockCount != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.AmbientBlockCount);
		}
		if (instance.AmbientSoundType != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.AmbientSoundType);
		}
		if (instance.AmbientMaxDistanceMerge != 0)
		{
			stream.WriteByte(88);
			ProtocolParser.WriteUInt32(stream, instance.AmbientMaxDistanceMerge);
		}
		if (instance.ByToolTool != null)
		{
			for (int k = 0; k < instance.ByToolToolCount; k++)
			{
				int i7 = instance.ByToolTool[k];
				stream.WriteByte(56);
				ProtocolParser.WriteUInt32(stream, i7);
			}
		}
		if (instance.ByToolSound != null)
		{
			for (int j = 0; j < instance.ByToolSoundCount; j++)
			{
				Packet_BlockSoundSet i8 = instance.ByToolSound[j];
				stream.WriteByte(66);
				CitoMemoryStream ms8 = new CitoMemoryStream(subBuffer);
				Serialize(ms8, i8);
				int len = ms8.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms8.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_BlockSoundSet instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlockSoundSet instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
