using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common;

/// <summary>
/// Use like any IDictionary. Similar to a FastSmallDictionary, but this one is thread-safe for simultaneous reads and writes - will not throw a ConcurrentModificationException
/// <br />This also inherently behaves as an OrderedDictionary (though without the OrderedDictionary extension methods such as ValuesOrdered, those can be added in future if required)
/// <br />Low-lock: there is no lock or interlocked operation except when adding new keys or when removing entries
/// <br />Low-memory: and contains only a single null field, if it is empty
/// <br />Two simultaneous writes, with the same key, at the same time, on different threads: small chance of throwing an intentional ConcurrentModificationException if both have the same keys, otherwise it's virtually impossible for us to preserve the rule that the Dictionary holds exactly one entry per key
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ConcurrentSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__26 : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private KeyValuePair<TKey, TValue> _003C_003E2__current;

		public ConcurrentSmallDictionary<TKey, TValue> _003C_003E4__this;

		private DTable<TKey, TValue> _003Ccontents_003E5__2;

		private int _003Cend_snapshot_003E5__3;

		private int _003Cpos_003E5__4;

		KeyValuePair<TKey, TValue> IEnumerator<KeyValuePair<TKey, TValue>>.Current
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
		public _003CGetEnumerator_003Ed__26(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Ccontents_003E5__2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			ConcurrentSmallDictionary<TKey, TValue> concurrentSmallDictionary = _003C_003E4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Cpos_003E5__4++;
			}
			else
			{
				_003C_003E1__state = -1;
				_003Ccontents_003E5__2 = concurrentSmallDictionary.contents;
				if (_003Ccontents_003E5__2 == null)
				{
					goto IL_00b6;
				}
				_003Cend_snapshot_003E5__3 = _003Ccontents_003E5__2.count;
				_003Cpos_003E5__4 = 0;
			}
			if (_003Cpos_003E5__4 < _003Cend_snapshot_003E5__3)
			{
				_003C_003E2__current = new KeyValuePair<TKey, TValue>(_003Ccontents_003E5__2.keys[_003Cpos_003E5__4], _003Ccontents_003E5__2.values[_003Cpos_003E5__4]);
				_003C_003E1__state = 1;
				return true;
			}
			goto IL_00b6;
			IL_00b6:
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

	private DTable<TKey, TValue> contents;

	public ICollection<TKey> Keys
	{
		get
		{
			DTable<TKey, TValue> contents = this.contents;
			if (contents != null)
			{
				return contents.KeysCopy();
			}
			return new TKey[0];
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			DTable<TKey, TValue> contents = this.contents;
			if (contents != null)
			{
				return contents.ValuesCopy();
			}
			return new TValue[0];
		}
	}

	public bool IsReadOnly => false;

	int ICollection<KeyValuePair<TKey, TValue>>.Count
	{
		get
		{
			DTable<TKey, TValue> contents = this.contents;
			if (contents != null)
			{
				return contents.count;
			}
			return 0;
		}
	}

	/// <summary>
	/// Amount of entries currently in the Dictionary
	/// </summary>
	public int Count
	{
		get
		{
			DTable<TKey, TValue> contents = this.contents;
			if (contents != null)
			{
				return contents.count;
			}
			return 0;
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			return contents.GetValue(key);
		}
		set
		{
			Add(key, value);
		}
	}

	public ConcurrentSmallDictionary(int capacity)
	{
		if (capacity == 0)
		{
			contents = null;
		}
		else
		{
			contents = new DTable<TKey, TValue>(capacity);
		}
	}

	public ConcurrentSmallDictionary()
		: this(4)
	{
	}

	public bool IsEmpty()
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents != null)
		{
			return contents.count == 0;
		}
		return true;
	}

	public void Add(TKey key, TValue value)
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents == null)
		{
			if (Interlocked.CompareExchange(ref this.contents, new DTable<TKey, TValue>(key, value), contents) != contents)
			{
				Add(key, value);
			}
		}
		else if (!contents.ReplaceIfKeyExists(key, value))
		{
			if (!contents.Add(key, value) && Interlocked.CompareExchange(ref this.contents, new DTable<TKey, TValue>(contents, key, value), contents) != contents)
			{
				Add(key, value);
			}
			this.contents.DuplicateKeyCheck(key);
		}
	}

	public TValue TryGetValue(TKey key)
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents != null)
		{
			return contents.TryGetValue(key);
		}
		return default(TValue);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents == null)
		{
			value = default(TValue);
			return false;
		}
		return contents.TryGetValue(key, out value);
	}

	public bool ContainsKey(TKey key)
	{
		return contents?.ContainsKey(key) ?? false;
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		if (!contents.TryGetValue(item.Key, out var valueFound))
		{
			return false;
		}
		return item.Value.Equals(valueFound);
	}

	public bool Remove(TKey key)
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents == null)
		{
			return false;
		}
		int i = contents.IndexOf(key);
		if (i < 0)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref this.contents, new DTable<TKey, TValue>(contents, i), contents) != contents)
		{
			return Remove(key);
		}
		return true;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		DTable<TKey, TValue> contents = this.contents;
		if (contents == null)
		{
			return false;
		}
		int i = contents.IndexOf(item.Key, item.Value);
		if (i < 0)
		{
			return false;
		}
		if (Interlocked.CompareExchange(ref this.contents, new DTable<TKey, TValue>(contents, i), contents) != contents)
		{
			return Remove(item);
		}
		return true;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		contents.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Threadsafe: but this might occasionally enumerate over a value which has, meanwhile, been removed from the Dictionary by a different thread
	/// Iterate over .Keys or .Values instead if an instantaneous snapshot is required (which will also therefore be a historical snapshot, if another thread meanwhile makes changes)
	/// </summary>
	/// <returns></returns>
	[IteratorStateMachine(typeof(ConcurrentSmallDictionary<, >._003CGetEnumerator_003Ed__26))]
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__26(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	/// <summary>
	/// Wipes the contents and resets the count.
	/// </summary>
	public void Clear()
	{
		contents = null;
	}
}
