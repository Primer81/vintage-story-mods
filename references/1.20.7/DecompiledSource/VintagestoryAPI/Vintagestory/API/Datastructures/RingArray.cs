using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public class RingArray<T> : IEnumerable<T>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__15 : IEnumerator<T>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private T _003C_003E2__current;

		public RingArray<T> _003C_003E4__this;

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
		public _003CGetEnumerator_003Ed__15(int _003C_003E1__state)
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
			RingArray<T> ringArray = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < ringArray.elements.Length)
			{
				_003C_003E2__current = ringArray.elements[(ringArray.cursor + _003Ci_003E5__2) % ringArray.elements.Length];
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

	private T[] elements;

	private int cursor;

	public T this[int index]
	{
		get
		{
			return elements[index];
		}
		set
		{
			elements[index] = value;
		}
	}

	public int EndPosition
	{
		get
		{
			return cursor;
		}
		set
		{
			cursor = value;
		}
	}

	public T[] Values => elements;

	/// <summary>
	/// Total size of the stack
	/// </summary>
	public int Length => elements.Length;

	public RingArray(int capacity)
	{
		elements = new T[capacity];
	}

	public RingArray(int capacity, T[] initialvalues)
	{
		elements = new T[capacity];
		if (initialvalues != null)
		{
			for (int i = 0; i < initialvalues.Length; i++)
			{
				Add(initialvalues[i]);
			}
		}
	}

	/// <summary>
	/// Pushes an element onto the end of the queue
	/// </summary>
	/// <param name="elem"></param>
	public void Add(T elem)
	{
		elements[cursor] = elem;
		cursor = (cursor + 1) % elements.Length;
	}

	[IteratorStateMachine(typeof(RingArray<>._003CGetEnumerator_003Ed__15))]
	public IEnumerator<T> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__15(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Wipes the buffer and resets the count
	/// </summary>
	public void Clear()
	{
		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = default(T);
		}
		cursor = 0;
	}

	/// <summary>
	/// If smaller than the old size, will discard oldest elements first
	/// </summary>
	/// <param name="size"></param>
	public void ResizeTo(int size)
	{
		T[] elements = new T[size];
		for (int i = 0; i < this.elements.Length; i++)
		{
			elements[size - 1] = this.elements[GameMath.Mod(EndPosition - i, this.elements.Length)];
			size--;
			if (size <= 0)
			{
				break;
			}
		}
		this.elements = elements;
	}
}
