namespace Vintagestory.Client.NoObf;

public class TouchEventArgs
{
	private int x;

	private int y;

	private int id;

	private bool handled;

	public int GetX()
	{
		return x;
	}

	public void SetX(int value)
	{
		x = value;
	}

	public int GetY()
	{
		return y;
	}

	public void SetY(int value)
	{
		y = value;
	}

	public int GetId()
	{
		return id;
	}

	public void SetId(int value)
	{
		id = value;
	}

	public bool GetHandled()
	{
		return handled;
	}

	public void SetHandled(bool value)
	{
		handled = value;
	}
}
