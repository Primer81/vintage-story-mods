using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Util;

public static class ListExtensions
{
	/// <summary>
	/// Performs a Fisher-Yates shuffle in linear time or O(n)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="rand"></param>
	/// <param name="array"></param>
	public static void Shuffle<T>(this List<T> array, Random rand)
	{
		int j = array.Count;
		while (j > 1)
		{
			int i = rand.Next(j);
			j--;
			T temp = array[j];
			array[j] = array[i];
			array[i] = temp;
		}
	}

	/// <summary>
	/// Performs a Fisher-Yates shuffle in linear time or O(n)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="rand"></param>
	/// <param name="array"></param>
	public static void Shuffle<T>(this List<T> array, IRandom rand)
	{
		int j = array.Count;
		while (j > 1)
		{
			int i = rand.NextInt(j);
			j--;
			T temp = array[j];
			array[j] = array[i];
			array[i] = temp;
		}
	}
}
