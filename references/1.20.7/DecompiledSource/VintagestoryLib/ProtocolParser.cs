using System.IO;

public class ProtocolParser
{
	private const int byteHighestBit = 128;

	private const int BitMaskLogicalRightShiftBy7 = 33554431;

	private const long BitMaskLogicalRightShiftBy7L = 144115188075855871L;

	private const int BitMask14bits = -16384;

	public static string ReadString(CitoStream stream)
	{
		return ProtoPlatform.BytesToString(ReadBytes(stream), 0);
	}

	public static byte[] ReadBytes(CitoStream stream)
	{
		int length = ReadUInt32(stream);
		byte[] buffer = new byte[length];
		int r;
		for (int read = 0; read < length; read += r)
		{
			r = stream.Read(buffer, read, length - read);
			if (r == 0)
			{
				throw new InvalidDataException("Expected " + (length - read) + " got " + read);
			}
		}
		return buffer;
	}

	public static void SkipBytes(CitoStream stream)
	{
		int length = ReadUInt32(stream);
		if (stream.CanSeek())
		{
			stream.Seek(length, CitoSeekOrigin.Current);
		}
		else
		{
			ReadBytes(stream);
		}
	}

	public static void WriteString(CitoStream stream, string val)
	{
		WriteBytes(stream, ProtoPlatform.StringToBytes(val));
	}

	public static void WriteBytes(CitoStream stream, byte[] val)
	{
		WriteUInt32_(stream, val.Length);
		stream.Write(val, 0, val.Length);
	}

	public static Key ReadKey_(byte firstByte, CitoStream stream)
	{
		if (firstByte < 128)
		{
			return Key.Create(firstByte);
		}
		return Key.Create(firstByte, ReadUInt32(stream));
	}

	public static int ReadKeyAsInt(int firstByte, CitoStream stream)
	{
		int secondByte = stream.ReadByte();
		return (firstByte & 0x7F) | (secondByte << 7);
	}

	public static void WriteKey(CitoStream stream, Key key)
	{
		WriteUInt32_(stream, key);
	}

	public static void SkipKey(CitoStream stream, Key key)
	{
		switch (key.WireType)
		{
		case 5:
			stream.Seek(4, CitoSeekOrigin.Current);
			break;
		case 1:
			stream.Seek(8, CitoSeekOrigin.Current);
			break;
		case 2:
			stream.Seek(ReadUInt32(stream), CitoSeekOrigin.Current);
			break;
		case 0:
			ReadSkipVarInt(stream);
			break;
		default:
			throw new InvalidDataException("Unknown wire type: " + key.WireType + " at stream position " + stream.Position());
		}
	}

	public static byte[] ReadValueBytes(CitoStream stream, Key key)
	{
		int offset = 0;
		switch (key.WireType)
		{
		case 5:
		{
			byte[] b;
			for (b = new byte[4]; offset < 4; offset += stream.Read(b, offset, 4 - offset))
			{
			}
			return b;
		}
		case 1:
		{
			byte[] b;
			for (b = new byte[8]; offset < 8; offset += stream.Read(b, offset, 8 - offset))
			{
			}
			return b;
		}
		case 2:
		{
			int length = ReadUInt32(stream);
			CitoMemoryStream ms = new CitoMemoryStream();
			WriteUInt32(ms, length);
			offset = ms.Position();
			int bLength = length + offset;
			byte[] b = new byte[bLength];
			for (int i = 0; i < offset; i++)
			{
				b[i] = ms.ToArray()[i];
			}
			for (; offset < bLength; offset += stream.Read(b, offset, bLength - offset))
			{
			}
			return b;
		}
		case 0:
			return ReadVarIntBytes(stream);
		default:
			throw new InvalidDataException("Unknown wire type: " + key.WireType + " at stream position " + stream.Position());
		}
	}

	public static void ReadSkipVarInt(CitoStream stream)
	{
		int num;
		do
		{
			num = stream.ReadByte();
			if (num < 0)
			{
				throw new IOException("Stream ended too early");
			}
		}
		while (((uint)num & 0x80u) != 0);
	}

	public static byte[] ReadVarIntBytes(CitoStream stream)
	{
		byte[] buffer = new byte[10];
		int offset = 0;
		while (true)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				throw new IOException("Stream ended too early");
			}
			buffer[offset] = (byte)b;
			offset++;
			if ((b & 0x80) == 0)
			{
				break;
			}
			if (offset >= buffer.Length)
			{
				throw new InvalidDataException("VarInt too long, more than 10 bytes");
			}
		}
		byte[] ret = new byte[offset];
		for (int i = 0; i < offset; i++)
		{
			ret[i] = buffer[i];
		}
		return ret;
	}

	public static int ReadInt32(CitoStream stream)
	{
		return ReadUInt32(stream);
	}

	public static void WriteInt32(CitoStream stream, int val)
	{
		WriteUInt32(stream, val);
	}

	public static int ReadZInt32(CitoStream stream)
	{
		int val = ReadUInt32(stream);
		return (val >> 1) ^ (val << 31 >> 31);
	}

	public static void WriteZInt32(CitoStream stream, int val)
	{
		WriteUInt32_(stream, (val << 1) ^ (val >> 31));
	}

	public static long ReadInt64(CitoStream stream)
	{
		return ReadUInt64(stream);
	}

	public static void WriteInt64(CitoStream stream, long val)
	{
		WriteUInt64(stream, val);
	}

	public static long ReadZInt64(CitoStream stream)
	{
		long val = ReadUInt64(stream);
		return (val >> 1) ^ (val << 63 >> 63);
	}

	public static void WriteZInt64(CitoStream stream, long val)
	{
		WriteUInt64(stream, (val << 1) ^ (val >> 63));
	}

	public static int ReadUInt32(CitoStream stream)
	{
		int val = 0;
		for (int i = 0; i < 5; i++)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 4 && b > 15)
			{
				throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
			}
			if ((b & 0x80) == 0)
			{
				return val | (b << 7 * i);
			}
			val |= (b & 0x7F) << 7 * i;
		}
		throw new InvalidDataException("Got larger VarInt than 32 bit unsigned");
	}

	public static void WriteUInt32(CitoStream stream, int val)
	{
		if ((val & -16384) == 0)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)((uint)val & 0x7Fu);
			val = (val >> 7) & 0x1FFFFFF;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static void WriteUInt32_(CitoStream stream, int val)
	{
		if (val <= 16383)
		{
			stream.WriteSmallInt(val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)((uint)val & 0x7Fu);
			val >>= 7;
			if (val == 0)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static long ReadUInt64(CitoStream stream)
	{
		long val = 0L;
		for (int i = 0; i < 10; i++)
		{
			int b = stream.ReadByte();
			if (b < 0)
			{
				throw new IOException("Stream ended too early");
			}
			if (i == 9 && b > 1)
			{
				throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
			}
			if ((b & 0x80) == 0)
			{
				return val | ((long)b << 7 * i);
			}
			val |= (long)(((ulong)b & 0x7FuL) << 7 * i);
		}
		throw new InvalidDataException("Got larger VarInt than 64 bit unsigned");
	}

	public static void WriteUInt64(CitoStream stream, long val)
	{
		if ((val & -16384) == 0L)
		{
			stream.WriteSmallInt((int)val);
			return;
		}
		byte b;
		while (true)
		{
			b = (byte)(val & 0x7F);
			val = (val >> 7) & 0x1FFFFFFFFFFFFFFL;
			if (val == 0L)
			{
				break;
			}
			stream.WriteByte((byte)(b + 128));
		}
		stream.WriteByte(b);
	}

	public static bool ReadBool(CitoStream stream)
	{
		int b = stream.ReadByte();
		if (b == 1)
		{
			return true;
		}
		if (b == 0)
		{
			return false;
		}
		if (b < 0)
		{
			throw new IOException("Stream ended too early");
		}
		throw new InvalidDataException("Invalid boolean value");
	}

	public static void WriteBool(CitoStream stream, bool val)
	{
		byte ret = 0;
		if (val)
		{
			ret = 1;
		}
		stream.WriteByte(ret);
	}

	public static int PeekPacketId(byte[] data)
	{
		if (data.Length == 0)
		{
			return -1;
		}
		int keyInt = data[0];
		if (keyInt >= 128)
		{
			if (data.Length == 1)
			{
				return -1;
			}
			int secondByte = data[1];
			if (secondByte >= 128)
			{
				return -1;
			}
			keyInt = (keyInt & 0x7F) | (secondByte << 7);
		}
		if (!Wire.IsValid(keyInt % 8))
		{
			return -1;
		}
		return keyInt;
	}
}
