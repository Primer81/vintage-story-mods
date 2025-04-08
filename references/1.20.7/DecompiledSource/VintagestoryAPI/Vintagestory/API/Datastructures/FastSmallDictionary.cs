using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.Datastructures;

/// <summary>
/// A fast implementation of IDictionary using arrays.  Only suitable for small dictionaries, typically 1-20 entries.
/// <br />Note that Add is implemented differently from a standard Dictionary, it does not check that the key is not already present (and does not throw an ArgumentException)
/// <br />Additional methods AddIfNotPresent() and Clone() are provided for convenience.  There are also additional convenient constructors
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class FastSmallDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__27 : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private KeyValuePair<TKey, TValue> _003C_003E2__current;

		public FastSmallDictionary<TKey, TValue> _003C_003E4__this;

		private int _003Ci_003E5__2;

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
		public _003CGetEnumerator_003Ed__27(int _003C_003E1__state)
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
			FastSmallDictionary<TKey, TValue> fastSmallDictionary = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < fastSmallDictionary.count)
			{
				_003C_003E2__current = new KeyValuePair<TKey, TValue>(fastSmallDictionary.keys[_003Ci_003E5__2], fastSmallDictionary.values[_003Ci_003E5__2]);
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

	private TKey[] keys;

	private TValue[] values;

	private int count;

	public ICollection<TKey> Keys
	{
		get
		{
			TKey[] result = new TKey[count];
			Array.Copy(keys, result, count);
			return result;
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			TValue[] result = new TValue[count];
			Array.Copy(values, result, count);
			return result;
		}
	}

	int ICollection<KeyValuePair<TKey, TValue>>.Count => count;

	public bool IsReadOnly => false;

	public int Count => count;

	/// <summary>
	/// It is calling code's responsibility to ensure the key being searched for is not null
	/// </summary>
	public TValue this[TKey key]
	{
		get
		{
			for (int i = 0; i < keys.Length && i < count; i++)
			{
				ref TKey reference = ref key;
				TKey val = default(TKey);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				if (reference.Equals(keys[i]))
				{
					return values[i];
				}
			}
			throw new KeyNotFoundException("The key " + key.ToString() + " was not found");
		}
		set
		{
			for (int i = 0; i < count; i++)
			{
				ref TKey reference = ref key;
				TKey val = default(TKey);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				if (reference.Equals(keys[i]))
				{
					values[i] = value;
					return;
				}
			}
			if (count == keys.Length)
			{
				ExpandArrays();
			}
			keys[count] = key;
			values[count++] = value;
		}
	}

	public FastSmallDictionary(int size)
	{
		keys = new TKey[size];
		values = new TValue[size];
	}

	public FastSmallDictionary(TKey key, TValue value)
		: this(1)
	{
		keys[0] = key;
		values[0] = value;
		count = 1;
	}

	public FastSmallDictionary(IDictionary<TKey, TValue> dict)
		: this(dict.Count)
	{
		foreach (KeyValuePair<TKey, TValue> val in dict)
		{
			Add(val);
		}
	}

	public FastSmallDictionary<TKey, TValue> Clone()
	{
		FastSmallDictionary<TKey, TValue> result = new FastSmallDictionary<TKey, TValue>(count);
		result.keys = new TKey[count];
		result.values = new TValue[count];
		result.count = count;
		Array.Copy(keys, result.keys, count);
		Array.Copy(values, result.values, count);
		return result;
	}

	public TKey GetFirstKey()
	{
		return keys[0];
	}

	public TValue TryGetValue(string key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (key.Equals(keys[i]))
			{
				return values[i];
			}
		}
		return default(TValue);
	}

	private void ExpandArrays()
	{
		int num = keys.Length + 3;
		TKey[] newKeys = new TKey[num];
		TValue[] newValues = new TValue[num];
		for (int i = 0; i < keys.Length; i++)
		{
			newKeys[i] = keys[i];
			newValues[i] = values[i];
		}
		values = newValues;
		keys = newKeys;
	}

	public bool ContainsKey(TKey key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			ref TKey reference = ref key;
			TKey val = default(TKey);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			if (reference.Equals(keys[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void Add(TKey key, TValue value)
	{
		if (count == keys.Length)
		{
			ExpandArrays();
		}
		keys[count] = key;
		values[count++] = value;
	}

	/// <summary>
	/// It is the calling code's responsibility to make sure that key is not null
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public bool TryGetValue(TKey key, out TValue value)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			ref TKey reference = ref key;
			TKey val = default(TKey);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			if (reference.Equals(keys[i]))
			{
				value = values[i];
				return true;
			}
		}
		value = default(TValue);
		return false;
	}

	public void Clear()
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			keys[i] = default(TKey);
			values[i] = default(TValue);
		}
		count = 0;
	}

	[IteratorStateMachine(typeof(FastSmallDictionary<, >._003CGetEnumerator_003Ed__27))]
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__27(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(KeyValuePair<TKey, TValue> item)
	{
		Add(item.Key, item.Value);
	}

	internal void AddIfNotPresent(TKey key, TValue value)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			ref TKey reference = ref key;
			TKey val = default(TKey);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			if (reference.Equals(keys[i]))
			{
				return;
			}
		}
		Add(key, value);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (item.Key.Equals(keys[i]))
			{
				TValue value = values[i];
				if (item.Value == null)
				{
					return value == null;
				}
				return item.Value.Equals(value);
			}
		}
		return false;
	}

	public bool Remove(TKey key)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			ref TKey reference = ref key;
			TKey val = default(TKey);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			if (reference.Equals(keys[i]))
			{
				removeEntry(i);
				return true;
			}
		}
		return false;
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		for (int i = 0; i < keys.Length && i < count; i++)
		{
			if (!item.Key.Equals(keys[i]))
			{
				continue;
			}
			TValue value = values[i];
			if (item.Value == null)
			{
				if (value == null)
				{
					removeEntry(i);
					return true;
				}
				return false;
			}
			if (item.Value.Equals(value))
			{
				removeEntry(i);
				return true;
			}
			return false;
		}
		return false;
	}

	private void removeEntry(int index)
	{
		for (int i = index + 1; i < keys.Length && i < count; i++)
		{
			keys[i - 1] = keys[i];
			values[i - 1] = values[i];
		}
		count--;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		KeyValuePair<TKey, TValue>[] ourArray = new KeyValuePair<TKey, TValue>[count];
		for (int i = 0; i < count; i++)
		{
			ourArray[i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}
		Array.Copy(ourArray, 0, array, arrayIndex, count);
	}
}
