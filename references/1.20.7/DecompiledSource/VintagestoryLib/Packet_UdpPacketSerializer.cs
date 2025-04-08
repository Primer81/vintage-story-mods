public class Packet_UdpPacketSerializer
{
	private const int field = 8;

	public static Packet_UdpPacket DeserializeLengthDelimitedNew(CitoMemoryStream stream)
	{
		Packet_UdpPacket instance = new Packet_UdpPacket();
		DeserializeLengthDelimited(stream, instance);
		return instance;
	}

	public static Packet_UdpPacket DeserializeBuffer(byte[] buffer, int length, Packet_UdpPacket instance)
	{
		Deserialize(new CitoMemoryStream(buffer, length), instance);
		return instance;
	}

	public static Packet_UdpPacket Deserialize(CitoMemoryStream stream, Packet_UdpPacket instance)
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
				instance.Id = ProtocolParser.ReadUInt32(stream);
				break;
			case 18:
				if (instance.EntityPosition == null)
				{
					instance.EntityPosition = Packet_EntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_EntityPositionSerializer.DeserializeLengthDelimited(stream, instance.EntityPosition);
				}
				break;
			case 26:
				if (instance.BulkPositions == null)
				{
					instance.BulkPositions = Packet_BulkEntityPositionSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_BulkEntityPositionSerializer.DeserializeLengthDelimited(stream, instance.BulkPositions);
				}
				break;
			case 34:
				if (instance.ChannelPaket == null)
				{
					instance.ChannelPaket = Packet_CustomPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_CustomPacketSerializer.DeserializeLengthDelimited(stream, instance.ChannelPaket);
				}
				break;
			case 42:
				if (instance.ConnectionPacket == null)
				{
					instance.ConnectionPacket = Packet_ConnectionPacketSerializer.DeserializeLengthDelimitedNew(stream);
				}
				else
				{
					Packet_ConnectionPacketSerializer.DeserializeLengthDelimited(stream, instance.ConnectionPacket);
				}
				break;
			case 48:
				instance.Length = ProtocolParser.ReadUInt32(stream);
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

	public static Packet_UdpPacket DeserializeLengthDelimited(CitoMemoryStream stream, Packet_UdpPacket instance)
	{
		int lengthOfPart = ProtocolParser.ReadUInt32(stream);
		int savedLength = stream.GetLength();
		stream.SetLength(stream.Position() + lengthOfPart);
		Packet_UdpPacket result = Deserialize(stream, instance);
		stream.SetLength(savedLength);
		return result;
	}

	public static void Serialize(CitoStream stream, Packet_UdpPacket instance)
	{
		BoxedArray subBuffer = new BoxedArray();
		stream.WriteByte(8);
		ProtocolParser.WriteUInt32(stream, instance.Id);
		if (instance.EntityPosition != null)
		{
			stream.WriteByte(18);
			CitoMemoryStream ms2 = new CitoMemoryStream(subBuffer);
			Packet_EntityPositionSerializer.Serialize(ms2, instance.EntityPosition);
			int len4 = ms2.Position();
			ProtocolParser.WriteUInt32_(stream, len4);
			stream.Write(ms2.GetBuffer(), 0, len4);
		}
		if (instance.BulkPositions != null)
		{
			stream.WriteByte(26);
			CitoMemoryStream ms3 = new CitoMemoryStream(subBuffer);
			Packet_BulkEntityPositionSerializer.Serialize(ms3, instance.BulkPositions);
			int len3 = ms3.Position();
			ProtocolParser.WriteUInt32_(stream, len3);
			stream.Write(ms3.GetBuffer(), 0, len3);
		}
		if (instance.ChannelPaket != null)
		{
			stream.WriteByte(34);
			CitoMemoryStream ms4 = new CitoMemoryStream(subBuffer);
			Packet_CustomPacketSerializer.Serialize(ms4, instance.ChannelPaket);
			int len2 = ms4.Position();
			ProtocolParser.WriteUInt32_(stream, len2);
			stream.Write(ms4.GetBuffer(), 0, len2);
		}
		if (instance.ConnectionPacket != null)
		{
			stream.WriteByte(42);
			CitoMemoryStream ms5 = new CitoMemoryStream(subBuffer);
			Packet_ConnectionPacketSerializer.Serialize(ms5, instance.ConnectionPacket);
			int len = ms5.Position();
			ProtocolParser.WriteUInt32_(stream, len);
			stream.Write(ms5.GetBuffer(), 0, len);
		}
		if (instance.Length != 0)
		{
			stream.WriteByte(48);
			ProtocolParser.WriteUInt32(stream, instance.Length);
		}
	}

	public static byte[] SerializeToBytes(Packet_UdpPacket instance)
	{
		CitoMemoryStream citoMemoryStream = new CitoMemoryStream();
		Serialize(citoMemoryStream, instance);
		return citoMemoryStream.ToArray();
	}

	public static void SerializeLengthDelimited(CitoStream stream, Packet_UdpPacket instance)
	{
		byte[] data = SerializeToBytes(instance);
		ProtocolParser.WriteUInt32_(stream, data.Length);
		stream.Write(data, 0, data.Length);
	}
}
