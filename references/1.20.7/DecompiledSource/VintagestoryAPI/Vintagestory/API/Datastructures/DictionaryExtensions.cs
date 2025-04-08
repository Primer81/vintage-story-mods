using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

public static class DictionaryExtensions
{
	public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key)
	{
		source.TryGetValue(key, out var val);
		return val;
	}
}
