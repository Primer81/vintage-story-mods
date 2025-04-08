using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.Common;

internal class IndexedFifoQueue<T> : IEnumerable<T>, IEnumerable where T : ILongIndex
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__18 : IEnumerator<T>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private T _003C_003E2__current;

		public IndexedFifoQueue<T> _003C_003E4__this;

		private int _003Ci_003E5__2;

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
		public _003CGetEnumerator_003Ed__18(int _003C_003E1__state)
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
			IndexedFifoQueue<T> indexedFifoQueue = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < indexedFifoQueue.Count)
			{
				int pos = _003Ci_003E5__2 + indexedFifoQueue.start;
				if (pos >= indexedFifoQueue.elements.Length)
				{
					pos -= indexedFifoQueue.elements.Length;
				}
				_003C_003E2__current = indexedFifoQueue.elements[pos];
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

	private readonly Dictionary<long, T> elementsByIndex;

	private readonly T[] elements;

	private int start;

	private int end;

	private bool isfull;

	public int Count
	{
		get
		{
			if (end >= start && (!isfull || end != start))
			{
				return end - start;
			}
			return elements.Length - start + end;
		}
	}

	public int Capacity => elements.Length;

	public IndexedFifoQueue(int capacity)
	{
		elements = new T[capacity];
		elementsByIndex = new Dictionary<long, T>(capacity);
	}

	public bool IsFull()
	{
		return isfull;
	}

	public T GetByIndex(long index)
	{
		T val = default(T);
		elementsByIndex.TryGetValue(index, out val);
		return val;
	}

	public T GetAtPosition(int position)
	{
		return elements[(start + position) % elements.Length];
	}

	public void Enqueue(T elem)
	{
		if (Count >= elements.Length - 1)
		{
			throw new Exception("Indexed Fifo Queue overflow");
		}
		elements[end] = elem;
		elementsByIndex[elem.Index] = elem;
		end++;
		if (end >= elements.Length)
		{
			end = 0;
		}
		isfull = start == end;
	}

	public T Dequeue()
	{
		T elem = elements[start];
		elements[start] = default(T);
		elementsByIndex.Remove(elem.Index);
		if (start != end)
		{
			start++;
		}
		if (start >= elements.Length)
		{
			start = 0;
		}
		isfull = false;
		return elem;
	}

	internal void Requeue()
	{
		T elem = Dequeue();
		if (elem != null)
		{
			Enqueue(elem);
		}
	}

	public T Peek()
	{
		return elements[start];
	}

	public bool Remove(long index)
	{
		bool found = false;
		elementsByIndex.Remove(index);
		for (int i = 0; i < Count; i++)
		{
			int pos = (i + start) % elements.Length;
			if (elements[pos].Index == index)
			{
				found = true;
			}
			if (found)
			{
				int nextPos = (pos + 1) % elements.Length;
				if (elements[nextPos] == null)
				{
					break;
				}
				elements[pos] = elements[nextPos];
			}
		}
		if (found)
		{
			end--;
			if (end < 0)
			{
				end += elements.Length;
			}
			elements[end] = default(T);
			isfull = false;
		}
		return found;
	}

	[IteratorStateMachine(typeof(IndexedFifoQueue<>._003CGetEnumerator_003Ed__18))]
	public IEnumerator<T> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__18(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = default(T);
		}
		elementsByIndex.Clear();
		start = 0;
		end = 0;
		isfull = false;
	}
}
