using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common;

public class CompressionZSTD : ICompression
{
	public const int ZSTDCompressionLevel = -3;

	private const int MaxUnusedLargeBufferCount = 100;

	private const int LargeBufferResetInterval = 960000;

	private const int LargeBufferSize = 524288;

	private const int LargeBufferHeadroom = 32768;

	[ThreadStatic]
	private static IZStdCompressor reusableCompressor;

	[ThreadStatic]
	private static IZStdDecompressor reusableDecompressor;

	[ThreadStatic]
	private static byte[] reusableBuffer;

	[ThreadStatic]
	internal static byte[] reusableBuffer2;

	[ThreadStatic]
	private static int largebufferUnusedCounter1;

	[ThreadStatic]
	private static int largebufferUnusedCounter2;

	[ThreadStatic]
	private static int largebufferMaxUsed1;

	[ThreadStatic]
	private static int largebufferMaxUsed2;

	[ThreadStatic]
	private static long largebufferLastReductionTime1;

	[ThreadStatic]
	private static long largebufferLastReductionTime2;

	public byte[] Buffer => reusableBuffer ?? (reusableBuffer = new byte[4096]);

	private IZStdCompressor ConstructCompressor()
	{
		return ZStdWrapper.ConstructCompressor(-3);
	}

	private byte[] GetOrCreateBuffer(int size)
	{
		byte[] buffer = reusableBuffer;
		if (buffer == null || buffer.Length < size)
		{
			buffer = (reusableBuffer = new byte[size]);
			largebufferUnusedCounter1 = 0;
			largebufferMaxUsed1 = 0;
		}
		else if (buffer.Length > 524288)
		{
			if (size > largebufferMaxUsed1)
			{
				largebufferMaxUsed1 = size;
			}
			if (size > buffer.Length * 3 / 4)
			{
				largebufferUnusedCounter1 = 0;
			}
			else if (largebufferUnusedCounter1++ >= 100)
			{
				largebufferUnusedCounter1 = 0;
				if (Environment.TickCount64 > largebufferLastReductionTime1 + 960000)
				{
					largebufferLastReductionTime1 = Environment.TickCount64;
					buffer = (reusableBuffer = new byte[Math.Max(largebufferMaxUsed1 + 32768, 524288)]);
					largebufferMaxUsed1 = 0;
				}
			}
		}
		return buffer;
	}

	private byte[] GetOrCreateBuffer2(int size)
	{
		byte[] buffer = reusableBuffer2;
		if (buffer == null || buffer.Length < size)
		{
			buffer = (reusableBuffer2 = new byte[size]);
			largebufferUnusedCounter2 = 0;
			largebufferMaxUsed2 = 0;
		}
		else if (buffer.Length > 524288)
		{
			if (size > largebufferMaxUsed2)
			{
				largebufferMaxUsed2 = size;
			}
			if (size > buffer.Length * 3 / 4)
			{
				largebufferUnusedCounter2 = 0;
			}
			else if (largebufferUnusedCounter2++ >= 100)
			{
				largebufferUnusedCounter2 = 0;
				if (Environment.TickCount64 > largebufferLastReductionTime2 + 960000)
				{
					largebufferLastReductionTime2 = Environment.TickCount64;
					buffer = (reusableBuffer2 = new byte[Math.Max(largebufferMaxUsed2 + 32768, 524288)]);
					largebufferMaxUsed2 = 0;
				}
			}
		}
		return buffer;
	}

	public byte[] Compress(byte[] data)
	{
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)data.Length);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		int compressedSize = (reusableCompressor ?? (reusableCompressor = ConstructCompressor())).Compress(buffer, data);
		return ArrayConvert.ByteToByte(buffer, compressedSize);
	}

	public byte[] Compress(byte[] data, int length, int reserveOffset)
	{
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)length);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		ReadOnlySpan<byte> input = new ReadOnlySpan<byte>(data, 0, length);
		int compressedSize = (reusableCompressor ?? (reusableCompressor = ConstructCompressor())).Compress(buffer, input);
		return ArrayConvert.ByteToByte(buffer, compressedSize, reserveOffset);
	}

	public unsafe byte[] Compress(ushort[] ushortdata)
	{
		int len = ushortdata.Length * 2;
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)len);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int compressedSize;
		fixed (ushort* pData = ushortdata)
		{
			ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>(pData, len);
			compressedSize = obj.Compress(buffer, bytedata);
		}
		return ArrayConvert.ByteToByte(buffer, compressedSize);
	}

	public unsafe byte[] Compress(int[] intdata, int length)
	{
		int len = length * 4;
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)len);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int compressedSize;
		fixed (int* pData = intdata)
		{
			ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>(pData, len);
			compressedSize = obj.Compress(buffer, bytedata);
		}
		return ArrayConvert.ByteToByte(buffer, compressedSize);
	}

	public unsafe byte[] Compress(uint[] uintdata, int length)
	{
		int len = length * 4;
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)len);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int compressedSize;
		fixed (uint* pData = uintdata)
		{
			ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>(pData, len);
			compressedSize = obj.Compress(buffer, bytedata);
		}
		return ArrayConvert.ByteToByte(buffer, compressedSize);
	}

	internal unsafe int CompressAndSize(int[] intdata, int length)
	{
		int len = length * 4;
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)len);
		byte[] buffer = GetOrCreateBuffer(compressMaxBufferSize);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int result;
		fixed (int* pData = intdata)
		{
			ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>(pData, len);
			result = obj.Compress(buffer, bytedata);
		}
		return result;
	}

	internal unsafe int Compress_To2ndBuffer(int[] intdata, int intLength)
	{
		int len = intLength * 4;
		int compressMaxBufferSize = (int)ZstdNative.ZSTD_compressBound((nuint)len);
		byte[] bufferInts = GetOrCreateBuffer2(compressMaxBufferSize);
		IZStdCompressor obj = reusableCompressor ?? (reusableCompressor = ConstructCompressor());
		int result;
		fixed (int* pData = intdata)
		{
			ReadOnlySpan<byte> bytedata = new ReadOnlySpan<byte>(pData, len);
			result = obj.Compress(bufferInts, bytedata);
		}
		return result;
	}

	public void Decompress(byte[] fi, byte[] dest)
	{
		(reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(dest, fi);
	}

	public byte[] Decompress(byte[] fi)
	{
		byte[] buffer;
		int decompressedSize = DecompressAndSize(fi, out buffer);
		return ArrayConvert.ByteToByte(buffer, decompressedSize);
	}

	public byte[] Decompress(byte[] fi, int offset, int length)
	{
		byte[] buffer;
		int decompressedSize = DecompressAndSize(fi, offset, length, out buffer);
		return ArrayConvert.ByteToByte(buffer, decompressedSize);
	}

	public int DecompressAndSize(byte[] compressedData, out byte[] buffer)
	{
		ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData);
		int decompressBufferSize = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
		buffer = GetOrCreateBuffer(decompressBufferSize);
		return (reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(buffer, compressedFrame);
	}

	public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
	{
		ReadOnlySpan<byte> compressedFrame = new ReadOnlySpan<byte>(compressedData, offset, length);
		int decompressBufferSize = (int)ZStdWrapper.GetDecompressedSize(compressedFrame);
		buffer = GetOrCreateBuffer(decompressBufferSize);
		return (reusableDecompressor ?? (reusableDecompressor = ZStdWrapper.CreateDecompressor())).Decompress(buffer, compressedFrame);
	}
}
