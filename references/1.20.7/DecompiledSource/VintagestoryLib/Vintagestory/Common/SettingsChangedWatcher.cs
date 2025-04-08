using Vintagestory.API.Client;

namespace Vintagestory.Common;

public class SettingsChangedWatcher<T>
{
	internal OnSettingsChanged<T> handler;

	internal string key;
}
