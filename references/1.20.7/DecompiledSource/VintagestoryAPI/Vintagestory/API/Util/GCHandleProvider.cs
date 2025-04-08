using System;
using System.Runtime.InteropServices;

namespace Vintagestory.API.Util;

public class GCHandleProvider : IDisposable
{
	public nint Pointer => Handle.ToIntPtr();

	public GCHandle Handle { get; }

	public GCHandleProvider(object target)
	{
		Handle = target.ToGcHandle();
	}

	private void ReleaseUnmanagedResources()
	{
		if (Handle.IsAllocated)
		{
			Handle.Free();
		}
	}

	public void Dispose()
	{
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}

	~GCHandleProvider()
	{
		ReleaseUnmanagedResources();
	}
}
