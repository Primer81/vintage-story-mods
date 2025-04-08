public class Packet_CompositeShapeSerializer
{
	private const int field = 8;

	public static Packet_CompositeShape DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_CompositeShape instance = new Packet_CompositeShape();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_CompositeShape DeserializeBuffer(byte[] buffer, int length, Packet_CompositeShape instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_CompositeShape Deserialize(CitoMemoryStream stream, Packet_CompositeShape instance)
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
				instance.Rotatex = ProtocolParser.ReadUInt32(stream);
				break;
			case 24:
				instance.Rotatey = ProtocolParser.ReadUInt32(stream);
				break;
			case 32:
				instance.Rotatez = ProtocolParser.ReadUInt32(stream);
				break;
			case 42:
				instance.AlternatesAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 90:
				instance.OverlaysAdd(DeserializeLengthDelimitedNew(stream));
				break;
			case 48:
				instance.VoxelizeShape = ProtocolParser.ReadUInt32(stream);
				break;
			case 58:
				instance.SelectiveElementsAdd(ProtocolParser.ReadString(stream));
				break;
			case 138:
				instance.IgnoreElementsAdd(ProtocolParser.ReadString(stream));
				break;
			case 64:
				instance.QuantityElements = ProtocolParser.ReadUInt32(stream);
				break;
			case 72:
				instance.QuantityElementsSet = ProtocolParser.ReadUInt32(stream);
				break;
			case 80:
				instance.Format = ProtocolParser.ReadUInt32(stream);
				break;
			case 96:
				instance.Offsetx = ProtocolParser.ReadUInt32(stream);
				break;
			case 104:
				instance.Offsety = ProtocolParser.ReadUInt32(stream);
				break;
			case 112:
				instance.Offsetz = ProtocolParser.ReadUInt32(stream);
				break;
			case 120:
				instance.InsertBakedTextures = ProtocolParser.ReadBool(stream);
				break;
			case 128:
				instance.ScaleAdjust = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_CompositeShape DeserializeLengthDelimited(CitoMemoryStream stream, Packet_CompositeShape instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_CompositeShape result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_CompositeShape instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Base != null)
		{
			stream.WriteByte(10);
			ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(instance.Base));
		}
		if (instance.Rotatex != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.Rotatex);
		}
		if (instance.Rotatey != 0)
		{
			stream.WriteByte(24);
			ProtocolParser.WriteUInt32(stream, instance.Rotatey);
		}
		if (instance.Rotatez != 0)
		{
			stream.WriteByte(32);
			ProtocolParser.WriteUInt32(stream, instance.Rotatez);
		}
		if (instance.Alternates != null)
		{
			for (int m = 0; m < instance.AlternatesCount; m++)
			{
				Packet_CompositeShape i13 = instance.Alternates[m];
				stream.WriteByte(42);
				CitoMemoryStream ms12 = new CitoMemoryStream(subBuffer);
				Serialize(ms12, i13);
				int len2 = ms12.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms12.GetBuffer(), 0, len2);
			}
		}
		if (instance.Overlays != null)
		{
			for (int l = 0; l < instance.OverlaysCount; l++)
			{
				Packet_CompositeShape i11 = instance.Overlays[l];
				stream.WriteByte(90);
				CitoMemoryStream ms11 = new CitoMemoryStream(subBuffer);
				Serialize(ms11, i11);
				int len = ms11.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms11.GetBuffer(), 0, len);
			}
		}
		if (instance.VoxelizeShape != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.VoxelizeShape);
		}
		if (instance.SelectiveElements != null)
		{
			for (int k = 0; k < instance.SelectiveElementsCount; k++)
			{
				string i14 = instance.SelectiveElements[k];
				stream.WriteByte(58);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i14));
			}
		}
		if (instance.IgnoreElements != null)
		{
			for (int j = 0; j < instance.IgnoreElementsCount; j++)
			{
				string i12 = instance.IgnoreElements[j];
				stream.WriteKey(17, 2);
				ProtocolParser.WriteBytes(stream, ProtoPlatform.StringToBytes(i12));
			}
		}
		if (instance.QuantityElements != 0)
		{
			stream.WriteByte(64);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElements);
		}
		if (instance.QuantityElementsSet != 0)
		{
			stream.WriteByte(72);
			ProtocolParser.WriteUInt32(stream, instance.QuantityElementsSet);
		}
		if (instance.Format != 0)
		{
			stream.WriteByte(80);
			ProtocolParser.WriteUInt32(stream, instance.Format);
		}
		if (instance.Offsetx != 0)
		{
			stream.WriteByte(96);
			ProtocolParser.WriteUInt32(stream, instance.Offsetx);
		}
		if (instance.Offsety != 0)
		{
			stream.WriteByte(104);
			ProtocolParser.WriteUInt32(stream, instance.Offsety);
		}
		if (instance.Offsetz != 0)
		{
			stream.WriteByte(112);
			ProtocolParser.WriteUInt32(stream, instance.Offsetz);
		}
		if (instance.InsertBakedTextures)
		{
			stream.WriteByte(120);
			ProtocolParser.WriteBool(stream, instance.InsertBakedTextures);
		}
		if (instance.ScaleAdjust != 0)
		{
			stream.WriteKey(16, 0);
			ProtocolParser.WriteUInt32(stream, instance.ScaleAdjust);
		}
	}

	public static byte[] SerializeToBytes(Packet_CompositeShape instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_CompositeShape instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
