using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public interface IGameSettingsHandler : IGuiCompositeHandler
{
	int? MaxViewDistanceAlarmValue { get; }

	bool IsIngame { get; }

	bool LeaveSettingsMenu();

	void ReloadShaders();

	GuiComposer dialogBase(string name, double width = -1.0, double height = -1.0);

	void OnMacroEditor();
}
