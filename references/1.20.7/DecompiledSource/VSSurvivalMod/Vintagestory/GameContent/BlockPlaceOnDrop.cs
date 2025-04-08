using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPlaceOnDrop : Block
{
	public override void OnGroundIdle(EntityItem entityItem)
	{
		if (entityItem.World.Side == EnumAppSide.Client || entityItem.ShouldDespawn)
		{
			return;
		}
		if (TryPlace(entityItem, 0, 0, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else if (TryPlace(entityItem, 0, 1, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else if (TryPlace(entityItem, 0, -1, 0))
		{
			entityItem.Die(EnumDespawnReason.Removed);
		}
		else
		{
			if (!entityItem.CollidedVertically)
			{
				return;
			}
			List<BlockPos> offsetsList = new List<BlockPos>();
			for (int x = -1; x < 1; x++)
			{
				for (int y = -1; y < 1; y++)
				{
					for (int z = -1; z < 1; z++)
					{
						offsetsList.Add(new BlockPos(x, y, z));
					}
				}
			}
			BlockPos[] offsets = offsetsList.ToArray().Shuffle(entityItem.World.Rand);
			for (int i = 0; i < offsets.Length; i++)
			{
				if (TryPlace(entityItem, offsets[i].X, offsets[i].Y, offsets[i].Z))
				{
					entityItem.Die(EnumDespawnReason.Removed);
					break;
				}
			}
		}
	}

	private bool TryPlace(EntityItem entityItem, int offX, int offY, int offZ)
	{
		IWorldAccessor world = entityItem.World;
		BlockPos bpos = entityItem.ServerPos.AsBlockPos.Add(offX, offY - 1, offZ);
		if (!world.BlockAccessor.GetMostSolidBlock(bpos).CanAttachBlockAt(world.BlockAccessor, this, bpos, BlockFacing.UP))
		{
			return false;
		}
		string useless = "";
		bool num = TryPlaceBlock(world, null, entityItem.Itemstack, new BlockSelection
		{
			Position = bpos,
			Face = BlockFacing.UP,
			HitPosition = new Vec3d(0.5, 1.0, 0.5)
		}, ref useless);
		if (num)
		{
			entityItem.World.PlaySoundAt(entityItem.Itemstack.Block.Sounds?.Place, bpos, -0.5);
		}
		return num;
	}
}
