public class Packet_ServerAssetsSerializer
{
	private const int field = 8;

	public static Packet_ServerAssets DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_ServerAssets instance = new Packet_ServerAssets();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_ServerAssets DeserializeBuffer(byte[] buffer, int length, Packet_ServerAssets instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_ServerAssets Deserialize(CitoMemoryStream stream, Packet_ServerAssets instance)
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
				instance.BlocksAdd(Packet_BlockTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 18:
				instance.ItemsAdd(Packet_ItemTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 26:
				instance.EntitiesAdd(Packet_EntityTypeSerializer.DeserializeLengthDelimitedNew(stream));
				break;
			case 34:
				instance.RecipesAdd(Packet_RecipesSerializer.DeserializeLengthDelimitedNew(stream));
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

	public static Packet_ServerAssets DeserializeLengthDelimited(CitoMemoryStream stream, Packet_ServerAssets instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_ServerAssets result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_ServerAssets instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		if (instance.Blocks != null)
		{
			for (int m = 0; m < instance.BlocksCount; m++)
			{
				Packet_BlockType i1 = instance.Blocks[m];
				stream.WriteByte(10);
				CitoMemoryStream ms1 = new CitoMemoryStream(subBuffer);
				Packet_BlockTypeSerializer.Serialize(ms1, i1);
				int len4 = ms1.Position();
				ProtocolParser.WriteUInt32_(stream, len4);
				stream.Write(ms1.GetBuffer(), 0, len4);
			}
		}
		if (instance.Items != null)
		{
			for (int l = 0; l < instance.ItemsCount; l++)
			{
				Packet_ItemType i2 = instance.Items[l];
				stream.WriteByte(18);
				CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
				Packet_ItemTypeSerializer.Serialize(ms2, i2);
				int len3 = ms2.Position();
				ProtocolParser.WriteUInt32_(stream, len3);
				stream.Write(ms2.GetBuffer(), 0, len3);
			}
		}
		if (instance.Entities != null)
		{
			for (int k = 0; k < instance.EntitiesCount; k++)
			{
				Packet_EntityType i3 = instance.Entities[k];
				stream.WriteByte(26);
				CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
				Packet_EntityTypeSerializer.Serialize(ms3, i3);
				int len2 = ms3.Position();
				ProtocolParser.WriteUInt32_(stream, len2);
				stream.Write(ms3.GetBuffer(), 0, len2);
			}
		}
		if (instance.Recipes != null)
		{
			for (int j = 0; j < instance.RecipesCount; j++)
			{
				Packet_Recipes i4 = instance.Recipes[j];
				stream.WriteByte(34);
				CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
				Packet_RecipesSerializer.Serialize(ms4, i4);
				int len = ms4.Position();
				ProtocolParser.WriteUInt32_(stream, len);
				stream.Write(ms4.GetBuffer(), 0, len);
			}
		}
	}

	public static byte[] SerializeToBytes(Packet_ServerAssets instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_ServerAssets instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
