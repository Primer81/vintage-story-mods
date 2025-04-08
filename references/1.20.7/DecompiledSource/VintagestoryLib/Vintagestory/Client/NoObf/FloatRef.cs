namespace Vintagestory.Client.NoObf;

public class FloatRef
{
	internal float value;

	public static FloatRef Create(float value_)
	{
		return new FloatRef
		{
			value = value_
		};
	}

	public float GetValue()
	{
		return value;
	}

	public void SetValue(float value_)
	{
		value = value_;
	}
}
