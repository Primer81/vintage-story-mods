using Vintagestory.API.Client;

namespace Vintagestory.Client;

public interface MouseEventHandler
{
	void OnMouseDown(MouseEvent e);

	void OnMouseUp(MouseEvent e);

	void OnMouseMove(MouseEvent e);

	void OnMouseWheel(MouseWheelEventArgs e);
}
