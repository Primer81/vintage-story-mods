using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

internal class ConcurrentIndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__23 : IEnumerator<T>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private T _003C_003E2__current;

		public ConcurrentIndexedFifoQueue<T> _003C_003E4__this;

		private uint _003Cend_snapshot_003E5__2;

		private uint _003Cpos_003E5__3;

		T IEnumerator<T>.Current
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
		public _003CGetEnumerator_003Ed__23(int _003C_003E1__state)
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
			ConcurrentIndexedFifoQueue<T> concurrentIndexedFifoQueue = _003C_003E4__this;
			switch (num)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Cend_snapshot_003E5__2 = concurrentIndexedFifoQueue.end;
				_003Cpos_003E5__3 = concurrentIndexedFifoQueue.startBleedingEdge;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Cpos_003E5__3++;
				break;
			}
			if (_003Cpos_003E5__3 != _003Cend_snapshot_003E5__2)
			{
				if ((int)(concurrentIndexedFifoQueue.startBleedingEdge - _003Cpos_003E5__3) > 0)
				{
					_003Cpos_003E5__3 = concurrentIndexedFifoQueue.startBleedingEdge;
					if ((int)(_003Cpos_003E5__3 - _003Cend_snapshot_003E5__2) >= 0)
					{
						goto IL_00b9;
					}
				}
				_003C_003E2__current = concurrentIndexedFifoQueue.elements[(ushort)_003Cpos_003E5__3 % concurrentIndexedFifoQueue.length];
				_003C_003E1__state = 1;
				return true;
			}
			goto IL_00b9;
			IL_00b9:
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

	internal readonly ConcurrentDictionary<long, T> elementsByIndex;

	private readonly T[] elements;

	private readonly int length;

	private volatile uint start;

	private volatile uint end;

	private volatile uint startBleedingEdge;

	private volatile uint endBleedingEdge;

	public int Count => (int)(end - start);

	public int Capacity => elements.Length;

	public ConcurrentIndexedFifoQueue(int capacity, int stages)
	{
		capacity = Math.Min(ArrayConvert.GetRoundedUpSize(capacity), 65536);
		elements = new T[capacity];
		length = capacity;
		elementsByIndex = new ConcurrentDictionary<long, T>(stages, capacity);
	}

	public bool IsFull()
	{
		return (int)(end - start) >= length;
	}

	public bool IsEmpty()
	{
		return start == end;
	}

	public T GetByIndex(long index)
	{
		elementsByIndex.TryGetValue(index, out var val);
		return val;
	}

	public void Enqueue(T elem)
	{
		elementsByIndex[elem.Index] = elem;
		EnqueueWithoutAddingToIndex(elem);
	}

	internal void EnqueueWithoutAddingToIndex(T elem)
	{
		uint localcopy = endBleedingEdge;
		if ((int)(localcopy - start) > length - 1)
		{
			throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
		}
		uint newEnd;
		while ((newEnd = Interlocked.CompareExchange(ref endBleedingEdge, localcopy + 1, localcopy)) != localcopy)
		{
			localcopy = newEnd;
			if ((int)(newEnd - start) > length - 1)
			{
				throw new Exception("Indexed Fifo Queue overflow. Try increasing servermagicnumbers RequestChunkColumnsQueueSize?");
			}
		}
		elements[(ushort)localcopy % length] = elem;
		Interlocked.Increment(ref end);
		if (!elementsByIndex.ContainsKey(elem.Index))
		{
			throw new Exception("In queue but missed from index!");
		}
	}

	public T DequeueWithoutRemovingFromIndex()
	{
		uint localcopy = startBleedingEdge;
		if ((int)(localcopy - end) >= 0)
		{
			return default(T);
		}
		uint newStart;
		while ((newStart = Interlocked.CompareExchange(ref startBleedingEdge, localcopy + 1, localcopy)) != localcopy)
		{
			localcopy = newStart;
			if ((int)(newStart - end) >= 0)
			{
				return default(T);
			}
		}
		T result = elements[(ushort)localcopy % length];
		Interlocked.Increment(ref start);
		elements[(ushort)localcopy % length] = default(T);
		return result;
	}

	public T Dequeue()
	{
		T elem = DequeueWithoutRemovingFromIndex();
		if (elem != null)
		{
			elementsByIndex.Remove(elem.Index);
		}
		return elem;
	}

	internal void Requeue()
	{
		if (IsFull())
		{
			Interlocked.Increment(ref endBleedingEdge);
			Interlocked.Increment(ref end);
			Interlocked.Increment(ref startBleedingEdge);
			Interlocked.Increment(ref start);
		}
		else
		{
			T elem = DequeueWithoutRemovingFromIndex();
			if (elem != null)
			{
				EnqueueWithoutAddingToIndex(elem);
			}
		}
	}

	public T Peek()
	{
		return elements[(ushort)startBleedingEdge % length];
	}

	public T PeekAtPosition(int position)
	{
		return elements[(ushort)((int)startBleedingEdge + position) % length];
	}

	public bool Remove(long index)
	{
		if (elementsByIndex.TryRemove(index, out var elem))
		{
			elem.FlagToDispose();
			return true;
		}
		return false;
	}

	[IteratorStateMachine(typeof(ConcurrentIndexedFifoQueue<>._003CGetEnumerator_003Ed__23))]
	public IEnumerator<T> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__23(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public ICollection<T> Snapshot()
	{
		return elementsByIndex.Values;
	}

	public void Clear()
	{
		elementsByIndex.Clear();
		start = 0u;
		startBleedingEdge = 0u;
		end = 0u;
		endBleedingEdge = 0u;
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = default(T);
		}
	}
}
