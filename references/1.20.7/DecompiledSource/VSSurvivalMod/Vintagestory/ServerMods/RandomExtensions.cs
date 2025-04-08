using System;

namespace Vintagestory.ServerMods;

internal static class RandomExtensions
{
	public static void Shuffle<T>(this Random rng, T[] array)
	{
		int j = array.Length;
		while (j > 1)
		{
			int i = rng.Next(j--);
			T temp = array[j];
			array[j] = array[i];
			array[i] = temp;
		}
	}
}
