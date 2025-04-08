using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public class ListDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
{
	public ListDictionary()
	{
	}

	public ListDictionary(int capacity)
		: base(capacity)
	{
	}

	public ListDictionary(IEqualityComparer<TKey> comparer)
		: base(comparer)
	{
	}

	public ListDictionary(IDictionary<TKey, List<TValue>> dictionary)
		: base(dictionary)
	{
	}

	public ListDictionary(int capacity, IEqualityComparer<TKey> comparer)
		: base(capacity, comparer)
	{
	}

	public ListDictionary(IDictionary<TKey, List<TValue>> dictionary, IEqualityComparer<TKey> comparer)
		: base(dictionary, comparer)
	{
	}

	public void Add(TKey key, TValue value)
	{
		List<TValue> list = base[key];
		if (list == null)
		{
			list = new List<TValue>();
			Add(key, list);
		}
		list.Add(value);
	}

	public TValue GetEquivalent(TKey key, TValue value)
	{
		foreach (TValue entry in base[key])
		{
			if (entry.Equals(value))
			{
				return entry;
			}
		}
		return default(TValue);
	}

	public bool ContainsValue(TKey key, TValue value)
	{
		return base[key].Contains(value);
	}

	public bool ContainsValue(TValue value)
	{
		foreach (List<TValue> value2 in base.Values)
		{
			if (value2.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	public void ClearKey(TKey key)
	{
		base[key].Clear();
	}

	public bool Remove(TKey key, TValue value)
	{
		return base[key].Remove(value);
	}

	public bool Remove(TValue value)
	{
		foreach (List<TValue> value2 in base.Values)
		{
			if (value2.Remove(value))
			{
				return true;
			}
		}
		return false;
	}

	public TKey GetKeyOfValue(TValue value)
	{
		foreach (TKey key in base.Keys)
		{
			if (base[key].Contains(value))
			{
				return key;
			}
		}
		return default(TKey);
	}
}
