namespace Vintagestory.Client;

public class QueueByte
{
	private static int bufferPortionSize = 5242880;

	private byte[] items;

	public int start;

	public int count;

	public int max;

	public QueueByte()
	{
		max = bufferPortionSize;
		items = new byte[max];
	}

	public int GetCount()
	{
		return count;
	}

	public void Enqueue(byte value)
	{
		if (count + 1 >= max)
		{
			byte[] moreitems = new byte[max + bufferPortionSize];
			int origcount = GetCount();
			for (int i = 0; i < origcount; i++)
			{
				moreitems[i] = items[(start + i) % max];
			}
			items = moreitems;
			start = 0;
			count = origcount;
			max += bufferPortionSize;
		}
		int pos = start + count;
		pos %= max;
		count++;
		items[pos] = value;
	}

	public byte Dequeue()
	{
		byte result = items[start];
		start++;
		start %= max;
		count--;
		return result;
	}

	public void DequeueRange(byte[] data, int length)
	{
		for (int i = 0; i < length; i++)
		{
			data[i] = Dequeue();
		}
	}

	internal void PeekRange(byte[] data, int length)
	{
		for (int i = 0; i < length; i++)
		{
			data[i] = items[(start + i) % max];
		}
	}
}
