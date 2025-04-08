using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBamboo : Block, ITreeGenerator, ICustomTreeFellingBehavior
{
	public const int MaxPlantHeight = 15;

	private Block greenSeg1;

	private Block greenSeg2;

	private Block greenSeg3;

	private Block brownSeg1;

	private Block brownSeg2;

	private Block brownSeg3;

	private Block brownLeaves;

	private Block greenLeaves;

	private static Random rand = new Random();

	private bool isSegmentWithLeaves;

	private Block greenShootBlock;

	private Block brownShootBlock;

	private IBlockAccessor lockFreeBa;

	private Dictionary<int, int[]> windModeByFlagCount = new Dictionary<int, int[]>();

	private Vec3i windDir = new Vec3i();

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			lockFreeBa = api.World.GetLockFreeBlockAccessor();
		}
		if (api is ICoreServerAPI sapi)
		{
			if (Code.Path.Equals("bamboo-grown-green-segment1"))
			{
				sapi.RegisterTreeGenerator(new AssetLocation("bamboo-grown-green"), this);
			}
			if (Code.Path.Equals("bamboo-grown-brown-segment1"))
			{
				sapi.RegisterTreeGenerator(new AssetLocation("bamboo-grown-brown"), this);
			}
		}
		if (greenSeg1 == null)
		{
			IBlockAccessor blockAccess = api.World.BlockAccessor;
			greenSeg1 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-green-segment1"));
			greenSeg2 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-green-segment2"));
			greenSeg3 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-green-segment3"));
			brownSeg1 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-brown-segment1"));
			brownSeg2 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-brown-segment2"));
			brownSeg3 = blockAccess.GetBlock(new AssetLocation("bamboo-grown-brown-segment3"));
			brownLeaves = blockAccess.GetBlock(new AssetLocation("bambooleaves-brown-grown"));
			greenLeaves = blockAccess.GetBlock(new AssetLocation("bambooleaves-green-grown"));
			greenShootBlock = blockAccess.GetBlock(new AssetLocation("sapling-greenbambooshoots-free"));
			brownShootBlock = blockAccess.GetBlock(new AssetLocation("sapling-brownbambooshoots-free"));
		}
		if (RandomDrawOffset > 0)
		{
			JsonObject overrider = Attributes?["overrideRandomDrawOffset"];
			if (overrider != null && overrider.Exists)
			{
				RandomDrawOffset = overrider.AsInt(1);
			}
		}
		isSegmentWithLeaves = LastCodePart() == "segment2" || LastCodePart() == "segment3";
	}

	public string Type()
	{
		return LastCodePart(1);
	}

	public Block NextSegment(IBlockAccessor blockAccess)
	{
		string part = LastCodePart();
		if (!(Type() == "green"))
		{
			if (!(part == "segment1"))
			{
				if (!(part == "segment2"))
				{
					return null;
				}
				return brownSeg3;
			}
			return brownSeg2;
		}
		if (!(part == "segment1"))
		{
			if (!(part == "segment2"))
			{
				return null;
			}
			return greenSeg3;
		}
		return greenSeg2;
	}

	public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, TreeGenParams treegenParams, IRandom random)
	{
		float f = ((treegenParams.otherBlockChance == 0f) ? (3f + (float)rand.NextDouble() * 6f) : ((3f + (float)rand.NextDouble() * 4f) * 3f * 3f));
		int quantity = GameMath.RoundRandom(rand, f);
		BlockPos npos = pos.Copy();
		float sizeModifier = GameMath.Mix(treegenParams.size, 1f, 0.5f);
		sizeModifier *= 1f + (float)rand.NextDouble() * 0.5f;
		while (quantity-- > 0)
		{
			float dist = Math.Max(1f, pos.DistanceTo(npos) - 2f);
			GrowStalk(blockAccessor, npos.UpCopy(), dist, sizeModifier, treegenParams.vinesGrowthChance);
			npos.Set(pos);
			npos.X += rand.Next(8) - 4;
			npos.Z += rand.Next(8) - 4;
			bool foundSuitableBlock = false;
			for (int y = 2; y >= -2; y--)
			{
				if (blockAccessor.GetBlock(npos.X, npos.Y + y, npos.Z).Fertility > 0)
				{
					npos.Y += y;
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

	private void GrowStalk(IBlockAccessor blockAccessor, BlockPos upos, float centerDist, float sizeModifier, float vineGrowthChance)
	{
		Block block = this;
		float heightf = (float)(8 + rand.Next(5)) * sizeModifier;
		heightf = Math.Max(1f, heightf - centerDist);
		int height = (int)heightf;
		int nextSegmentAtHeight = height / 3;
		BlockPos npos = upos.Copy();
		BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
		foreach (BlockFacing face in hORIZONTALS)
		{
			if (rand.NextDouble() > 0.75)
			{
				BlockPos bpos2 = npos.Set(upos).Add(face);
				Block shootBlock = ((block == greenSeg3) ? greenShootBlock : brownShootBlock);
				if (blockAccessor.GetBlock(bpos2).Replaceable >= shootBlock.Replaceable && blockAccessor.GetBlock(bpos2.X, bpos2.Y - 1, bpos2.Z).Fertility > 0 && blockAccessor.GetBlock(bpos2, 2).BlockId == 0)
				{
					blockAccessor.SetBlock(shootBlock.BlockId, bpos2);
				}
			}
		}
		if (height < 4)
		{
			block = ((BlockBamboo)block).NextSegment(blockAccessor);
		}
		for (int i = 0; i < height; i++)
		{
			if (!blockAccessor.GetBlock(upos).IsReplacableBy(block))
			{
				break;
			}
			blockAccessor.SetBlock(block.BlockId, upos);
			if (nextSegmentAtHeight <= i)
			{
				block = ((BlockBamboo)block).NextSegment(blockAccessor);
				nextSegmentAtHeight += height / 3;
			}
			if (block == null)
			{
				break;
			}
			if (block == greenSeg3 || block == brownSeg3)
			{
				Block blockLeaves = ((block == greenSeg3) ? greenLeaves : brownLeaves);
				hORIZONTALS = BlockFacing.ALLFACES;
				foreach (BlockFacing facing in hORIZONTALS)
				{
					if (facing == BlockFacing.DOWN)
					{
						continue;
					}
					float chanceFac = ((facing == BlockFacing.UP) ? 0f : 0.25f);
					if (!(rand.NextDouble() > (double)chanceFac))
					{
						continue;
					}
					npos.Set(upos.X + facing.Normali.X, upos.Y + facing.Normali.Y, upos.Z + facing.Normali.Z);
					if (rand.NextDouble() > 0.33)
					{
						BlockPos bpos = npos.DownCopy();
						if (blockAccessor.GetBlock(bpos).Replaceable >= blockLeaves.Replaceable)
						{
							blockAccessor.SetBlock(blockLeaves.BlockId, bpos);
						}
					}
					if (blockAccessor.GetBlock(npos).Replaceable < blockLeaves.Replaceable)
					{
						continue;
					}
					blockAccessor.SetBlock(blockLeaves.BlockId, npos);
					BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
					foreach (BlockFacing facing2 in aLLFACES)
					{
						if (rand.NextDouble() > 0.5)
						{
							npos.Set(upos.X + facing.Normali.X + facing2.Normali.X, upos.Y + facing.Normali.Y + facing2.Normali.Y, upos.Z + facing.Normali.Z + facing2.Normali.Z);
							if (blockAccessor.GetBlock(npos).Replaceable >= blockLeaves.Replaceable)
							{
								blockAccessor.SetBlock(blockLeaves.BlockId, npos);
							}
							break;
						}
					}
				}
			}
			upos.Up();
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (!isSegmentWithLeaves || LastCodePart() != "segment3")
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		if (Textures == null || Textures.Count == 0)
		{
			return 0;
		}
		if (!Textures.TryGetValue(facing.Code, out var tex))
		{
			tex = Textures.First().Value;
		}
		if (tex?.Baked == null)
		{
			return 0;
		}
		int color = capi.BlockTextureAtlas.GetRandomColor(tex.Baked.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", SeasonColorMap, color, pos.X, pos.Y, pos.Z);
	}

	public override void OnDecalTesselation(IWorldAccessor world, MeshData decalMesh, BlockPos pos)
	{
		bool enableWind = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) >= 14;
		windDir.X = Math.Sign(GlobalConstants.CurrentWindSpeedClient.X);
		windDir.Z = 0;
		applyWindSwayToMesh(decalMesh, enableWind, pos, windDir);
		base.OnDecalTesselation(world, decalMesh, pos);
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		bool enableWind = ((lightRgbsByCorner[24] >> 24) & 0xFF) >= 159;
		windDir.X = Math.Sign(GlobalConstants.CurrentWindSpeedClient.X);
		windDir.Z = 0;
		applyWindSwayToMesh(sourceMesh, enableWind, pos, windDir);
	}

	private void applyWindSwayToMesh(MeshData sourceMesh, bool enableWind, BlockPos pos, Vec3i windDir)
	{
		if (!windModeByFlagCount.TryGetValue(sourceMesh.FlagsCount, out var origFlags))
		{
			int[] array2 = (windModeByFlagCount[sourceMesh.FlagsCount] = new int[sourceMesh.FlagsCount]);
			origFlags = array2;
			for (int i = 0; i < origFlags.Length; i++)
			{
				origFlags[i] = sourceMesh.Flags[i] & 0x1E000000;
			}
		}
		bool sideDisableWindWaveDown = false;
		Block nblock = lockFreeBa.GetBlock(pos.X, pos.Y - 1, pos.Z);
		if (nblock.VertexFlags.WindMode == EnumWindBitMode.NoWind && nblock.SideSolid[4])
		{
			sideDisableWindWaveDown = true;
		}
		else if (nblock is BlockBamboo)
		{
			nblock = lockFreeBa.GetBlock(pos.X + windDir.X, pos.Y - 1, pos.Z + windDir.Z);
			if (nblock.VertexFlags.WindMode == EnumWindBitMode.NoWind && nblock.SideSolid[3])
			{
				sideDisableWindWaveDown = true;
			}
		}
		int groundOffset = 1;
		nblock = lockFreeBa.GetBlock(pos.X + windDir.X, pos.Y, pos.Z + windDir.Z);
		if (nblock.VertexFlags.WindMode == EnumWindBitMode.NoWind && nblock.SideSolid[3])
		{
			enableWind = false;
		}
		if (enableWind)
		{
			bool bambooLeavesFound = isSegmentWithLeaves;
			bool continuousBambooCane = true;
			for (; groundOffset < 8; groundOffset++)
			{
				Block block = api.World.BlockAccessor.GetBlockBelow(pos, groundOffset);
				Block blockInWindDir = ((block is BlockBamboo) ? api.World.BlockAccessor.GetBlock(pos.X + windDir.X, pos.Y - groundOffset, pos.Z + windDir.Z) : null);
				if ((block.VertexFlags.WindMode == EnumWindBitMode.NoWind && block.SideSolid[4]) || (blockInWindDir != null && blockInWindDir.VertexFlags.WindMode == EnumWindBitMode.NoWind && blockInWindDir.SideSolid[3]))
				{
					break;
				}
				if (blockInWindDir == null)
				{
					continuousBambooCane = false;
				}
				if (!bambooLeavesFound && continuousBambooCane && block is BlockBamboo { isSegmentWithLeaves: not false })
				{
					bambooLeavesFound = true;
				}
			}
			int y = pos.Y;
			while (!bambooLeavesFound && y - pos.Y < 15)
			{
				Block block = api.World.BlockAccessor.GetBlock(pos.X, ++y, pos.Z);
				if (block is BlockBamboo bam)
				{
					bambooLeavesFound = bam.isSegmentWithLeaves;
					continue;
				}
				if (block is BlockWithLeavesMotion)
				{
					bambooLeavesFound = true;
				}
				break;
			}
			if (!bambooLeavesFound)
			{
				enableWind = false;
			}
		}
		int clearFlags = 33554431;
		int verticesCount = sourceMesh.VerticesCount;
		if (!enableWind)
		{
			for (int vertexNum = 0; vertexNum < verticesCount; vertexNum++)
			{
				sourceMesh.Flags[vertexNum] &= clearFlags;
			}
			return;
		}
		for (int vertexNum2 = 0; vertexNum2 < verticesCount; vertexNum2++)
		{
			int flag = sourceMesh.Flags[vertexNum2] & clearFlags;
			float fy = sourceMesh.xyz[vertexNum2 * 3 + 1];
			if (fy > 0.05f || !sideDisableWindWaveDown)
			{
				flag |= origFlags[vertexNum2] | (GameMath.Clamp(groundOffset + ((fy < 0.95f) ? (-1) : 0), 0, 7) << 29);
			}
			sourceMesh.Flags[vertexNum2] = flag;
		}
	}

	public EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex)
	{
		return EnumTreeFellingBehavior.ChopSpreadVertical;
	}
}
