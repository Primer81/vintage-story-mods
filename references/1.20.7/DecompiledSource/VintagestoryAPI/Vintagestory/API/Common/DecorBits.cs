using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// An actual instance of this struct is the 'faceAndSubposition' data.
/// <br />The struct also provides various static methods to convert elements to and from a PackedIndex used in WorldChunk storage
/// </summary>
public struct DecorBits
{
	private int faceAndSubposition;

	/// <summary>
	/// A bit mask to select bits 0-14, i.e. the chunk's index3d
	/// </summary>
	private const int Index3dMask = 32767;

	/// <summary>
	/// A bit mask to select the three most significant bits of a byte, 0b11100000 or 0xE0
	/// </summary>
	private const int mask3Bits = 224;

	/// <summary>
	/// A bit mask to select the five least significant bits of a byte, 0b00011111 or 0x1F
	/// </summary>
	private const int mask5Bits = 31;

	/// <summary>
	/// A bit mask to select the three rotation data bits; this is also the maxvalue of the rotationData
	/// </summary>
	public const int maskRotationData = 7;

	public int Face => faceAndSubposition % 6;

	public int SubPosition => (faceAndSubposition / 6) & 0xFFF;

	public int Rotation
	{
		get
		{
			return faceAndSubposition / 6 >> 12;
		}
		set
		{
			int newSubPositionAndRotation = SubPosition + (value << 12);
			faceAndSubposition = faceAndSubposition % 6 + newSubPositionAndRotation * 6;
		}
	}

	public static implicit operator int(DecorBits a)
	{
		return a.faceAndSubposition;
	}

	public DecorBits(int value)
	{
		faceAndSubposition = value;
	}

	/// <summary>
	/// Simplest case, we supply just a face  (no subposition for cave-art, and no rotation)
	/// </summary>
	/// <param name="face"></param>
	public DecorBits(BlockFacing face)
	{
		faceAndSubposition = face.Index;
	}

	/// <summary>
	/// Turn both face and local voxel position to a decor faceAndSubposition index
	/// </summary>
	/// <param name="face"></param>
	/// <param name="vx">0..15</param>
	/// <param name="vy">0..15</param>
	/// <param name="vz">0..15</param>
	public DecorBits(BlockFacing face, int vx, int vy, int vz)
	{
		int offset = 0;
		switch (face.Index)
		{
		case 0:
			offset = 15 - vx + vy * 16;
			break;
		case 1:
			offset = 15 - vz + vy * 16;
			break;
		case 2:
			offset = vx + vy * 16;
			break;
		case 3:
			offset = vz + vy * 16;
			break;
		case 4:
			offset = vx + vz * 16;
			break;
		case 5:
			offset = vx + (15 - vz) * 16;
			break;
		}
		faceAndSubposition = face.Index + 6 * (1 + offset);
	}

	/// <summary>
	///             The packedIndex works like this: [radfast 7 Dec 2024, 1.20-rc.2]
	/// <code>
	/// The packedIndex has four components:
	///     index3d for the block's local x,y,z value within the chunk, each in the range 0-31, for 15 bits in total
	///     faceindex for the face of the block this decor is on (corresponding to BlockFacing.Index), range 0-5
	///     optionally, a subposition in the range 0-256, where 0 means no subposition, and values 1-256 give a subposition in the 16x16 subgrid, used for ArtPigment or similar
	///     optionally, 3 bits of rotation data
	///
	/// These are packed into bits in the following way, it has to be this way for backwards compatibility reasons (assuming we do not want to add a new chunk dataversion)
	/// 31 - 24  (the five Least Significant Bits of the subposition) * 6 + faceindex
	/// 23 - 21  (the three Most Significant Bits of the subposition)
	/// 20 - 19  (unused)
	/// 18 - 16  rotation data
	/// 15       (unused)  
	/// 14 - 0   index3d
	///
	/// (Exceptionally, the value in bits 31-24 has the magic value of (0x20 * 6 + faceindex), and the value in bits 23-16 is 0xE0, if a subposition value of 256 is intended: this works within the existing algorithms because 0xE0 + 0x20 == 0x100 i.e. 256.   If necessary we can have values up to 0x2A there, so the range of possible subpositions is up to 266)
	///
	/// 0000 0000 0000 0000 0000 0000 0000 0000
	/// </code>
	///
	/// </summary>
	public static int FaceAndSubpositionToIndex(int faceAndSubposition)
	{
		int subPosition = faceAndSubposition / 6;
		int rotationData = (subPosition >> 12) & 7;
		subPosition &= 0xFFF;
		int decorFaceIndex = faceAndSubposition % 6;
		if (subPosition < 256)
		{
			faceAndSubposition = decorFaceIndex + (subPosition & 0x1F) * 6;
			subPosition &= 0xE0;
		}
		else
		{
			faceAndSubposition = decorFaceIndex + 192;
			subPosition = 224;
		}
		return (faceAndSubposition << 24) + (subPosition + rotationData << 16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceAndSubpositionFromIndex(int packedIndex)
	{
		int bits16to23 = packedIndex >> 16;
		return ((packedIndex >> 24) & 0xFF) + ((bits16to23 & 0xE0) + ((bits16to23 & 7) << 12)) * 6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceToIndex(BlockFacing face)
	{
		return face.Index << 24;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FaceFromIndex(int packedIndex)
	{
		return ((packedIndex >> 24) & 0xFF) % 6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Index3dFromIndex(int packedIndex)
	{
		return packedIndex & 0x7FFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SubpositionFromIndex(int packedIndex)
	{
		return ((packedIndex >> 24) & 0xFF) / 6 + ((packedIndex >> 16) & 0xE0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int RotationFromIndex(int packedIndex)
	{
		return (packedIndex >> 16) & 7;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BlockFacing FacingFromIndex(int packedIndex)
	{
		return BlockFacing.ALLFACES[FaceFromIndex(packedIndex)];
	}
}
