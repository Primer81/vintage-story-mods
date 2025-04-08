using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class AiRuntimeConfig : ModSystem
{
	public static bool RunAiTasks = true;

	public static bool RunAiActivities = true;

	private ICoreServerAPI sapi;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.RegisterGameTickListener(onTick250ms, 250, 31);
	}

	private void onTick250ms(float obj)
	{
		RunAiTasks = sapi.World.Config.GetAsBool("runAiTasks", defaultValue: true);
		RunAiActivities = sapi.World.Config.GetAsBool("runAiActivities", defaultValue: true);
	}
}
