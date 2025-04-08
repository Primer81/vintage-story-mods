using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class DisableRecipeRegisteringSystem : ModSystem
{
	public override double ExecuteOrder()
	{
		return 99999.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		RecipeRegistrySystem.canRegister = false;
	}
}
