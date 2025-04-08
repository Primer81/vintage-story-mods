using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common;

public class ZStdDecompressorImpl : IZStdDecompressor
{
	private unsafe readonly ZstdNative.ZstdDCtx* dCtx;

	public unsafe ZStdDecompressorImpl()
	{
		dCtx = ZstdNative.ZSTD_createDCtx();
	}

	public unsafe void Decompress(byte[] output, byte[] input)
	{
		fixed (byte* pDst = output)
		{
			fixed (byte* pSrc = input)
			{
				ZstdNative.ZSTD_decompressDCtx(dCtx, pDst, (nuint)output.Length, pSrc, (nuint)input.Length);
			}
		}
	}

	public unsafe int Decompress(byte[] output, ReadOnlySpan<byte> input)
	{
		fixed (byte* pDst = output)
		{
			fixed (byte* pSrc = input)
			{
				return (int)ZstdNative.ZSTD_decompressDCtx(dCtx, pDst, (nuint)output.Length, pSrc, (nuint)input.Length);
			}
		}
	}
}
