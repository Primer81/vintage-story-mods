using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFernTree : Block, ITreeGenerator
{
	public Block trunk;

	public Block trunkTopYoung;

	public Block trunkTopMedium;

	public Block trunkTopOld;

	public Block foliage;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api is ICoreServerAPI sapi && Code.Path.Equals("ferntree-normal-trunk"))
		{
			sapi.RegisterTreeGenerator(new AssetLocation("ferntree-normal-trunk"), this);
		}
		if (trunk == null)
		{
			IBlockAccessor blockAccess = api.World.BlockAccessor;
			trunk = blockAccess.GetBlock(new AssetLocation("ferntree-normal-trunk"));
			trunkTopYoung = blockAccess.GetBlock(new AssetLocation("ferntree-normal-trunk-top-young"));
			trunkTopMedium = blockAccess.GetBlock(new AssetLocation("ferntree-normal-trunk-top-medium"));
			trunkTopOld = blockAccess.GetBlock(new AssetLocation("ferntree-normal-trunk-top-old"));
			foliage = blockAccess.GetBlock(new AssetLocation("ferntree-normal-foliage"));
		}
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);
		if (this == foliage)
		{
			for (int i = 0; i < sourceMesh.FlagsCount; i++)
			{
				sourceMesh.Flags[i] = (sourceMesh.Flags[i] & -33546241) | BlockFacing.UP.NormalPackedFlags;
			}
		}
	}

	public string Type()
	{
		return Variant["type"];
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treeGenParams, IRandom rand)
	{
		float f = ((treeGenParams.otherBlockChance == 0f) ? (1f + (float)rand.NextDouble() * 2.5f) : (1.5f + (float)rand.NextDouble() * 4f));
		int quantity = GameMath.RoundRandom(rand, f);
		while (quantity-- > 0)
		{
			GrowOneFern(blockAccessor, pos.UpCopy(), treeGenParams.size, treeGenParams.vinesGrowthChance, rand);
			pos.X += rand.NextInt(8) - 4;
			pos.Z += rand.NextInt(8) - 4;
			bool foundSuitableBlock = false;
			for (int y = 2; y >= -2; y--)
			{
				if (blockAccessor.GetBlockAbove(pos, y).Fertility > 0 && !blockAccessor.GetBlockAbove(pos, y + 1, 2).IsLiquid())
				{
					pos.Y += y;
					foundSuitableBlock = true;
					break;
				}
			}
			if (!foundSuitableBlock)
			{
				break;
			}
		}
	}

	private void GrowOneFern(IBlockAccessor blockAccessor, BlockPos upos, float sizeModifier, float vineGrowthChance, IRandom rand)
	{
		int height = GameMath.Clamp((int)(sizeModifier * (float)(2 + rand.NextInt(6))), 2, 6);
		Block trunkTop = ((height > 2) ? trunkTopOld : trunkTopMedium);
		if (height == 1)
		{
			trunkTop = trunkTopYoung;
		}
		for (int j = 0; j < height; j++)
		{
			Block toplaceblock2 = trunk;
			if (j == height - 1)
			{
				toplaceblock2 = foliage;
			}
			if (j == height - 2)
			{
				toplaceblock2 = trunkTop;
			}
			if (!blockAccessor.GetBlockAbove(upos, j).IsReplacableBy(toplaceblock2))
			{
				return;
			}
		}
		for (int i = 0; i < height; i++)
		{
			Block toplaceblock = trunk;
			if (i == height - 1)
			{
				toplaceblock = foliage;
			}
			if (i == height - 2)
			{
				toplaceblock = trunkTop;
			}
			blockAccessor.SetBlock(toplaceblock.BlockId, upos);
			upos.Up();
		}
	}
}
