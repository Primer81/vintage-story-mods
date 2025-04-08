using System;

namespace Vintagestory.Common;

public interface IZStdDecompressor
{
	void Decompress(byte[] dest, byte[] src);

	int Decompress(byte[] buffer, ReadOnlySpan<byte> compressedFrame);
}
