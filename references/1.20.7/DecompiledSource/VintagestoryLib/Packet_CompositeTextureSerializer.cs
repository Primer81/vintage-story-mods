public class Packet_CompositeTextureSerializer
{
	private const int field = 8;

	public static Packet_CompositeTexture DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeTexture instance = new Packet_CompositeTexture();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CompositeTexture DeserializeBuffer(byte[] buffer, int length, Packet_CompositeTexture instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeTexture Deserialize(CitoMemoryStream stream, Packet_CompositeTexture instance)
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
			case 18:
				instance.OverlaysAdd(Packet_BlendedOverlayTextureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.AlternatesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 32:
				instance.Rotation = ProtocolParser.ReadUInt32(stream);
				break;
			case 40:
				instance.Alpha = ProtocolParser.ReadUInt32(stream);
				break;
			case 50:
				instance.TilesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 56:
				instance.TilesWidth = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CompositeTexture DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeTexture instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CompositeTexture result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeTexture instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Base));
		}
		if (instance.Overlays != null)
		{
			for (int l = 0; l < instance.OverlaysCount; l++)
			{
				Packet_BlendedOverlayTexture i2 = instance.Overlays[l];
				stream.WriteByte(18);
				CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
				Packet_BlendedOverlayTextureSerializer.Serialize(ms2, i2);
				int len3 = ms2.Position();
				ProtocolParser.WriteUInt32_(stream, len3);
				stream.Write(ms2.GetBuffer(), 0, len3);
			}
		}
		if (instance.Alternates != null)
		{
			for (int k = 0; k < instance.AlternatesCount; k++)
			{
				Packet_CompositeTexture i3 = instance.Alternates[k];
				stream.WriteByte(26);
				CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
				Serialize(ms3, i3);
				int len2 = ms3.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms3.GetBuffer(), 0, len2);
			}
		}
		if (instance.Rotation != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotation);
		}
		if (instance.Alpha != 0)
		{
			stream.WriteByte(40);
			ProtocolParser.WriteUInt32(stream, instance.Alpha);
		}
		if (instance.Tiles != null)
		{
			for (int j = 0; j < instance.TilesCount; j++)
			{
				Packet_CompositeTexture i4 = instance.Tiles[j];
				stream.WriteByte(50);
				CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
				Serialize(ms4, i4);
				int len = ms4.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms4.GetBuffer(), 0, len);
			}
		}
		if (instance.TilesWidth != 0)
		{
			stream.WriteByte(56);
			ProtocolParser.WriteUInt32(stream, instance.TilesWidth);
		}
	}

	public static byte[] SerializeToBytes(Packet_CompositeTexture instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeTexture instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
