namespace Vintagestory.Common;

public class QueueIncomingNetMessage
{
	private NetIncomingMessage[] items;

	private int count;

	private int itemsSize;

	public QueueIncomingNetMessage()
	{
		items = new NetIncomingMessage[1];
		itemsSize = 1;
		count = 0;
	}

	internal int Count()
	{
		return count;
	}

	internal NetIncomingMessage Dequeue()
	{
		NetIncomingMessage ret = items[0];
		for (int i = 0; i < count - 1; i++)
		{
			items[i] = items[i + 1];
		}
		count--;
		return ret;
	}

	internal void Enqueue(NetIncomingMessage p)
	{
		if (count == itemsSize)
		{
			NetIncomingMessage[] items2 = new NetIncomingMessage[itemsSize * 2];
			for (int i = 0; i < itemsSize; i++)
			{
				items2[i] = items[i];
			}
			itemsSize *= 2;
			items = items2;
		}
		items[count++] = p;
	}
}
