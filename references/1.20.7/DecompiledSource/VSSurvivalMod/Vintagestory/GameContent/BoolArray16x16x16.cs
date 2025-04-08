using System.Collections;
using System.Runtime.CompilerServices;

namespace Vintagestory.GameContent;

public class BoolArray16x16x16
{
	private BitArray voxels;

	public bool this[int x, int y, int z]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return voxels[(x * 16 + y) * 16 + z];
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set
		{
			voxels[(x * 16 + y) * 16 + z] = value;
		}
	}

	public BoolArray16x16x16()
	{
		voxels = new BitArray(4096);
	}
}
