using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public interface IGuiCompositeHandler
{
	ICoreClientAPI Api { get; }

	GuiComposerManager GuiComposers { get; }

	void LoadComposer(GuiComposer composer);

	bool OnBackPressed();
}
