using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

public class ModCompatiblityUtil : ModSystem
{
	public static AssetCategory compatibility;

	public static string[] partiallyWorkingCategories = new string[2] { "shapes", "textures" };

	public static List<string> LoadedModIds { get; private set; } = new List<string>();


	public override double ExecuteOrder()
	{
		return 0.04;
	}

	public override void StartPre(ICoreAPI api)
	{
		compatibility = new AssetCategory("compatibility", AffectsGameplay: true, EnumAppSide.Universal);
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		LoadedModIds = api.ModLoader.Mods.Select((Mod m) => m.Info.ModID).ToList();
		RemapFromCompatbilityFolder(api);
	}

	private void RemapFromCompatbilityFolder(ICoreAPI api)
	{
		int quantityAdded = 0;
		int quantityReplaced = 0;
		foreach (Mod mod in api.ModLoader.Mods)
		{
			string prefix = "compatibility/" + mod.Info.ModID + "/";
			foreach (IAsset asset in api.Assets.GetManyInCategory("compatibility", mod.Info.ModID + "/"))
			{
				AssetLocation origPath = new AssetLocation(mod.Info.ModID, asset.Location.Path.Remove(0, prefix.Length));
				if (api.Assets.AllAssets.ContainsKey(origPath))
				{
					quantityReplaced++;
				}
				else
				{
					quantityAdded++;
				}
				if ((origPath.Category.SideType & api.Side) != 0)
				{
					api.Assets.Add(origPath, asset);
				}
			}
		}
		api.World.Logger.Notification("Compatibility lib: {0} assets added, {1} assets replaced.", quantityAdded, quantityReplaced);
	}
}
