using System;

namespace Vintagestory.Common;

public interface IZStdCompressor
{
	int Compress(byte[] buffer, byte[] data);

	int Compress(byte[] buffer, ReadOnlySpan<byte> src);
}
