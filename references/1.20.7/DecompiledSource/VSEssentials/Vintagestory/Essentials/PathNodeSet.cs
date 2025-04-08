using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.Essentials;

public class PathNodeSet : IEnumerable<PathNode>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__13 : IEnumerator<PathNode>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private PathNode _003C_003E2__current;

		public PathNodeSet _003C_003E4__this;

		private int _003Cbucket_003E5__2;

		private int _003Ci_003E5__3;

		PathNode IEnumerator<PathNode>.Current
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
		public _003CGetEnumerator_003Ed__13(int _003C_003E1__state)
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
			PathNodeSet pathNodeSet = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Ci_003E5__3++;
				goto IL_006a;
			}
			_003C_003E1__state = -1;
			_003Cbucket_003E5__2 = 0;
			goto IL_008f;
			IL_006a:
			if (_003Ci_003E5__3 < pathNodeSet.bucketCount[_003Cbucket_003E5__2])
			{
				_003C_003E2__current = pathNodeSet.buckets[_003Cbucket_003E5__2][_003Ci_003E5__3];
				_003C_003E1__state = 1;
				return true;
			}
			_003Cbucket_003E5__2++;
			goto IL_008f;
			IL_008f:
			if (_003Cbucket_003E5__2 < 4)
			{
				_003Ci_003E5__3 = 0;
				goto IL_006a;
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

	private int arraySize = 16;

	private PathNode[][] buckets = new PathNode[4][];

	private int[] bucketCount = new int[4];

	private int size;

	public int Count => size;

	public void Clear()
	{
		for (int i = 0; i < 4; i++)
		{
			bucketCount[i] = 0;
		}
		size = 0;
	}

	public PathNodeSet()
	{
		for (int i = 0; i < 4; i++)
		{
			buckets[i] = new PathNode[arraySize];
		}
	}

	public bool Add(PathNode value)
	{
		int bucket = value.Z % 2 * 2 + value.X % 2;
		bucket = (bucket + 4) % 4;
		PathNode[] set = buckets[bucket];
		int size = bucketCount[bucket];
		int i = size;
		while (--i >= 0)
		{
			if (value.Equals(set[i]))
			{
				return false;
			}
		}
		if (size >= arraySize)
		{
			ExpandArrays();
			set = buckets[bucket];
		}
		float fCost = value.fCost;
		i = size - 1;
		while (i >= 0 && (set[i].fCost < fCost || (set[i].fCost == fCost && set[i].hCost < value.hCost)))
		{
			i--;
		}
		i++;
		int j = size;
		while (j > i)
		{
			set[j] = set[--j];
		}
		set[i] = value;
		size++;
		bucketCount[bucket] = size;
		this.size++;
		return true;
	}

	public PathNode RemoveNearest()
	{
		if (size == 0)
		{
			return null;
		}
		PathNode nearestNode = null;
		int bucketToRemoveFrom = 0;
		for (int bucket = 0; bucket < 4; bucket++)
		{
			int endIndex = bucketCount[bucket] - 1;
			if (endIndex >= 0)
			{
				PathNode node = buckets[bucket][endIndex];
				if ((object)nearestNode == null || node.fCost < nearestNode.fCost || (node.fCost == nearestNode.fCost && node.hCost < nearestNode.hCost))
				{
					nearestNode = node;
					bucketToRemoveFrom = bucket;
				}
			}
		}
		bucketCount[bucketToRemoveFrom]--;
		size--;
		return nearestNode;
	}

	public void Remove(PathNode value)
	{
		int bucket = value.Z % 2 * 2 + value.X % 2;
		bucket = (bucket + 4) % 4;
		PathNode[] set = buckets[bucket];
		int size = bucketCount[bucket];
		int i = size;
		while (--i >= 0)
		{
			if (value.Equals(set[i]))
			{
				size = --bucketCount[bucket];
				while (i < size)
				{
					set[i] = set[++i];
				}
				this.size--;
				break;
			}
		}
	}

	public PathNode TryFindValue(PathNode value)
	{
		int bucket = value.Z % 2 * 2 + value.X % 2;
		bucket = (bucket + 4) % 4;
		PathNode[] set = buckets[bucket];
		int i = bucketCount[bucket];
		while (--i >= 0)
		{
			if (value.Equals(set[i]))
			{
				return set[i];
			}
		}
		return null;
	}

	private void ExpandArrays()
	{
		int newSize = arraySize * 3 / 2;
		for (int bucket = 0; bucket < 4; bucket++)
		{
			PathNode[] newArray = new PathNode[newSize];
			int size = bucketCount[bucket];
			PathNode[] set = buckets[bucket];
			for (int i = 0; i < size; i++)
			{
				newArray[i] = set[i];
			}
			buckets[bucket] = newArray;
		}
		arraySize = newSize;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__13))]
	public IEnumerator<PathNode> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__13(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
