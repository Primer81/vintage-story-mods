using System.Collections;
using System.Collections.Generic;

namespace Vintagestory.API.Datastructures;

/// <summary>
/// Same as your normal c# queue but ensures that every element is contained only once using a Hashset.
/// </summary>
/// <typeparam name="T"></typeparam>
public class UniqueQueue<T> : IEnumerable<T>, IEnumerable
{
	private HashSet<T> hashSet;

	private Queue<T> queue;

	/// <summary>
	/// Amount of elements in the queue
	/// </summary>
	public int Count => hashSet.Count;

	public UniqueQueue()
	{
		hashSet = new HashSet<T>();
		queue = new Queue<T>();
	}

	/// <summary>
	/// Emptys the queue and the hashset
	/// </summary>
	public void Clear()
	{
		hashSet.Clear();
		queue.Clear();
	}

	/// <summary>
	/// Check if given item is contained. Uses the hashset to find the item.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool Contains(T item)
	{
		return hashSet.Contains(item);
	}

	/// <summary>
	/// Adds an item to the queue
	/// </summary>
	/// <param name="item"></param>
	public void Enqueue(T item)
	{
		if (hashSet.Add(item))
		{
			queue.Enqueue(item);
		}
	}

	/// <summary>
	/// Removes an item from the queue
	/// </summary>
	/// <returns></returns>
	public T Dequeue()
	{
		T item = queue.Dequeue();
		hashSet.Remove(item);
		return item;
	}

	/// <summary>
	/// Returns the first item in the queue without removing it.
	/// </summary>
	/// <returns></returns>
	public T Peek()
	{
		return queue.Peek();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return queue.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return queue.GetEnumerator();
	}
}
