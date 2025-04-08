using System;
using System.Collections.Generic;
using System.Threading;

namespace Vintagestory.Common;

/// <summary>
/// A single object to allow the arrays in ConcurrentSmallDictionary to be replaced atomically.
/// Keys, once entered in the keys array within a DTable, are invariable: if we ever need to remove a key we will create a new DTable
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class DTable<TKey, TValue>
{
	public readonly TKey[] keys;

	public readonly TValue[] values;

	public volatile int count;

	private volatile int countBleedingEdge;

	public DTable(int capacity)
	{
		keys = new TKey[capacity];
		values = new TValue[capacity];
		count = 0;
	}

	/// <summary>
	/// Special constructor for a new array with a single, first, entry
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public DTable(TKey key, TValue value)
	{
		int capacity = 4;
		keys = new TKey[capacity];
		values = new TValue[capacity];
		int count = 0;
		keys[count] = key;
		values[count] = value;
		this.count = count + 1;
		countBleedingEdge = count + 1;
	}

	/// <summary>
	/// Special constructor which copies the old arrays and adds one new entry at the end - all will be atomic when the Dictionary replaces contents with the results of this
	/// </summary>
	/// <param name="old"></param>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public DTable(DTable<TKey, TValue> old, TKey key, TValue value)
	{
		int capacity = old.values.Length;
		capacity = ((capacity <= 5) ? 8 : ((capacity * 3 / 2 + 1) / 2 * 2));
		keys = new TKey[capacity];
		values = new TValue[capacity];
		int count = old.count;
		CopyArray(old.keys, keys, 0, 0, count);
		CopyArray(old.values, values, 0, 0, count);
		keys[count] = key;
		values[count] = value;
		this.count = count + 1;
		countBleedingEdge = count + 1;
	}

	/// <summary>
	/// Special constructor which copies the old arrays and removes one entry
	/// </summary>
	/// <param name="old"></param>
	/// <param name="toRemove">the index of the entry to remove</param>
	public DTable(DTable<TKey, TValue> old, int toRemove)
	{
		int capacity = old.values.Length;
		keys = new TKey[capacity];
		values = new TValue[capacity];
		int count = old.count;
		if (toRemove >= count)
		{
			toRemove = count;
			CopyArray(old.keys, keys, 0, 0, toRemove);
			CopyArray(old.values, values, 0, 0, toRemove);
			this.count = count;
			countBleedingEdge = count;
		}
		else
		{
			CopyArray(old.keys, keys, 0, 0, toRemove);
			CopyArray(old.keys, keys, toRemove + 1, toRemove, count);
			CopyArray(old.values, values, 0, 0, toRemove);
			CopyArray(old.values, values, toRemove + 1, toRemove, count);
			this.count = count - 1;
			countBleedingEdge = count - 1;
		}
	}

	private void CopyArray<T>(T[] source, T[] dest, int sourceStart, int destStart, int sourceEnd)
	{
		if (sourceEnd - sourceStart < 32)
		{
			int dAdjust = destStart - sourceStart;
			for (int i = sourceStart; i < source.Length && i < sourceEnd; i++)
			{
				dest[i + dAdjust] = source[i];
			}
		}
		else
		{
			Array.Copy(source, sourceStart, dest, destStart, sourceEnd - sourceStart);
		}
	}

	internal TValue GetValue(TKey key)
	{
		TKey[] keys = this.keys;
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

	internal TValue TryGetValue(TKey key)
	{
		TKey[] keys = this.keys;
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
		return default(TValue);
	}

	internal bool TryGetValue(TKey key, out TValue value)
	{
		TKey[] keys = this.keys;
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

	internal bool ContainsKey(TKey key)
	{
		TKey[] keys = this.keys;
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

	internal int IndexOf(TKey key)
	{
		TKey[] keys = this.keys;
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
				return i;
			}
		}
		return -1;
	}

	internal int IndexOf(TKey key, TValue value)
	{
		TKey[] keys = this.keys;
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
				ref TValue reference2 = ref value;
				TValue val2 = default(TValue);
				if (val2 == null)
				{
					val2 = reference2;
					reference2 = ref val2;
				}
				if (reference2.Equals(values[i]))
				{
					return i;
				}
				return -1;
			}
		}
		return -1;
	}

	/// <summary>
	/// Returns true if key exists (in which case the value at that key is replaced), otherwise returns false
	/// </summary>
	/// <param name="key"></param>
	/// <param name="newValue"></param>
	/// <returns>true if successful replacement, otherwise false</returns>
	internal bool ReplaceIfKeyExists(TKey key, TValue newValue)
	{
		TKey[] keys = this.keys;
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
				values[i] = newValue;
				return true;
			}
		}
		return false;
	}

	internal ICollection<TKey> KeysCopy()
	{
		TKey[] result = new TKey[count];
		if (result.Length < 32)
		{
			TKey[] source = keys;
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = source[i];
			}
		}
		else
		{
			Array.Copy(keys, result, result.Length);
		}
		return result;
	}

	internal ICollection<TValue> ValuesCopy()
	{
		TValue[] result = new TValue[count];
		if (result.Length < 32)
		{
			TValue[] source = values;
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = source[i];
			}
		}
		else
		{
			Array.Copy(values, result, result.Length);
		}
		return result;
	}

	/// <summary>
	/// Add the key value pair to the table.  Fails (adds nothing and returns false) if the table is full
	/// </summary>
	internal bool Add(TKey key, TValue value)
	{
		int pos;
		for (pos = countBleedingEdge; Interlocked.CompareExchange(ref countBleedingEdge, pos + 1, pos) != pos; pos++)
		{
		}
		if (pos >= values.Length)
		{
			return false;
		}
		keys[pos] = key;
		values[pos] = value;
		Interlocked.Increment(ref count);
		return true;
	}

	internal void DuplicateKeyCheck(TKey key)
	{
		TKey[] keys = this.keys;
		bool keyPresentAlready = false;
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
				if (keyPresentAlready)
				{
					val = key;
					throw new InvalidOperationException("ConcurrentSmallDictionary was written to with the same key '" + val?.ToString() + "' in two different threads, we can't handle that!");
				}
				keyPresentAlready = true;
			}
		}
	}

	internal void CopyTo(KeyValuePair<TKey, TValue>[] dest, int destIndex)
	{
		int limit = count;
		if (limit > dest.Length - destIndex)
		{
			limit = dest.Length - destIndex;
		}
		for (int i = 0; i < keys.Length && i < limit; i++)
		{
			dest[i + destIndex] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
		}
	}
}
