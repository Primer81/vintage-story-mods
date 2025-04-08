using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public static class DictionaryExtensions
{
	public static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(this Dictionary<TKey, TValue> source, IRandom rand)
	{
		return source.OrderBy((KeyValuePair<TKey, TValue> x) => rand.NextInt()).ToDictionary((KeyValuePair<TKey, TValue> item) => item.Key, (KeyValuePair<TKey, TValue> item) => item.Value);
	}

	public static Dictionary<TKey, TValue> ShallowClone<TKey, TValue>(this Dictionary<TKey, TValue> source)
	{
		Dictionary<TKey, TValue> cloned = new Dictionary<TKey, TValue>();
		foreach (KeyValuePair<TKey, TValue> val in source)
		{
			cloned[val.Key] = val.Value;
		}
		return cloned;
	}
}
