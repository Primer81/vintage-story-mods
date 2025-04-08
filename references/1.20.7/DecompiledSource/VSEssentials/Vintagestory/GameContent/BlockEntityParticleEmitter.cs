using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockEntityParticleEmitter : BlockEntity
{
	private Block block;

	private long listenerId;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.World is IClientWorldAccessor)
		{
			listenerId = api.Event.RegisterGameTickListener(OnGameTick, 25);
			block = api.World.BlockAccessor.GetBlock(Pos);
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		Api.World.UnregisterGameTickListener(listenerId);
	}

	private void OnGameTick(float dt)
	{
		if (((IClientWorldAccessor)Api.World).Player.Entity.Pos.InRangeOf(Pos, 16384f) && block != null && block.ParticleProperties != null)
		{
			for (int i = 0; i < block.ParticleProperties.Length; i++)
			{
				AdvancedParticleProperties bps = block.ParticleProperties[i];
				bps.basePos.X = (float)Pos.X + block.TopMiddlePos.X;
				bps.basePos.Y = (float)Pos.InternalY + block.TopMiddlePos.Y;
				bps.basePos.Z = (float)Pos.Z + block.TopMiddlePos.Z;
				Api.World.SpawnParticles(bps);
			}
		}
	}
}
