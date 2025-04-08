using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLocustNest : Block
{
	public Block[] DecoBlocksCeiling;

	public Block[] DecoBlocksFloor;

	public Block[] DecorBlocksWall;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		DecorBlocksWall = new Block[1] { api.World.GetBlock(new AssetLocation("oxidation-rust-normal")) };
		DecoBlocksCeiling = new Block[7]
		{
			api.World.GetBlock(new AssetLocation("locustnest-cage")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none-upsidedown")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-main1")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-small1")),
			api.World.GetBlock(new AssetLocation("locustnest-stalagmite-small2"))
		};
		DecoBlocksFloor = new Block[10]
		{
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-none")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-tiny")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-small")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-small")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-medium")),
			api.World.GetBlock(new AssetLocation("locustnest-metalspike-large"))
		};
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(world, blockPos, byItemStack);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (itemStack != null && (itemStack.Attributes?.GetBool("spawnOnlyAfterImport")).GetValueOrDefault())
		{
			return base.GetHeldItemName(itemStack) + " " + Lang.Get("(delayed spawn)");
		}
		return base.GetHeldItemName(itemStack);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		ItemStack itemstack = inSlot.Itemstack;
		if (itemstack != null && (itemstack.Attributes?.GetBool("spawnOnlyAfterImport")).GetValueOrDefault())
		{
			dsc.AppendLine(Lang.Get("Spawns locust nests only after import/world generation"));
		}
	}

	public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
	{
		(api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLocustNest)?.OnBlockBreaking();
		return base.OnGettingBroken(player, blockSel, itemslot, remainingResistance, dt, counter);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockId(pos.X, pos.Y, pos.Z) != 0)
		{
			return false;
		}
		if (blockAccessor.GetTerrainMapheightAt(pos) - pos.Y < 30 || pos.Y < 25)
		{
			return false;
		}
		BlockPos cavepos = getSemiLargeCavePos(blockAccessor, pos);
		if (cavepos == null)
		{
			return false;
		}
		int dy;
		for (dy = 0; dy < 15 && !blockAccessor.IsSideSolid(cavepos.X, cavepos.Y + dy, cavepos.Z, BlockFacing.UP); dy++)
		{
		}
		if (dy >= 15)
		{
			return false;
		}
		blockAccessor.SetBlock(BlockId, cavepos.AddCopy(0, dy, 0));
		if (EntityClass != null)
		{
			blockAccessor.SpawnBlockEntity(EntityClass, cavepos.AddCopy(0, dy, 0));
		}
		BlockPos tmppos = new BlockPos();
		int tries = 55 + worldGenRand.NextInt(55);
		while (tries-- > 0)
		{
			int offX = worldGenRand.NextInt(15) - 7;
			int offY = worldGenRand.NextInt(15) - 7;
			int offZ = worldGenRand.NextInt(15) - 7;
			if (worldGenRand.NextDouble() < 0.4)
			{
				if (offX != 0 || offZ != 0 || offY > dy)
				{
					tryPlaceDecoUp(tmppos.Set(cavepos.X + offX, cavepos.Y + offY, cavepos.Z + offZ), blockAccessor, worldGenRand);
				}
			}
			else if (offX != 0 || offZ != 0 || offY < dy)
			{
				tryPlaceDecoDown(tmppos.Set(cavepos.X + offX, cavepos.Y + offY, cavepos.Z + offZ), blockAccessor, worldGenRand);
			}
		}
		blockAccessor.WalkBlocks(pos.AddCopy(-7, -7, -7), pos.AddCopy(7, 7, 7), delegate(Block block, int x, int y, int z)
		{
			if (block.Replaceable < 6000 && !(api.World.Rand.NextDouble() < 0.5))
			{
				for (int i = 0; i < 6; i++)
				{
					if (block.SideSolid[i])
					{
						BlockFacing blockFacing = BlockFacing.ALLFACES[i];
						if (blockAccessor.GetBlock(x + blockFacing.Normali.X, y + blockFacing.Normali.Y, z + blockFacing.Normali.Z).Id == 0)
						{
							blockAccessor.SetDecor(DecorBlocksWall[0], tmppos.Set(x, y, z), blockFacing);
							if (api.World.Rand.NextDouble() < 0.5)
							{
								break;
							}
						}
					}
				}
			}
		});
		return true;
	}

	private void tryPlaceDecoDown(BlockPos blockPos, IBlockAccessor blockAccessor, IRandom worldGenRand)
	{
		if (blockAccessor.GetBlock(blockPos).Id != 0)
		{
			return;
		}
		int tries = 7;
		while (tries-- > 0)
		{
			blockPos.Y--;
			if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.UP.Index])
			{
				blockPos.Y++;
				blockAccessor.SetBlock(DecoBlocksFloor[worldGenRand.NextInt(DecoBlocksFloor.Length)].BlockId, blockPos);
				break;
			}
		}
	}

	private void tryPlaceDecoUp(BlockPos blockPos, IBlockAccessor blockAccessor, IRandom worldgenRand)
	{
		if (blockAccessor.GetBlock(blockPos).Id != 0)
		{
			return;
		}
		int tries = 7;
		while (tries-- > 0)
		{
			blockPos.Y++;
			if (blockAccessor.GetBlock(blockPos).SideSolid[BlockFacing.DOWN.Index])
			{
				blockPos.Y--;
				Block placeblock = DecoBlocksCeiling[worldgenRand.NextInt(DecoBlocksCeiling.Length)];
				blockAccessor.SetBlock(placeblock.BlockId, blockPos);
				break;
			}
		}
	}

	private BlockPos getSemiLargeCavePos(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockPos outpos = pos.Copy();
		int maxY = pos.Y;
		int minY = pos.Y;
		int minX = pos.X;
		int maxX = pos.X;
		int minZ = pos.Z;
		int maxZ = pos.Z;
		while (pos.Y - minY < 12 && blockAccessor.GetBlockId(pos.X, minY - 1, pos.Z) == 0)
		{
			minY--;
		}
		for (; maxY - pos.Y < 12 && blockAccessor.GetBlockId(pos.X, maxY + 1, pos.Z) == 0; maxY++)
		{
		}
		outpos.Y = (maxY + minY) / 2;
		if (maxY - minY < 4 || maxY - minY >= 10)
		{
			return null;
		}
		while (pos.X - minX < 12 && blockAccessor.GetBlockId(minX - 1, pos.Y, pos.Z) == 0)
		{
			minX--;
		}
		for (; maxX - pos.X < 12 && blockAccessor.GetBlockId(maxX + 1, pos.Y, pos.Z) == 0; maxX++)
		{
		}
		if (maxX - minX < 3)
		{
			return null;
		}
		outpos.X = (maxX + minX) / 2;
		while (pos.Z - minZ < 12 && blockAccessor.GetBlockId(pos.X, pos.Y, minZ - 1) == 0)
		{
			minZ--;
		}
		for (; maxZ - pos.Z < 12 && blockAccessor.GetBlockId(pos.X, pos.Y, maxZ + 1) == 0; maxZ++)
		{
		}
		if (maxZ - minZ < 3)
		{
			return null;
		}
		outpos.Z = (maxZ + minZ) / 2;
		return outpos;
	}
}
