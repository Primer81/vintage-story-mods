namespace Vintagestory.API.Client;

public class MouseButtonState
{
	public bool Left;

	public bool Middle;

	public bool Right;

	public void Clear()
	{
		Left = false;
		Middle = false;
		Right = false;
	}
}
