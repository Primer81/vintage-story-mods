using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public class CachedCuboidList : IEnumerable<Cuboidd>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__9 : IEnumerator<Cuboidd>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private Cuboidd _003C_003E2__current;

		public CachedCuboidList _003C_003E4__this;

		private int _003Ci_003E5__2;

		Cuboidd IEnumerator<Cuboidd>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetEnumerator_003Ed__9(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			CachedCuboidList cachedCuboidList = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Ci_003E5__2 = 0;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Ci_003E5__2++;
				break;
			}
			if (_003Ci_003E5__2 < cachedCuboidList.Count)
			{
				_003C_003E2__current = cachedCuboidList.cuboids[_003Ci_003E5__2];
				_003C_003E1__state = 1;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public Cuboidd[] cuboids = new Cuboidd[0];

	public BlockPos[] positions;

	public Block[] blocks;

	public int Count;

	private int populatedSize;

	public void Clear()
	{
		Count = 0;
	}

	public void Add(Cuboidf[] cuboids, int x, int y, int z, Block block = null)
	{
		for (int i = 0; i < cuboids.Length; i++)
		{
			Add(cuboids[i], x, y, z, block);
		}
	}

	public void Add(Cuboidf cuboid, int x, int y, int z, Block block = null)
	{
		if (cuboid == null)
		{
			return;
		}
		if (Count >= populatedSize)
		{
			if (Count >= cuboids.Length)
			{
				ExpandArrays();
			}
			cuboids[Count] = cuboid.OffsetCopyDouble(x, y % 32768, z);
			positions[Count] = new BlockPos(x, y, z);
			blocks[Count] = block;
			populatedSize++;
		}
		else
		{
			cuboids[Count].Set(cuboid.X1 + (float)x, cuboid.Y1 + (float)(y % 32768), cuboid.Z1 + (float)z, cuboid.X2 + (float)x, cuboid.Y2 + (float)(y % 32768), cuboid.Z2 + (float)z);
			positions[Count].Set(x, y, z);
			blocks[Count] = block;
		}
		Count++;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__9))]
	public IEnumerator<Cuboidd> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__9(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void ExpandArrays()
	{
		int num = ((populatedSize == 0) ? 16 : (populatedSize * 3 / 2));
		Cuboidd[] newCuboids = new Cuboidd[num];
		BlockPos[] newPositions = new BlockPos[num];
		Block[] newBlocks = new Block[num];
		for (int i = 0; i < populatedSize; i++)
		{
			newCuboids[i] = cuboids[i];
			newPositions[i] = positions[i];
			newBlocks[i] = blocks[i];
		}
		cuboids = newCuboids;
		positions = newPositions;
		blocks = newBlocks;
	}
}
