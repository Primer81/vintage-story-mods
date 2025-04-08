using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemDormancyStateChecker : ModSystem
{
	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	private void on1stick(float dt)
	{
	}
}
