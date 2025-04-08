using System.Collections.Generic;

namespace Vintagestory.API.Client;

public interface IMacroManager
{
	SortedDictionary<int, IMacroBase> MacrosByIndex { get; set; }

	void DeleteMacro(int macroIndex);

	void LoadMacros();

	bool RunMacro(int macroIndex, IClientWorldAccessor world);

	bool SaveMacro(int macroIndex);

	void SetMacro(int macroIndex, IMacroBase macro);
}
