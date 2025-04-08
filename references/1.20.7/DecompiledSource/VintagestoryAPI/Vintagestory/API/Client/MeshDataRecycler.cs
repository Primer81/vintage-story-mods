using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Vintagestory.API.Client;

/// <summary>
/// This is a recycling system for MeshData objects, so that they can be re-used: helps performance by easing memory allocation pressure, at the cost of holding typically around 300-400MB of memory for these recycled objects
/// </summary>
public class MeshDataRecycler
{
	public const int MinimumSizeForRecycling = 4096;

	public const int TTL = 15000;

	private const int smallLimit = 368;

	private const int mediumLimit = 3072;

	private const int Four = 4;

	private SortedList<float, MeshData> smallSizes = new SortedList<float, MeshData>();

	private SortedList<float, MeshData> mediumSizes = new SortedList<float, MeshData>();

	private SortedList<float, MeshData> largeSizes = new SortedList<float, MeshData>();

	private IClientWorldAccessor game;

	private bool disposed;

	private ConcurrentQueue<MeshData> forRecycling = new ConcurrentQueue<MeshData>();

	private FieldInfo keysAccessor;

	public MeshDataRecycler(IClientWorldAccessor clientMain)
	{
		game = clientMain;
		Type t = typeof(SortedList<float, MeshData>);
		keysAccessor = t.GetField("keys", BindingFlags.Instance | BindingFlags.NonPublic);
	}

	/// <summary>
	/// Gets or creates a MeshData with basic data fields already allocated (may contain junk data) and capacity (VerticesMax) at least equal to minimumVertices; in MeshData created/recycled using this system, IndicesMax will be fixed equal to VerticesMax * 6 / 4
	/// </summary>
	/// <param name="minimumVertices"></param>
	/// <returns></returns>
	public MeshData GetOrCreateMesh(int minimumVertices)
	{
		minimumVertices = (minimumVertices + 4 - 1) / 4 * 4;
		MeshData newMesh = (disposed ? null : GetRecycled(minimumVertices));
		if (newMesh == null)
		{
			if (!disposed)
			{
				minimumVertices = (minimumVertices * 41 / 40 + 4 - 1) / 4 * 4;
			}
			newMesh = new MeshData(minimumVertices);
		}
		else if (newMesh.IndicesMax != newMesh.VerticesMax * 6 / 4)
		{
			newMesh.Indices = new int[newMesh.VerticesMax * 6 / 4];
			newMesh.IndicesMax = newMesh.Indices.Length;
		}
		newMesh.Recyclable = true;
		return newMesh;
	}

	/// <summary>
	/// Call this periodically on the same thread which will call GetOrCreateMesh, this is required to ensure the Recycling system is up to date
	/// </summary>
	public void DoRecycling()
	{
		if (disposed)
		{
			forRecycling.Clear();
			smallSizes.Clear();
			mediumSizes.Clear();
			largeSizes.Clear();
		}
		if (forRecycling.IsEmpty)
		{
			return;
		}
		ControlSizeOfLists();
		MeshData recycled;
		while (!forRecycling.IsEmpty && forRecycling.TryDequeue(out recycled))
		{
			int entrySize = recycled.VerticesMax / 4;
			if (entrySize < 368)
			{
				TryAdd(smallSizes, entrySize, recycled);
			}
			else if (entrySize < 3072)
			{
				TryAdd(mediumSizes, entrySize, recycled);
			}
			else
			{
				TryAdd(largeSizes, entrySize, recycled);
			}
			recycled.RecyclingTime = game.ElapsedMilliseconds;
		}
	}

	/// <summary>
	/// Offer this MeshData to the recycling system: it will first be queued for recycling, and later processed to be either recycled or disposed of
	/// </summary>
	/// <param name="meshData"></param>
	public void Recycle(MeshData meshData)
	{
		if (!disposed)
		{
			forRecycling.Enqueue(meshData);
		}
	}

	/// <summary>
	/// Dispose of the MeshDataRecycler (normally on game exit, but can also be used to disable further use of it)
	/// </summary>
	public void Dispose()
	{
		disposed = true;
	}

	private void ControlSizeOfLists()
	{
		RemoveOldest(smallSizes, 300000);
		RemoveOldest(mediumSizes, 900000);
		RemoveOldest(largeSizes, 2240000);
	}

	private void RemoveOldest(SortedList<float, MeshData> list, int maxSize)
	{
		if (list.Count == 0)
		{
			return;
		}
		int totalsize = 0;
		int indexOldest = 0;
		long timeOldest = list.GetValueAtIndex(0).RecyclingTime;
		int index = 0;
		foreach (KeyValuePair<float, MeshData> entry in list)
		{
			totalsize += (int)entry.Key;
			if (entry.Value.RecyclingTime < timeOldest)
			{
				indexOldest = index;
				timeOldest = entry.Value.RecyclingTime;
			}
			index++;
		}
		if (totalsize > maxSize || timeOldest < game.ElapsedMilliseconds - 15000)
		{
			list.GetValueAtIndex(indexOldest).DisposeBasicData();
			list.RemoveAt(indexOldest);
		}
	}

	private MeshData GetRecycled(int minimumCapacity)
	{
		if (disposed)
		{
			return null;
		}
		int entrySize = minimumCapacity / 4;
		if (entrySize < 368)
		{
			return TryGet(smallSizes, entrySize);
		}
		if (entrySize < 3072)
		{
			return TryGet(mediumSizes, entrySize);
		}
		return TryGet(largeSizes, entrySize);
	}

	private void TryAdd(SortedList<float, MeshData> list, int intkey, MeshData entry)
	{
		float key = intkey;
		while (key < (float)(intkey + 1))
		{
			if (list.TryAdd(key, entry))
			{
				return;
			}
			float newkey = key + 0.25f;
			if (newkey == key)
			{
				newkey = key + 0.5f;
			}
			if (newkey == key)
			{
				break;
			}
			key = newkey;
		}
		entry.DisposeBasicData();
	}

	private MeshData TryGet(SortedList<float, MeshData> list, int entrySize)
	{
		if (list.Count == 0)
		{
			return null;
		}
		int index = Array.BinarySearch((float[])keysAccessor.GetValue(list), 0, list.Count, entrySize, null);
		if (index < 0)
		{
			index = ~index;
			if (index >= list.Count)
			{
				return null;
			}
			if ((int)list.GetKeyAtIndex(index) > entrySize * 5 / 4 + 64)
			{
				return null;
			}
		}
		MeshData valueAtIndex = list.GetValueAtIndex(index);
		list.RemoveAt(index);
		return valueAtIndex;
	}
}
