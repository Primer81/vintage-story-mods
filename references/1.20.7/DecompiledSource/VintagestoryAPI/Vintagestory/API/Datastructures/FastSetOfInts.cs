using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

public class FastSetOfInts : IEnumerable<int>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__11 : IEnumerator<int>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private int _003C_003E2__current;

		public FastSetOfInts _003C_003E4__this;

		private int _003Ci_003E5__2;

		int IEnumerator<int>.Current
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
		public _003CGetEnumerator_003Ed__11(int _003C_003E1__state)
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
			FastSetOfInts fastSetOfInts = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < fastSetOfInts.size)
			{
				_003C_003E2__current = fastSetOfInts.set[_003Ci_003E5__2];
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

	private int size;

	private int[] set;

	public int Count => size;

	public void Clear()
	{
		size = 0;
	}

	public FastSetOfInts()
	{
		set = new int[27];
	}

	/// <summary>
	/// Add four separate components, assumed to be signed int in the range -128 to +127
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="c"></param>
	/// <param name="d"></param>
	/// <returns></returns>
	public bool Add(int a, int b, int c, int d)
	{
		return Add(a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24));
	}

	/// <summary>
	/// Return false if the set already contained this value; return true if the Add was successful
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool Add(int value)
	{
		int i = size;
		while (--i >= 0)
		{
			if (set[i] == value)
			{
				return false;
			}
		}
		if (size >= set.Length)
		{
			expandArray();
		}
		set[size++] = value;
		return true;
	}

	public void RemoveIfMatches(int a, int b, int c, int d)
	{
		int i = size;
		int match = a + 128 + (b + 128 << 8) + (c + 128 << 16) + (d << 24);
		while (--i >= 0)
		{
			if (set[i] == match)
			{
				RemoveAt(i);
				break;
			}
		}
	}

	private void RemoveAt(int i)
	{
		for (int j = i + 1; j < size; j++)
		{
			set[j - 1] = set[j];
		}
		size--;
	}

	private void expandArray()
	{
		int[] newArray = new int[set.Length * 3 / 2 + 1];
		for (int i = 0; i < set.Length; i++)
		{
			newArray[i] = set[i];
		}
		set = newArray;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__11))]
	public IEnumerator<int> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__11(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
