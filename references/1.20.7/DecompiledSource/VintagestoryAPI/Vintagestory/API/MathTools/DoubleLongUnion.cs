using System.Runtime.InteropServices;

namespace Vintagestory.API.MathTools;

[StructLayout(LayoutKind.Explicit)]
internal struct DoubleLongUnion
{
	[FieldOffset(0)]
	public double f;

	[FieldOffset(0)]
	public long tmp;
}
