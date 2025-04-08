using System;
using System.IO;
using System.IO.Compression;

namespace Vintagestory.Common;

public class CompressionGzip : ICompression
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
		using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
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
		using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
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
		using (GZipStream compress = new GZipStream(output, CompressionMode.Compress))
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
		fixed (byte* pBuffer = buffer)
		{
			ushort* pBuffer2 = (ushort*)pBuffer;
			using GZipStream compress = new GZipStream(output, CompressionMode.Compress);
			int inpos = 0;
			int outpos = 0;
			while (inpos < ushortdata.Length)
			{
				pBuffer2[outpos++] = ushortdata[inpos++];
				if (outpos == 2048 || inpos == ushortdata.Length)
				{
					compress.Write(buffer, 0, outpos * 2);
					outpos = 0;
				}
			}
		}
		return output.ToArray();
	}

	public byte[] Compress(int[] data, int length)
	{
		throw new NotImplementedException();
	}

	public byte[] Compress(uint[] data, int length)
	{
		throw new NotImplementedException();
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
			using GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress);
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
		using GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress);
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
		throw new NotImplementedException();
	}

	public int DecompressAndSize(byte[] fi, out byte[] buffer)
	{
		throw new NotImplementedException();
	}

	public int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer)
	{
		throw new NotImplementedException();
	}

	public byte[] DecompressFromOffset(byte[] compressedData, int offset)
	{
		throw new NotImplementedException();
	}
}
