using System;
using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.Common;

public class BlocksCompositeIdEnumerator : IEnumerator<int>, IEnumerator, IDisposable
{
	private int index;

	public ChunkData inst;

	public int Current => inst[index];

	object IEnumerator.Current => inst[index];

	public BlocksCompositeIdEnumerator(ChunkData inst)
	{
		this.inst = inst;
	}

	public void Dispose()
	{
	}

	public bool MoveNext()
	{
		index++;
		return index < inst.Length;
	}

	public void Reset()
	{
		index = 0;
	}
}
