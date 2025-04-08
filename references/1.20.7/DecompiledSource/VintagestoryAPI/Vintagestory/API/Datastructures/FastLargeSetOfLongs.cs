using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

public class FastLargeSetOfLongs : IEnumerable<long>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__10 : IEnumerator<long>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private long _003C_003E2__current;

		public FastLargeSetOfLongs _003C_003E4__this;

		private int _003Cj_003E5__2;

		private int _003Csize_003E5__3;

		private long[] _003Cset_003E5__4;

		private int _003Ci_003E5__5;

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
		public _003CGetEnumerator_003Ed__10(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cset_003E5__4 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			FastLargeSetOfLongs fastLargeSetOfLongs = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Ci_003E5__5++;
				goto IL_008c;
			}
			_003C_003E1__state = -1;
			_003Cj_003E5__2 = 0;
			goto IL_00b1;
			IL_008c:
			if (_003Ci_003E5__5 < _003Csize_003E5__3)
			{
				_003C_003E2__current = _003Cset_003E5__4[_003Ci_003E5__5];
				_003C_003E1__state = 1;
				return true;
			}
			_003Cset_003E5__4 = null;
			_003Cj_003E5__2++;
			goto IL_00b1;
			IL_00b1:
			if (_003Cj_003E5__2 < fastLargeSetOfLongs.buckets.Length)
			{
				_003Csize_003E5__3 = fastLargeSetOfLongs.sizes[_003Cj_003E5__2];
				_003Cset_003E5__4 = fastLargeSetOfLongs.buckets[_003Cj_003E5__2];
				_003Ci_003E5__5 = 0;
				goto IL_008c;
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

	private int count;

	private readonly long[][] buckets;

	private readonly int[] sizes;

	private readonly int mask;

	public int Count => count;

	public void Clear()
	{
		count = 0;
		for (int i = 0; i < sizes.Length; i++)
		{
			sizes[i] = 0;
		}
	}

	public FastLargeSetOfLongs(int numbuckets)
	{
		int num = numbuckets - 1;
		int num2 = num | (num >> 1);
		int num3 = num2 | (num2 >> 2);
		int num4 = num3 | (num3 >> 4);
		int num5 = num4 | (num4 >> 8);
		numbuckets = (num5 | (num5 >> 16)) + 1;
		sizes = new int[numbuckets];
		buckets = new long[numbuckets][];
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i] = new long[7];
		}
		mask = numbuckets - 1;
	}

	/// <summary>
	/// Return false if the set already contained this value; return true if the Add was successful
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool Add(long value)
	{
		int j = (int)value & mask;
		long[] set = buckets[j];
		int siz = sizes[j];
		int i = siz;
		while (--i >= 0)
		{
			if (set[i] == value)
			{
				return false;
			}
		}
		if (siz >= set.Length)
		{
			set = expandArray(j);
		}
		set[siz++] = value;
		sizes[j] = siz;
		count++;
		return true;
	}

	private long[] expandArray(int j)
	{
		long[] set = buckets[j];
		long[] newArray = new long[set.Length * 3 / 2 + 1];
		for (int i = 0; i < set.Length; i++)
		{
			newArray[i] = set[i];
		}
		buckets[j] = newArray;
		return newArray;
	}

	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__10))]
	public IEnumerator<long> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__10(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
