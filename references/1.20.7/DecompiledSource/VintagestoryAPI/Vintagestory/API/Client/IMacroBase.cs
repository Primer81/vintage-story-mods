namespace Vintagestory.API.Client;

public interface IMacroBase
{
	int Index { get; set; }

	string Code { get; set; }

	string Name { get; set; }

	string[] Commands { get; set; }

	KeyCombination KeyCombination { get; set; }

	LoadedTexture iconTexture { get; set; }

	void GenTexture(ICoreClientAPI capi, int size);
}
