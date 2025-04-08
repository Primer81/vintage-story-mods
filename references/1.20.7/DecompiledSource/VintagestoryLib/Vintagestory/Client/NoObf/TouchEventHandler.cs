namespace Vintagestory.Client.NoObf;

public abstract class TouchEventHandler
{
	public abstract void OnTouchStart(TouchEventArgs e);

	public abstract void OnTouchMove(TouchEventArgs e);

	public abstract void OnTouchEnd(TouchEventArgs e);
}
