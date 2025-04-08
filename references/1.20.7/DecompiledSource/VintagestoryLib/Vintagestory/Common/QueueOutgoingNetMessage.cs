namespace Vintagestory.Common;

public class QueueOutgoingNetMessage
{
	private byte[][] packets;

	private int count;

	private int itemsSize;

	public QueueOutgoingNetMessage()
	{
		packets = new byte[1][];
		itemsSize = 1;
		count = 0;
	}

	internal int Count()
	{
		return count;
	}

	internal byte[] Dequeue()
	{
		byte[] ret = packets[0];
		for (int i = 0; i < count - 1; i++)
		{
			packets[i] = packets[i + 1];
		}
		count--;
		return ret;
	}

	internal void Enqueue(byte[] data)
	{
		if (count == itemsSize)
		{
			byte[][] grownMessageQueue = new byte[itemsSize * 2][];
			for (int i = 0; i < itemsSize; i++)
			{
				grownMessageQueue[i] = packets[i];
			}
			itemsSize *= 2;
			packets = grownMessageQueue;
		}
		packets[count++] = data;
	}
}
