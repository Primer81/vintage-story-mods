using System.Runtime.InteropServices;

namespace Vintagestory.API.Util;

public static class ObjectHandleExtensions
{
	public static nint ToIntPtr(this object target)
	{
		return GCHandle.Alloc(target).ToIntPtr();
	}

	public static GCHandle ToGcHandle(this object target)
	{
		return GCHandle.Alloc(target);
	}

	public static nint ToIntPtr(this GCHandle target)
	{
		return GCHandle.ToIntPtr(target);
	}
}
