public class Packet_BlendedOverlayTextureSerializer
{
	private const int field = 8;

	public static Packet_BlendedOverlayTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_BlendedOverlayTexture instance = new Packet_BlendedOverlayTexture();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_BlendedOverlayTexture DeserializeBuffer(byte[] buffer, int length, Packet_BlendedOverlayTexture instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_BlendedOverlayTexture Deserialize(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
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
				instance.Base = ProtocolParser.ReadString(stream);
				break;
			case 16:
				instance.Mode = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_BlendedOverlayTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_BlendedOverlayTexture instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_BlendedOverlayTexture result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Base));
		}
		if (instance.Mode != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Mode);
		}
	}

	public static byte[] SerializeToBytes(Packet_BlendedOverlayTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_BlendedOverlayTexture instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
