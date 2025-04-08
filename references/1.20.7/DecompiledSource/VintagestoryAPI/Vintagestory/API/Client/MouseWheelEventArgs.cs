namespace Vintagestory.API.Client;

/// <summary>
/// The event arguments for the mouse.
/// </summary>
public class MouseWheelEventArgs
{
	/// <summary>
	/// The rough change in time since last called.
	/// </summary>
	public int delta;

	/// <summary>
	/// The precise change in time since last called.
	/// </summary>
	public float deltaPrecise;

	/// <summary>
	/// The rough change in value.
	/// </summary>
	public int value;

	/// <summary>
	/// The precise change in value.
	/// </summary>
	public float valuePrecise;

	/// <summary>
	/// Is the current event being handled?
	/// </summary>
	public bool IsHandled { get; private set; }

	/// <summary>
	/// Changes or sets the current handled state.
	/// </summary>
	/// <param name="value">Should the event be handled?</param>
	public void SetHandled(bool value = true)
	{
		IsHandled = value;
	}
}
