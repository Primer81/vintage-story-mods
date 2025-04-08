using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class AiTaskTargetableAt : AiTaskBaseTargetable
{
	public Vec3d SpawnPos;

	public Vec3d CenterPos;

	protected AiTaskTargetableAt(EntityAgent entity)
		: base(entity)
	{
	}

	public override void OnEntityLoaded()
	{
		loadOrCreateSpawnPos();
	}

	public override void OnEntitySpawn()
	{
		loadOrCreateSpawnPos();
	}

	public void loadOrCreateSpawnPos()
	{
		if (entity.WatchedAttributes.HasAttribute("spawnPosX"))
		{
			SpawnPos = new Vec3d(entity.WatchedAttributes.GetDouble("spawnPosX"), entity.WatchedAttributes.GetDouble("spawnPosY"), entity.WatchedAttributes.GetDouble("spawnPosZ"));
			return;
		}
		SpawnPos = entity.ServerPos.XYZ;
		entity.WatchedAttributes.SetDouble("spawnPosX", SpawnPos.X);
		entity.WatchedAttributes.SetDouble("spawnPosY", SpawnPos.Y);
		entity.WatchedAttributes.SetDouble("spawnPosZ", SpawnPos.Z);
	}
}
