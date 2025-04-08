using System.Collections.Generic;

namespace Vintagestory.API.Client;

public interface IGuiComposerManager
{
	Dictionary<string, GuiComposer> Composers { get; }

	void UnfocusElements();
}
