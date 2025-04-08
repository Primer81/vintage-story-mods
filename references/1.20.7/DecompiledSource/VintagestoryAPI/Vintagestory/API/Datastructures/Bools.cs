namespace Vintagestory.API.Datastructures;

public class Bools
{
	private int data;

	public bool this[int i]
	{
		get
		{
			return (data & (1 << i)) != 0;
		}
		set
		{
			if (value)
			{
				data |= 1 << i;
			}
			else
			{
				data &= ~(1 << i);
			}
		}
	}

	public Bools(bool a, bool b)
	{
		data = (b ? 2 : 0) + (a ? 1 : 0);
	}

	internal bool Parity()
	{
		return (data + 1) / 2 != 1;
	}
}
