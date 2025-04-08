using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

public class ResolvedVariant
{
	public OrderedDictionary<string, string> CodeParts = new OrderedDictionary<string, string>();

	public AssetLocation Code;

	public void ResolveCode(AssetLocation baseCode)
	{
		Code = baseCode.Clone();
		foreach (string code in CodeParts.Values)
		{
			if (code.Length > 0)
			{
				AssetLocation code2 = Code;
				code2.Path = code2.Path + "-" + code;
			}
		}
	}
}
