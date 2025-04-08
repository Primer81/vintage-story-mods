using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

public class FastSetOfLongs : IEnumerable<long>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__9 : IEnumerator<long>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private long _003C_003E2__current;

		public FastSetOfLongs _003C_003E4__this;

		private int _003Ci_003E5__2;

		long IEnumerator<long>.Current
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
			FastSetOfLongs fastSetOfLongs = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < fastSetOfLongs.size)
			{
				_003C_003E2__current = fastSetOfLongs.set[_003Ci_003E5__2];
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

	private long[] set;

	private long last = long.MinValue;

	public int Count => size;

	public void Clear()
	{
		size = 0;
		last = long.MinValue;
	}

	public FastSetOfLongs()
	{
		set = new long[27];
	}

	/// <summary>
	/// Return false if the set already contained this value; return true if the Add was successful
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool Add(long value)
	{
		if (value == last && size > 0)
		{
			return false;
		}
		last = value;
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

	private void expandArray()
	{
		long[] newArray = new long[set.Length * 3 / 2 + 1];
		for (int i = 0; i < set.Length; i++)
		{
			newArray[i] = set[i];
		}
		set = newArray;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__9))]
	public IEnumerator<long> GetEnumerator()
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
}
