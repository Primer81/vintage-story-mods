using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class BlockStalagSection : Block
{
	public string[] Thicknesses = new string[6] { "14", "12", "10", "08", "06", "04" };

	public int ThicknessInt;

	public Dictionary<string, int> thicknessIndex = new Dictionary<string, int>
	{
		{ "14", 0 },
		{ "12", 1 },
		{ "10", 2 },
		{ "08", 3 },
		{ "06", 4 },
		{ "04", 5 }
	};

	public string Thickness => Variant["thickness"];

	public override void OnLoaded(ICoreAPI api)
	{
		CanStep = false;
		ThicknessInt = int.Parse(Variant["thickness"]);
		base.OnLoaded(api);
	}

	public Block GetBlock(IWorldAccessor world, string rocktype, string thickness)
	{
		return world.GetBlock(CodeWithParts(rocktype, thickness));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (IsSurroundedByNonSolid(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public bool IsSurroundedByNonSolid(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos neighborPos = pos.AddCopy(facing.Normali);
			Block neighborBlock = world.BlockAccessor.GetBlock(neighborPos);
			if (neighborBlock.SideSolid[facing.Opposite.Index] || neighborBlock is BlockStalagSection)
			{
				return false;
			}
		}
		return true;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		bool didplace = false;
		if (blockAccessor.GetBlock(pos).Replaceable < 6000)
		{
			return false;
		}
		pos = pos.Copy();
		ModStdWorldGen modSys = null;
		if (blockAccessor is IWorldGenBlockAccessor wgba)
		{
			modSys = wgba.WorldgenWorldAccessor.Api.ModLoader.GetModSystem<GenVegetationAndPatches>();
		}
		for (int i = 0; i < 5 + worldGenRand.NextInt(25); i++)
		{
			if (pos.Y >= 15 && (modSys == null || modSys.GetIntersectingStructure(pos, ModStdWorldGen.SkipStalagHashCode) == null))
			{
				didplace |= TryGenStalag(blockAccessor, pos, worldGenRand.NextInt(4), worldGenRand);
				pos.X += worldGenRand.NextInt(9) - 4;
				pos.Y += worldGenRand.NextInt(3) - 1;
				pos.Z += worldGenRand.NextInt(9) - 4;
			}
		}
		return didplace;
	}

	private bool TryGenStalag(IBlockAccessor blockAccessor, BlockPos pos, int thickOff, IRandom worldGenRand)
	{
		bool didplace = false;
		for (int dy2 = 0; dy2 < 5; dy2++)
		{
			Block block2 = blockAccessor.GetBlockAbove(pos, dy2, 1);
			if (block2.SideSolid[BlockFacing.DOWN.Index] && block2.BlockMaterial == EnumBlockMaterial.Stone)
			{
				if (block2.Variant.TryGetValue("rock", out var rocktype2))
				{
					GrowDownFrom(blockAccessor, pos.AddCopy(0, dy2 - 1, 0), rocktype2, thickOff, worldGenRand);
					didplace = true;
				}
				break;
			}
			if (block2.Id != 0)
			{
				break;
			}
		}
		if (!didplace)
		{
			return false;
		}
		for (int dy = 0; dy < 12; dy++)
		{
			Block block = blockAccessor.GetBlockBelow(pos, dy, 1);
			if (block.SideSolid[BlockFacing.UP.Index] && block.BlockMaterial == EnumBlockMaterial.Stone)
			{
				if (block.Variant.TryGetValue("rock", out var rocktype))
				{
					GrowUpFrom(blockAccessor, pos.AddCopy(0, -dy + 1, 0), rocktype, thickOff);
					didplace = true;
				}
				break;
			}
			if (block.Id != 0 && !(block is BlockStalagSection))
			{
				break;
			}
		}
		return didplace;
	}

	private void GrowUpFrom(IBlockAccessor blockAccessor, BlockPos pos, string rocktype, int thickOff)
	{
		for (int i = thicknessIndex[Thickness] + thickOff; i < Thicknesses.Length; i++)
		{
			BlockStalagSection stalagBlock = (BlockStalagSection)GetBlock(api.World, rocktype, Thicknesses[i]);
			if (stalagBlock != null)
			{
				Block block = blockAccessor.GetBlock(pos);
				if (block.Replaceable < 6000 && !((block as BlockStalagSection)?.ThicknessInt < stalagBlock.ThicknessInt))
				{
					break;
				}
				blockAccessor.SetBlock(stalagBlock.BlockId, pos);
				pos.Y++;
			}
		}
	}

	private void GrowDownFrom(IBlockAccessor blockAccessor, BlockPos pos, string rocktype, int thickOff, IRandom worldGenRand)
	{
		for (int i = thicknessIndex[Thickness] + thickOff + worldGenRand.NextInt(2); i < Thicknesses.Length; i++)
		{
			Block stalagBlock = GetBlock(api.World, rocktype, Thicknesses[i]);
			if (stalagBlock != null)
			{
				if (blockAccessor.GetBlock(pos).Replaceable < 6000)
				{
					break;
				}
				blockAccessor.SetBlock(stalagBlock.BlockId, pos);
				pos.Y--;
			}
		}
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		return Lang.Get("block-speleothem", Lang.Get("rock-" + Variant["rock"]));
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		return Lang.Get("block-speleothem", Lang.Get("rock-" + Variant["rock"]));
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		dsc.AppendLine();
		dsc.AppendLine(Lang.Get("rock-" + Variant["rock"]));
	}
}
