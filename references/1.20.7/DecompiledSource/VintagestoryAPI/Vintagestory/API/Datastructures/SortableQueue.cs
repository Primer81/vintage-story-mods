using System;

namespace Vintagestory.API.Datastructures;

public class SortableQueue<T> where T : IComparable<T>
{
	public int Count;

	public int maxSize = 27;

	protected int tail;

	protected int head;

	protected T[] array;

	public SortableQueue()
	{
		array = new T[maxSize];
	}

	public void Clear()
	{
		Count = 0;
		head = 0;
		tail = 0;
		Array.Clear(array, 0, maxSize);
	}

	public void Enqueue(T v)
	{
		if (Count == maxSize)
		{
			expandArray();
		}
		array[tail++ % maxSize] = v;
		Count++;
	}

	public void EnqueueOrMerge(T v)
	{
		if (head % maxSize > tail % maxSize)
		{
			for (int j = head % maxSize; j < maxSize; j++)
			{
				if (((IMergeable<T>)(object)array[j]).MergeIfEqual(v))
				{
					return;
				}
			}
			for (int i = 0; i < tail % maxSize; i++)
			{
				if (((IMergeable<T>)(object)array[i]).MergeIfEqual(v))
				{
					return;
				}
			}
		}
		else
		{
			for (int k = head % maxSize; k < tail % maxSize; k++)
			{
				if (((IMergeable<T>)(object)array[k]).MergeIfEqual(v))
				{
					return;
				}
			}
		}
		Enqueue(v);
	}

	/// <summary>
	/// Will return invalid data if called when Count &lt;= 0: it is the responsibility of the calling code to check Count &gt; 0
	/// </summary>
	/// <returns></returns>
	public T Dequeue()
	{
		Count--;
		int index = head++ % maxSize;
		T result = array[index];
		array[index] = default(T);
		return result;
	}

	public void Sort()
	{
		if (head % maxSize > tail % maxSize)
		{
			T[] newArray = new T[maxSize];
			int lengthToCopy = maxSize - head % maxSize;
			ArrayCopy(array, head % maxSize, newArray, 0, lengthToCopy);
			ArrayCopy(array, 0, newArray, lengthToCopy, tail % maxSize);
			Array.Clear(array, 0, maxSize);
			array = newArray;
			head = 0;
			tail = tail % maxSize + lengthToCopy;
		}
		int start = head % maxSize;
		Array.Sort(array, start, tail % maxSize - start);
	}

	public void RunForEach(Action<T> action)
	{
		if (head % maxSize > tail % maxSize)
		{
			for (int j = head % maxSize; j < maxSize; j++)
			{
				action(array[j]);
			}
			for (int i = 0; i < tail % maxSize; i++)
			{
				action(array[i]);
			}
		}
		else
		{
			for (int k = head % maxSize; k < tail % maxSize; k++)
			{
				action(array[k]);
			}
		}
	}

	/// <summary>
	/// Will return invalid data if called when Count &lt;= 0: it is the responsibility of the calling code to check Count &gt; 0
	/// </summary>
	/// <returns></returns>
	public T DequeueLIFO()
	{
		Count--;
		T result = array[tail-- % maxSize];
		if (tail < 0)
		{
			tail += maxSize;
		}
		return result;
	}

	private void expandArray()
	{
		head %= maxSize;
		int lengthToCopy = maxSize;
		maxSize = maxSize * 2 + 1;
		T[] newArray = new T[maxSize];
		if (head == 0)
		{
			ArrayCopy(array, 0, newArray, 0, lengthToCopy);
		}
		else
		{
			lengthToCopy -= head;
			ArrayCopy(array, head, newArray, 0, lengthToCopy);
			ArrayCopy(array, 0, newArray, lengthToCopy, head);
		}
		array = newArray;
		head = 0;
		tail = Count;
	}

	private void ArrayCopy(T[] src, int srcOffset, T[] dest, int destOffset, int len)
	{
		if (len > 128)
		{
			Array.Copy(src, srcOffset, dest, destOffset, len);
			return;
		}
		int i = srcOffset;
		int j = destOffset;
		int lim = len / 4 * 4 + srcOffset;
		while (i < lim)
		{
			dest[j] = src[i];
			dest[j + 1] = src[i + 1];
			dest[j + 2] = src[i + 2];
			dest[j + 3] = src[i + 3];
			i += 4;
			j += 4;
		}
		len += srcOffset;
		while (i < len)
		{
			dest[j++] = src[i++];
		}
	}
}
