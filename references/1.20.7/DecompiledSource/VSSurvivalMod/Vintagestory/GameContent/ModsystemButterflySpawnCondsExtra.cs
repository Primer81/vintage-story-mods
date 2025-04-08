using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModsystemButterflySpawnCondsExtra : ModSystem
{
	private ICoreServerAPI sapi;

	private BlockPos tmpPos = new BlockPos();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		sapi.Event.OnTrySpawnEntity += Event_OnTrySpawnEntity;
	}

	private bool Event_OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
	{
		if (properties.Server.SpawnConditions?.Runtime?.MaxQuantityByGroup?.Code.Path == "butterfly-*")
		{
			tmpPos.Set((int)spawnPosition.X, (int)spawnPosition.Y, (int)spawnPosition.Z);
			if (blockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, sapi.World.Calendar.TotalDays).Temperature < 0f)
			{
				return false;
			}
		}
		return true;
	}
}
