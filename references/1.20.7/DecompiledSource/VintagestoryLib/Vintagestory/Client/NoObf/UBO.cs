using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

public class UBO : UBORef
{
	public override void Bind()
	{
		GL.BindBuffer(BufferTarget.UniformBuffer, Handle);
		GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, Handle);
	}

	public override void Dispose()
	{
		base.Dispose();
		GL.DeleteBuffers(1, ref Handle);
	}

	public override void Unbind()
	{
		GL.BindBuffer(BufferTarget.UniformBuffer, 0);
	}

	public override void Update<T>(T data)
	{
		if (Unsafe.SizeOf<T>() != base.Size)
		{
			throw new ArgumentException("Supplied struct must be of byte size " + base.Size + " but has size " + Unsafe.SizeOf<T>());
		}
		Bind();
		using (GCHandleProvider handleProvider = new GCHandleProvider(data))
		{
			GL.BufferData(BufferTarget.UniformBuffer, base.Size, handleProvider.Pointer, BufferUsageHint.DynamicDraw);
		}
		Unbind();
	}

	public override void Update<T>(T data, int offset, int size)
	{
		if (Unsafe.SizeOf<T>() != base.Size)
		{
			throw new ArgumentException("Supplied struct must be of byte size " + base.Size + " but has size " + Unsafe.SizeOf<T>());
		}
		Bind();
		using (GCHandleProvider handleProvider = new GCHandleProvider(data))
		{
			GL.BufferSubData(BufferTarget.UniformBuffer, offset, size, handleProvider.Pointer);
		}
		Unbind();
	}

	public override void Update(object data, int offset, int size)
	{
		Bind();
		GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
		nint ptr = pinned.AddrOfPinnedObject();
		GL.BufferSubData(BufferTarget.UniformBuffer, offset, size, ptr);
		pinned.Free();
		Unbind();
	}
}
