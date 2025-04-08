using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common;

public class ZStdWrapper
{
	public static IZStdCompressor ConstructCompressor(int compressionLevel)
	{
		return new ZStdCompressorImpl(compressionLevel);
	}

	public static IZStdDecompressor CreateDecompressor()
	{
		return new ZStdDecompressorImpl();
	}

	public unsafe static ulong GetDecompressedSize(ReadOnlySpan<byte> compressedFrame)
	{
		fixed (byte* src = compressedFrame)
		{
			return ZstdNative.ZSTD_getFrameContentSize(src, (nuint)compressedFrame.Length);
		}
	}
}
