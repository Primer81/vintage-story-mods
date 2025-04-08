public class Packet_MapRegionSerializer
{
	private const int field = 8;

	public static Packet_MapRegion DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_MapRegion instance = new Packet_MapRegion();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_MapRegion DeserializeBuffer(byte[] buffer, int length, Packet_MapRegion instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_MapRegion Deserialize(CitoMemoryStream stream, Packet_MapRegion instance)
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
				instance.RegionX = ProtocolParser.ReadUInt32(stream);
				break;
			case 16:
				instance.RegionZ = ProtocolParser.ReadUInt32(stream);
				break;
			case 26:
				if (instance.LandformMap == null)
				{
					instance.LandformMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.LandformMap);
				}
				break;
			case 34:
				if (instance.ForestMap == null)
				{
					instance.ForestMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.ForestMap);
				}
				break;
			case 42:
				if (instance.ClimateMap == null)
				{
					instance.ClimateMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.ClimateMap);
				}
				break;
			case 50:
				if (instance.GeologicProvinceMap == null)
				{
					instance.GeologicProvinceMap = Packet_IntMapSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_IntMapSerializer.DeserializeLengthDelimited(stream, instance.GeologicProvinceMap);
				}
				break;
			case 58:
				instance.GeneratedStructuresAdd(Packet_GeneratedStructureSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 66:
				instance.Moddata = ProtocolParser.ReadBytes(stream);
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

	public static Packet_MapRegion DeserializeLengthDelimited(CitoMemoryStream stream, Packet_MapRegion instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_MapRegion result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_MapRegion instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.RegionX != 0)
		{
			stream.WriteByte(8);
			ProtocolParser.WriteUInt32(stream, instance.RegionX);
		}
		if (instance.RegionZ != 0)
		{
			stream.WriteByte(16);
			ProtocolParser.WriteUInt32(stream, instance.RegionZ);
		}
		if (instance.LandformMap != null)
		{
			stream.WriteByte(26);
			CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
			Packet_IntMapSerializer.Serialize(ms3, instance.LandformMap);
			int len5 = ms3.Position();
			ProtocolParser.WriteUInt32_(stream, len5);
			stream.Write(ms3.GetBuffer(), 0, len5);
		}
		if (instance.ForestMap != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
			Packet_IntMapSerializer.Serialize(ms4, instance.ForestMap);
			int len4 = ms4.Position();
			ProtocolParser.WriteUInt32_(stream, len4);
			stream.Write(ms4.GetBuffer(), 0, len4);
		}
		if (instance.ClimateMap != null)
		{
			stream.WriteByte(42);
			CitoMemoryStream ms5 = new CitoMemoryStream(subBuffer);
			Packet_IntMapSerializer.Serialize(ms5, instance.ClimateMap);
			int len3 = ms5.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms5.GetBuffer(), 0, len3);
		}
		if (instance.GeologicProvinceMap != null)
		{
			stream.WriteByte(50);
			CitoMemoryStream ms6 = new CitoMemoryStream(subBuffer);
			Packet_IntMapSerializer.Serialize(ms6, instance.GeologicProvinceMap);
			int len2 = ms6.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms6.GetBuffer(), 0, len2);
		}
		if (instance.GeneratedStructures != null)
		{
			for (int j = 0; j < instance.GeneratedStructuresCount; j++)
			{
				Packet_GeneratedStructure i7 = instance.GeneratedStructures[j];
				stream.WriteByte(58);
				CitoMemoryStream ms7 = new CitoMemoryStream(subBuffer);
				Packet_GeneratedStructureSerializer.Serialize(ms7, i7);
				int len = ms7.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms7.GetBuffer(), 0, len);
			}
		}
		if (instance.Moddata != null)
		{
			stream.WriteByte(66);
			ProtocolParser.WriteBytes(stream, instance.Moddata);
		}
	}

	public static byte[] SerializeToBytes(Packet_MapRegion instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_MapRegion instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
