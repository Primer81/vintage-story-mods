using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

[DocumentAsJson]
public class RegistryObjectVariantGroup
{
	[DocumentAsJson]
	public AssetLocation LoadFromProperties;

	[DocumentAsJson]
	public AssetLocation[] LoadFromPropertiesCombine;

	[DocumentAsJson]
	public string Code;

	[DocumentAsJson]
	public string[] States;

	[DocumentAsJson]
	public EnumCombination Combine = EnumCombination.Multiply;

	[DocumentAsJson]
	public string OnVariant;

	public string IsValue;
}
