using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class MacroBase : IMacroBase
{
	[JsonProperty]
	public int Index { get; set; }

	[JsonProperty]
	public string Code { get; set; }

	[JsonProperty]
	public string Name { get; set; }

	[JsonProperty]
	public string[] Commands { get; set; }

	[JsonProperty]
	public KeyCombination KeyCombination { get; set; }

	public LoadedTexture iconTexture { get; set; }

	public virtual void GenTexture(ICoreClientAPI capi, int size)
	{
	}

	public MacroBase()
	{
		Code = "";
		Name = "";
		Commands = new string[0];
		KeyCombination = null;
	}
}
