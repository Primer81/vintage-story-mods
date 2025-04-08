using System;
using Vintagestory.API.Common;

public class CitoMemoryStream : CitoStream
{
	private const int byteHighestBit = 128;

	private byte[] buffer_;

	private int bufferlength;

	private int position_;

	private readonly BoxedArray ba;

	public CitoMemoryStream()
	{
		bufferlength = 16;
		buffer_ = new byte[16];
		position_ = 0;
	}

	public CitoMemoryStream(BoxedArray reusableBuffer)
	{
		ba = reusableBuffer.CheckCreated();
		bufferlength = ba.buffer.Length;
		buffer_ = ba.buffer;
		position_ = 0;
	}

	public CitoMemoryStream(byte[] buffer, int length)
	{
		bufferlength = length;
		buffer_ = buffer;
		position_ = 0;
	}

	public override int Position()
	{
		return position_;
	}

	internal int GetLength()
	{
		return bufferlength;
	}

	internal void SetLength(int value)
	{
		bufferlength = Math.Min(value, buffer_.Length);
	}

	public byte[] ToArray()
	{
		return buffer_;
	}

	public byte[] GetBuffer()
	{
		return buffer_;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int ourOffset = position_;
		int maxCount = bufferlength - ourOffset;
		if (count > maxCount)
		{
			count = maxCount;
		}
		byte[] ourBuffer = buffer_;
		for (int i = 0; i < count; i++)
		{
			buffer[offset + i] = ourBuffer[ourOffset + i];
		}
		position_ = ourOffset + count;
		return count;
	}

	public override bool CanSeek()
	{
		return false;
	}

	public override void Seek(int length, CitoSeekOrigin seekOrigin)
	{
		if (seekOrigin == CitoSeekOrigin.Current)
		{
			position_ += length;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		if (position_ + count - 1 >= bufferlength)
		{
			int newSize = bufferlength * 2;
			if (newSize < position_ + count)
			{
				newSize = position_ + count;
			}
			buffer_ = FastCopy(buffer_, position_, newSize);
			bufferlength = newSize;
		}
		if (count > 200)
		{
			Array.Copy(buffer, offset, buffer_, position_, count);
			position_ += count;
			return;
		}
		int i = offset;
		count += offset;
		int dWordBoundary = (i + 3) / 4 * 4;
		byte[] ourBuffer = buffer_;
		int ourPosition = position_;
		while (i < dWordBoundary)
		{
			ourBuffer[ourPosition++] = buffer[i++];
		}
		int fastLoopLimit = count / 4 * 4;
		for (i += 3; i < buffer.Length && i < fastLoopLimit; i += 4)
		{
			ourBuffer[ourPosition] = buffer[i - 3];
			ourBuffer[ourPosition + 1] = buffer[i - 2];
			ourBuffer[ourPosition + 2] = buffer[i - 1];
			ourBuffer[ourPosition + 3] = buffer[i];
			ourPosition += 4;
		}
		i -= 3;
		while (i < count)
		{
			ourBuffer[ourPosition++] = buffer[i++];
		}
		position_ = ourPosition;
	}

	public override void Seek_(int p, CitoSeekOrigin seekOrigin)
	{
	}

	public override int ReadByte()
	{
		if (position_ >= bufferlength)
		{
			return -1;
		}
		return buffer_[position_++];
	}

	public override void WriteByte(byte p)
	{
		if (position_ >= bufferlength)
		{
			buffer_ = FastCopy(buffer_, position_, bufferlength *= 2);
		}
		buffer_[position_++] = p;
	}

	public override void WriteSmallInt(int v)
	{
		if (v < 128)
		{
			WriteByte((byte)v);
			return;
		}
		if (position_ >= bufferlength - 1)
		{
			buffer_ = FastCopy(buffer_, position_, bufferlength *= 2);
		}
		buffer_[position_++] = (byte)((uint)v | 0x80u);
		buffer_[position_++] = (byte)(v >> 7);
	}

	public override void WriteKey(byte field, byte wiretype)
	{
		WriteSmallInt(new Key(field, wiretype));
	}

	private byte[] FastCopy(byte[] buffer, int oldLength, int newSize)
	{
		byte[] buffer2 = new byte[newSize];
		if (oldLength > 256)
		{
			Array.Copy(buffer, 0, buffer2, 0, oldLength);
		}
		else
		{
			int i = 0;
			if (oldLength >= 4)
			{
				int fastLoopLength = oldLength / 4 * 4;
				for (i = 3; i < buffer.Length && i < fastLoopLength; i += 4)
				{
					buffer2[i - 3] = buffer[i - 3];
					buffer2[i - 2] = buffer[i - 2];
					buffer2[i - 1] = buffer[i - 1];
					buffer2[i] = buffer[i];
				}
				i -= 3;
			}
			for (; i < oldLength; i++)
			{
				buffer2[i] = buffer[i];
			}
		}
		if (ba != null)
		{
			ba.buffer = buffer2;
		}
		return buffer2;
	}

	public static void NetworkTest(ILogger Logger)
	{
		int a = int.MaxValue;
		int b = int.MinValue;
		int c = -1;
		uint d = uint.MaxValue;
		long e = long.MaxValue;
		long f = long.MinValue;
		long g = -1L;
		ulong h = ulong.MaxValue;
		CitoMemoryStream ms = new CitoMemoryStream();
		ProtocolParser.WriteUInt32(ms, a);
		ProtocolParser.WriteUInt32(ms, b);
		ProtocolParser.WriteUInt32(ms, c);
		ProtocolParser.WriteUInt32(ms, (int)d);
		ProtocolParser.WriteUInt64(ms, e);
		ProtocolParser.WriteUInt64(ms, f);
		ProtocolParser.WriteUInt64(ms, g);
		ProtocolParser.WriteUInt64(ms, (long)h);
		ms.position_ = 0;
		Logger.Notification("Test positive int.   Wrote " + a + "  Read " + ProtocolParser.ReadUInt32(ms));
		Logger.Notification("Test negative int.   Wrote " + b + "  Read " + ProtocolParser.ReadUInt32(ms));
		Logger.Notification("Test negative int.   Wrote " + c + "  Read " + ProtocolParser.ReadUInt32(ms));
		Logger.Notification("Test unsigned uint.  Wrote " + d + "  Read " + (uint)ProtocolParser.ReadUInt32(ms));
		Logger.Notification("Test positive long.  Wrote " + e + "  Read " + ProtocolParser.ReadUInt64(ms));
		Logger.Notification("Test negative long.  Wrote " + f + "  Read " + ProtocolParser.ReadUInt64(ms));
		Logger.Notification("Test negative long.  Wrote " + g + "  Read " + ProtocolParser.ReadUInt64(ms));
		Logger.Notification("Test unsigned ulong.  Wrote " + h + "  Read " + (ulong)ProtocolParser.ReadUInt64(ms));
	}
}
