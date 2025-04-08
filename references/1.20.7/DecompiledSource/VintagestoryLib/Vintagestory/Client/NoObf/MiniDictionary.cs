namespace Vintagestory.Client.NoObf;

public class MiniDictionary
{
	private string[] keys;

	private int[] values;

	public int Count;

	public const int NOTFOUND = -1;

	public int this[string key]
	{
		get
		{
			for (int i = 0; i < keys.Length; i++)
			{
				if (i >= Count)
				{
					return -1;
				}
				if (key == keys[i])
				{
					return values[i];
				}
			}
			return -1;
		}
		set
		{
			for (int i = 0; i < Count; i++)
			{
				if (keys[i] == key)
				{
					values[i] = value;
					return;
				}
			}
			if (Count == keys.Length)
			{
				ExpandArrays();
			}
			keys[Count] = key;
			values[Count++] = value;
		}
	}

	public MiniDictionary(int size)
	{
		keys = new string[size];
		values = new int[size];
	}

	private void ExpandArrays()
	{
		int num = keys.Length + 3;
		string[] newKeys = new string[num];
		int[] newValues = new int[num];
		for (int i = 0; i < keys.Length; i++)
		{
			newKeys[i] = keys[i];
			newValues[i] = values[i];
		}
		values = newValues;
		keys = newKeys;
	}
}
