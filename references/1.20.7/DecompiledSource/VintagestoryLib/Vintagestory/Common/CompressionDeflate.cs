using System;
using System.IO;
using System.IO.Compression;

namespace Vintagestory.Common;

public class CompressionDeflate : ICompression
{
	private const int SIZE = 4096;

	[ThreadStatic]
	private static byte[] buffer;

	public byte[] Compress(MemoryStream input)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream output = new MemoryStream();
		input.Position = 0L;
		using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
		{
			int numRead;
			while ((numRead = input.Read(buffer, 0, 4096)) != 0)
			{
				compress.Write(buffer, 0, numRead);
			}
		}
		return output.ToArray();
	}

	public byte[] Compress(byte[] data)
	{
		int len = data.Length;
		MemoryStream output = new MemoryStream();
		using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
		{
			int i = 0;
			for (int penultimate = len - 4096; i < penultimate; i += 4096)
			{
				compress.Write(data, i, 4096);
			}
			compress.Write(data, i, len - i);
		}
		return output.ToArray();
	}

	public byte[] Compress(byte[] data, int len, int reserveOffset)
	{
		MemoryStream output = new MemoryStream((len / 2048 + 1) * 256);
		for (int j = 0; j < reserveOffset; j++)
		{
			output.WriteByte(0);
		}
		using (DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest))
		{
			int i = 0;
			for (int penultimate = len - 4096; i < penultimate; i += 4096)
			{
				compress.Write(data, i, 4096);
			}
			compress.Write(data, i, len - i);
		}
		return output.ToArray();
	}

	public unsafe byte[] Compress(ushort[] ushortdata)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream output = new MemoryStream();
		fixed (byte* bBuffer = buffer)
		{
			ushort* pBuffer = (ushort*)bBuffer;
			using DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest);
			int inpos = 0;
			int outpos = 0;
			while (inpos < ushortdata.Length)
			{
				pBuffer[outpos++] = ushortdata[inpos++];
				if (outpos == 2048 || inpos == ushortdata.Length)
				{
					compress.Write(buffer, 0, outpos * 2);
					outpos = 0;
				}
			}
		}
		return output.ToArray();
	}

	public unsafe byte[] Compress(int[] intdata, int length)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream output = new MemoryStream();
		fixed (byte* bBuffer = buffer)
		{
			int* pBuffer = (int*)bBuffer;
			using DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest);
			int inpos = 0;
			int outpos = 0;
			while (inpos < length)
			{
				pBuffer[outpos++] = intdata[inpos++];
				if (outpos == 1024 || inpos == length)
				{
					compress.Write(buffer, 0, outpos * 4);
					outpos = 0;
				}
			}
		}
		return output.ToArray();
	}

	public unsafe byte[] Compress(uint[] uintdata, int length)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream output = new MemoryStream();
		fixed (byte* bBuffer = buffer)
		{
			uint* pBuffer = (uint*)bBuffer;
			using DeflateStream compress = new DeflateStream(output, CompressionLevel.Fastest);
			int inpos = 0;
			int outpos = 0;
			while (inpos < length)
			{
				pBuffer[outpos++] = uintdata[inpos++];
				if (outpos == 1024 || inpos == length)
				{
					compress.Write(buffer, 0, outpos * 4);
					outpos = 0;
				}
			}
		}
		return output.ToArray();
	}

	public byte[] Decompress(byte[] fi)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream ms = new MemoryStream();
		using (MemoryStream inFile = new MemoryStream(fi))
		{
			using DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress);
			int numRead;
			while ((numRead = Decompress.Read(buffer, 0, 4096)) != 0)
			{
				ms.Write(buffer, 0, numRead);
			}
		}
		return ms.ToArray();
	}

	public void Decompress(byte[] fi, byte[] dest)
	{
		using MemoryStream inFile = new MemoryStream(fi);
		using DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress);
		int i = 0;
		int penultimate = dest.Length - 4096;
		int numRead;
		while ((numRead = Decompress.Read(dest, i, 4096)) != 0)
		{
			if ((i += numRead) > penultimate)
			{
				if (i < dest.Length)
				{
					Decompress.Read(dest, i, dest.Length - i);
				}
				break;
			}
		}
	}

	public byte[] Decompress(byte[] fi, int offset, int length)
	{
		if (buffer == null)
		{
			buffer = new byte[4096];
		}
		MemoryStream ms = new MemoryStream();
		using (MemoryStream inFile = new MemoryStream(fi, offset, length))
		{
			using DeflateStream Decompress = new DeflateStream(inFile, CompressionMode.Decompress);
			int numRead;
			while ((numRead = Decompress.Read(buffer, 0, 4096)) != 0)
			{
				ms.Write(buffer, 0, numRead);
			}
		}
		return ms.ToArray();
	}

	public int DecompressAndSize(byte[] fi, out byte[] buffer)
	{
		buffer = Decompress(fi);
		return buffer.Length;
	}

	public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
	{
		buffer = Decompress(compressedData, offset, length);
		return buffer.Length;
	}
}
