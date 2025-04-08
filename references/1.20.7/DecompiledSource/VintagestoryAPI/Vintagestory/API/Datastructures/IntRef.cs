namespace Vintagestory.API.Datastructures;

public class IntRef
{
	internal int value;

	public static IntRef Create(int value_)
	{
		return new IntRef
		{
			value = value_
		};
	}

	public int GetValue()
	{
		return value;
	}

	public void SetValue(int value_)
	{
		value = value_;
	}
}
