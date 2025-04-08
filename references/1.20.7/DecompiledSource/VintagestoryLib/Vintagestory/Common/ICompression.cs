namespace Vintagestory.Common;

public interface ICompression
{
	byte[] Compress(byte[] data);

	byte[] Compress(byte[] data, int len, int reserveOffset);

	byte[] Compress(ushort[] data);

	byte[] Compress(int[] data, int length);

	byte[] Compress(uint[] data, int length);

	byte[] Decompress(byte[] fi);

	byte[] Decompress(byte[] fi, int offset, int length);

	int DecompressAndSize(byte[] fi, out byte[] buffer);

	void Decompress(byte[] fi, byte[] dest);

	int DecompressAndSize(byte[] compressedData, int offset, int length, out byte[] buffer);
}
