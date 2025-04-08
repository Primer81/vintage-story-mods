using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class OffthreadBaSupplier : IWorldIntersectionSupplier
{
	private ClientMain game;

	private IBlockAccessor ba;

	public Vec3i MapSize => ba.MapSize;

	public IBlockAccessor blockAccessor => ba;

	public OffthreadBaSupplier(ClientMain game)
	{
		this.game = game;
		ba = game.GetLockFreeBlockAccessor();
	}

	public Block GetBlock(BlockPos pos)
	{
		return ba.GetBlock(pos);
	}

	public bool IsValidPos(BlockPos pos)
	{
		return ba.IsValidPos(pos);
	}

	public Cuboidf[] GetBlockIntersectionBoxes(BlockPos pos)
	{
		return ba.GetBlock(pos).GetSelectionBoxes(ba, pos);
	}

	public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		throw new NotImplementedException();
	}
}
