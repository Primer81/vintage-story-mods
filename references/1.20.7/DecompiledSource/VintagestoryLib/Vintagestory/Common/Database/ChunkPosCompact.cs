namespace Vintagestory.Common.Database;

public struct ChunkPosCompact
{
	private const int bitSizeXZ = 21;

	private const int bitMask = 2097151;

	private readonly long compacted;

	public readonly int X => (int)compacted & 0x1FFFFF;

	public readonly int Y => (int)(compacted >> 42);

	public readonly int Z => (int)(compacted >> 21) & 0x1FFFFF;

	public ChunkPosCompact(int cx, int cy, int cz)
	{
		compacted = (uint)cx | ((long)cz << 21) | ((long)cy << 42);
	}

	public override int GetHashCode()
	{
		return (int)compacted + (int)(compacted >> 32) * 13;
	}

	public override bool Equals(object obj)
	{
		if (obj is ChunkPosCompact other)
		{
			return compacted == other.compacted;
		}
		return base.Equals(obj);
	}

	public static bool operator ==(ChunkPosCompact left, ChunkPosCompact right)
	{
		return left.compacted == right.compacted;
	}

	public static bool operator !=(ChunkPosCompact left, ChunkPosCompact right)
	{
		return left.compacted != right.compacted;
	}
}
