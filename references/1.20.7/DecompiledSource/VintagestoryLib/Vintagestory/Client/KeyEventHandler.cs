using Vintagestory.API.Client;

namespace Vintagestory.Client;

public interface KeyEventHandler
{
	void OnKeyDown(KeyEvent e);

	void OnKeyPress(KeyEvent e);

	void OnKeyUp(KeyEvent e);
}
