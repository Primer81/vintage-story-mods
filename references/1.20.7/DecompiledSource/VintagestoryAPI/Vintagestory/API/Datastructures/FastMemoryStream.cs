using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

public class FastMemoryStream : Stream
{
	private byte[] buffer;

	private int bufferlength;

	private const int MaxLength = 2147483616;

	/// <summary>
	/// When serializing to a buffer, indicates the count of bytes written so far
	/// </summary>
	public override long Position { get; set; }

	/// <summary>
	/// When deserializing from a buffer, this is the full buffer length
	/// </summary>
	public override long Length => bufferlength;

	public override bool CanSeek => false;

	public override bool CanRead => true;

	public override bool CanWrite => true;

	public FastMemoryStream()
	{
		bufferlength = 1024;
		buffer = new byte[1024];
		Position = 0L;
	}

	public FastMemoryStream(int capacity)
	{
		bufferlength = capacity;
		buffer = new byte[capacity];
		Position = 0L;
	}

	public FastMemoryStream(byte[] buffer, int length)
	{
		bufferlength = length;
		this.buffer = buffer;
		Position = 0L;
	}

	public override void SetLength(long value)
	{
		if (value > 2147483616)
		{
			throw new IndexOutOfRangeException("FastMemoryStream limited to 2GB in size");
		}
		bufferlength = Math.Min((int)value, buffer.Length);
	}

	public byte[] ToArray()
	{
		return FastCopy(buffer, bufferlength, (int)Position);
	}

	public byte[] GetBuffer()
	{
		return buffer;
	}

	public override int Read(byte[] destBuffer, int offset, int count)
	{
		long origPosition = Position;
		long bufferlength = this.bufferlength;
		byte[] streamBuffer = buffer;
		for (int i = 0; i < count; i++)
		{
			if (origPosition + i >= bufferlength)
			{
				Position += i;
				return i;
			}
			destBuffer[offset + i] = streamBuffer[origPosition + i];
		}
		Position += count;
		return count;
	}

	public override void Write(byte[] srcBuffer, int srcOffset, int count)
	{
		if (count <= 0)
		{
			return;
		}
		CheckCapacity(count);
		if (count < 128)
		{
			byte[] buffer = this.buffer;
			uint pos = (uint)Position;
			uint i = (uint)srcOffset;
			uint srcLimit = (uint)(srcOffset + count);
			while (i < srcLimit)
			{
				buffer[pos++] = srcBuffer[i++];
			}
		}
		else
		{
			Array.Copy(srcBuffer, srcOffset, this.buffer, (int)Position, count);
		}
		Position += count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckCapacity(int count)
	{
		if (Position + count <= bufferlength)
		{
			return;
		}
		int newSize = ((bufferlength <= 1073741808) ? (bufferlength * 2) : (Math.Min(bufferlength, 1879048164) + 268435452));
		if (Position + count > newSize)
		{
			if (Position + count > 2147483616)
			{
				throw new IndexOutOfRangeException("FastMemoryStream limited to 2GB in size");
			}
			newSize = (int)Position + count;
		}
		buffer = FastCopy(buffer, (int)Position, newSize);
		bufferlength = newSize;
	}

	public override int ReadByte()
	{
		if (Position >= bufferlength)
		{
			return -1;
		}
		return buffer[Position++];
	}

	public override void WriteByte(byte p)
	{
		CheckCapacity(1);
		buffer[Position++] = p;
	}

	public override void Write(ReadOnlySpan<byte> inputBuffer)
	{
		int length = inputBuffer.Length;
		CheckCapacity(length);
		Span<byte> streamBuffer = new Span<byte>(buffer, (int)Position, length);
		inputBuffer.CopyTo(streamBuffer);
		Position += length;
	}

	private static byte[] FastCopy(byte[] buffer, int oldLength, int newSize)
	{
		if (newSize < oldLength)
		{
			oldLength = newSize;
		}
		byte[] bufferCopy = new byte[newSize];
		if (oldLength >= 128)
		{
			Array.Copy(buffer, 0, bufferCopy, 0, oldLength);
		}
		else
		{
			uint i = 0u;
			if (oldLength > 15)
			{
				for (uint srcLimit = (uint)(oldLength - 3); i < buffer.Length && i < srcLimit; i += 4)
				{
					bufferCopy[i] = buffer[i];
					bufferCopy[i + 1] = buffer[i + 1];
					bufferCopy[i + 2] = buffer[i + 2];
					bufferCopy[i + 3] = buffer[i + 3];
				}
			}
			for (; i < oldLength; i++)
			{
				bufferCopy[i] = buffer[i];
			}
		}
		return bufferCopy;
	}

	public override void Flush()
	{
	}

	public void Reset()
	{
		Position = 0L;
		bufferlength = buffer.Length;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return -1L;
	}

	public void RemoveFromStart(int newStart)
	{
		int oldLength = (int)Position;
		int newLength = oldLength - newStart;
		if (newLength >= 128)
		{
			Array.Copy(this.buffer, newStart, this.buffer, 0, newLength);
		}
		else
		{
			byte[] buffer = this.buffer;
			uint i = (uint)newStart;
			uint j = 0u;
			if (newLength > 15)
			{
				for (uint srcLimit = (uint)(oldLength - 3); i < buffer.Length && i < srcLimit; i += 4)
				{
					buffer[j] = buffer[i];
					buffer[j + 1] = buffer[i + 1];
					buffer[j + 2] = buffer[i + 2];
					buffer[j + 3] = buffer[i + 3];
					j += 4;
				}
			}
			while (i < oldLength)
			{
				buffer[j++] = buffer[i++];
			}
		}
		Position -= newStart;
	}
}
