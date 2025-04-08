using System.Runtime.InteropServices;

namespace Vintagestory.API.MathTools;

[StructLayout(LayoutKind.Explicit)]
internal struct FloatIntUnion
{
	[FieldOffset(0)]
	public float f;

	[FieldOffset(0)]
	public int tmp;
}
