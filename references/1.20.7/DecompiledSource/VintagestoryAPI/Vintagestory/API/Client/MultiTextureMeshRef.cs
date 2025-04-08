using System;

namespace Vintagestory.API.Client;

public class MultiTextureMeshRef : IDisposable
{
	public MeshRef[] meshrefs;

	public int[] textureids;

	private bool disposed;

	public bool Disposed => disposed;

	public bool Initialized
	{
		get
		{
			if (meshrefs.Length != 0)
			{
				return meshrefs[0].Initialized;
			}
			return false;
		}
	}

	public MultiTextureMeshRef(MeshRef[] meshrefs, int[] textureids)
	{
		this.meshrefs = meshrefs;
		this.textureids = textureids;
	}

	public void Dispose()
	{
		MeshRef[] array = meshrefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Dispose();
		}
		disposed = true;
	}
}
