using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientChunkDataPool : ChunkDataPool
{
	public ClientMain game;

	public override bool ShuttingDown => game.threadsShouldExit;

	public override GameMain Game => game;

	public override ILogger Logger => game.Logger;

	public ClientChunkDataPool(int chunksize, ClientMain game)
	{
		base.chunksize = chunksize;
		BlackHoleData = ClientChunkData.CreateNew(chunksize, this);
		OnlyAirBlocksData = NoChunkData.CreateNew(chunksize);
		this.game = game;
	}

	public override ChunkData Request()
	{
		quantityRequestsSinceLastSlowDispose++;
		return ClientChunkData.CreateNew(chunksize, this);
	}
}
