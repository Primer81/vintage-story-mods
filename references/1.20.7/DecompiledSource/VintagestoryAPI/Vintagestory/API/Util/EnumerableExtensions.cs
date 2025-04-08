using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class EnumerableExtensions
{
	public static int IndexOf<T>(this IEnumerable<T> source, ActionBoolReturn<T> onelem)
	{
		int i = 0;
		foreach (T elem in source)
		{
			if (onelem(elem))
			{
				return i;
			}
			i++;
		}
		return -1;
	}

	public static void Foreach<T>(this IEnumerable<T> array, Action<T> onelement)
	{
		foreach (T val in array)
		{
			onelement(val);
		}
	}

	public static T Nearest<T>(this IEnumerable<T> array, System.Func<T, double> getDistance)
	{
		double nearestDist = double.MaxValue;
		T nearest = default(T);
		foreach (T val in array)
		{
			double d = getDistance(val);
			if (d < nearestDist)
			{
				nearestDist = d;
				nearest = val;
			}
		}
		return nearest;
	}

	public static double NearestDistance<T>(this IEnumerable<T> array, System.Func<T, double> getDistance)
	{
		double nearestDist = double.MaxValue;
		foreach (T val in array)
		{
			double d = getDistance(val);
			if (d < nearestDist)
			{
				nearestDist = d;
			}
		}
		return nearestDist;
	}
}
