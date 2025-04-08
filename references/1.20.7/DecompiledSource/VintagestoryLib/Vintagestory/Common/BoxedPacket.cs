using System;

namespace Vintagestory.Common;

public class BoxedPacket : BoxedArray
{
	public int Length;

	public int LengthSent;

	internal int Serialize(IPacket p)
	{
		CitoMemoryStream ms = new CitoMemoryStream(this);
		p.SerializeTo(ms);
		return Length = ms.Position();
	}

	internal void Dispose()
	{
		buffer = null;
		Length = 0;
		LengthSent = 0;
	}

	internal byte[] Clone(int destOffset)
	{
		int len = Length;
		byte[] dest = new byte[len + destOffset];
		if (len > 256)
		{
			Array.Copy(buffer, 0, dest, destOffset, len);
		}
		else
		{
			int fastLoopLength = len - len % 4;
			int i;
			for (i = 0; i < fastLoopLength; i += 4)
			{
				dest[destOffset] = buffer[i];
				dest[destOffset + 1] = buffer[i + 1];
				dest[destOffset + 2] = buffer[i + 2];
				dest[destOffset + 3] = buffer[i + 3];
				destOffset += 4;
			}
			for (; i < len; i++)
			{
				dest[destOffset++] = buffer[i];
			}
		}
		return dest;
	}
}
