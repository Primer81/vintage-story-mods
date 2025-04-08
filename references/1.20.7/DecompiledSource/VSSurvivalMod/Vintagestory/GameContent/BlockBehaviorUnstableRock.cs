using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockBehaviorUnstableRock : BlockBehavior, IConditionalChiselable
{
	protected AssetLocation fallSound = new AssetLocation("effect/rockslide");

	protected float dustIntensity = 1f;

	protected float impactDamageMul = 1f;

	protected AssetLocation collapsedBlockLoc;

	protected Block collapsedBlock;

	protected float collapseChance = 0.25f;

	protected float maxSupportSearchDistanceSq = 36f;

	protected float maxSupportDistance = 2f;

	protected float maxCollapseDistance = 1f;

	private ICoreServerAPI sapi;

	private ICoreAPI api;

	private bool Enabled
	{
		get
		{
			if (api.World.Config.GetString("caveIns") == "on")
			{
				if (sapi != null)
				{
					return sapi.Server.Config.AllowFallingBlocks;
				}
				return true;
			}
			return false;
		}
	}

	public BlockBehaviorUnstableRock(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		dustIntensity = properties["dustIntensity"].AsFloat(1f);
		collapseChance = properties["collapseChance"].AsFloat(0.25f);
		maxSupportDistance = properties["maxSupportDistance"].AsFloat(2f);
		maxCollapseDistance = properties["maxCollapseDistance"].AsFloat(1f);
		string sound = properties["fallSound"].AsString();
		if (sound != null)
		{
			fallSound = AssetLocation.Create(sound, block.Code.Domain);
		}
		impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
		string str = properties["collapsedBlock"].AsString();
		if (str != null)
		{
			collapsedBlockLoc = AssetLocation.Create(str, block.Code.Domain);
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		sapi = api as ICoreServerAPI;
		this.api = api;
		collapsedBlock = ((collapsedBlockLoc == null) ? block : (api.World.GetBlock(collapsedBlockLoc) ?? block));
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		base.OnBlockBroken(world, pos, byPlayer, ref handling);
		checkCollapsibleNeighbours(world, pos);
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
	{
		base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
		checkCollapsibleNeighbours(world, pos);
	}

	protected void checkCollapsibleNeighbours(IWorldAccessor world, BlockPos pos)
	{
		if (Enabled)
		{
			BlockFacing[] faces = (BlockFacing[])BlockFacing.ALLFACES.Clone();
			GameMath.Shuffle(world.Rand, faces);
			for (int i = 0; i < faces.Length && i < 3 && !CheckCollapsible(world, pos.AddCopy(faces[i])); i++)
			{
			}
		}
	}

	public bool CheckCollapsible(IWorldAccessor world, BlockPos pos)
	{
		if (world.Side != EnumAppSide.Server)
		{
			return false;
		}
		if (!Enabled)
		{
			return false;
		}
		if (!world.BlockAccessor.GetBlock(pos, 1).HasBehavior<BlockBehaviorUnstableRock>())
		{
			return false;
		}
		CollapsibleSearchResult res = searchCollapsible(pos, ignoreBeams: false);
		if (res.Unconnected)
		{
			collapse(world, res.SupportPositions, pos);
		}
		else
		{
			if (world.Rand.NextDouble() + 0.001 > (double)res.Instability)
			{
				return false;
			}
			if (world.Rand.NextDouble() > (double)collapseChance)
			{
				return false;
			}
			collapse(world, res.SupportPositions, pos);
		}
		return true;
	}

	private void collapse(IWorldAccessor world, List<Vec4i> supportPositions, BlockPos startPos)
	{
		List<BlockPos> unstablePositions = getNearestUnstableBlocks(world, supportPositions, startPos);
		if (unstablePositions.Any())
		{
			IOrderedEnumerable<BlockPos> yorderedPositions = unstablePositions.OrderBy((BlockPos pos) => pos.Y);
			int y = yorderedPositions.First().Y;
			collapseLayer(world, yorderedPositions, y);
		}
	}

	private void collapseLayer(IWorldAccessor world, IOrderedEnumerable<BlockPos> yorderedPositions, int y)
	{
		foreach (BlockPos pos in yorderedPositions)
		{
			if (pos.Y < y)
			{
				continue;
			}
			if (pos.Y > y)
			{
				world.Api.Event.RegisterCallback(delegate
				{
					collapseLayer(world, yorderedPositions, pos.Y);
				}, 200);
				return;
			}
			if (world.GetNearestEntity(pos.ToVec3d().Add(0.5, 0.5, 0.5), 1f, 1.5f, (Entity e) => e is EntityBlockFalling entityBlockFalling && entityBlockFalling.initialPos.Equals(pos)) == null)
			{
				BlockBehaviorUnstableRock bh = world.BlockAccessor.GetBlock(pos, 1).GetBehavior<BlockBehaviorUnstableRock>();
				if (bh != null)
				{
					EntityBlockFalling entityblock = new EntityBlockFalling(bh.collapsedBlock, world.BlockAccessor.GetBlockEntity(pos), pos, fallSound, impactDamageMul, canFallSideways: true, dustIntensity);
					world.SpawnEntity(entityblock);
				}
			}
		}
		BlockPos firstpos = yorderedPositions.First();
		for (int i = 0; i < 3; i++)
		{
			checkCollapsibleNeighbours(world, firstpos.AddCopy(world.Rand.Next(17) - 8, 0, world.Rand.Next(17) - 8));
		}
	}

	private CollapsibleSearchResult searchCollapsible(BlockPos startPos, bool ignoreBeams)
	{
		CollapsibleSearchResult searchResult = getNearestVerticalSupports(api.World, startPos);
		searchResult.NearestSupportDistance = 9999f;
		foreach (Vec4i pos in searchResult.SupportPositions)
		{
			searchResult.NearestSupportDistance = Math.Min(searchResult.NearestSupportDistance, GameMath.Sqrt(Math.Max(0f, pos.HorDistanceSqTo(startPos.X, startPos.Z) - (float)(pos.W - 1))));
		}
		if (ignoreBeams)
		{
			searchResult.Instability = Math.Clamp(searchResult.NearestSupportDistance / maxSupportDistance, 0f, 99f);
			return searchResult;
		}
		StartEnd startend;
		double beamDist = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>().GetStableMostBeam(startPos, out startend);
		searchResult.NearestSupportDistance = (float)Math.Min(searchResult.NearestSupportDistance, beamDist);
		searchResult.Instability = Math.Clamp(searchResult.NearestSupportDistance / maxSupportDistance, 0f, 99f);
		return searchResult;
	}

	private float getNearestSupportDistance(List<Vec4i> supportPositions, BlockPos startPos)
	{
		float nearestDist = 99f;
		if (supportPositions.Count == 0)
		{
			return nearestDist;
		}
		foreach (Vec4i pos in supportPositions)
		{
			nearestDist = Math.Min(nearestDist, pos.HorDistanceSqTo(startPos.X, startPos.Z) - (float)(pos.W - 1));
		}
		return GameMath.Sqrt(nearestDist);
	}

	private List<BlockPos> getNearestUnstableBlocks(IWorldAccessor world, List<Vec4i> supportPositions, BlockPos startPos)
	{
		Queue<BlockPos> bfsQueue = new Queue<BlockPos>();
		bfsQueue.Enqueue(startPos);
		HashSet<BlockPos> visited = new HashSet<BlockPos>();
		List<BlockPos> unstableBlocks = new List<BlockPos>();
		int blocksToCollapse = 2 + world.Rand.Next(30) + world.Rand.Next(11) * world.Rand.Next(11);
		int maxy = 1 + world.Rand.Next(3);
		while (bfsQueue.Count > 0)
		{
			BlockPos ipos = bfsQueue.Dequeue();
			if (visited.Contains(ipos))
			{
				continue;
			}
			visited.Add(ipos);
			for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
			{
				BlockPos npos = ipos.AddCopy(BlockFacing.ALLFACES[i]);
				if (npos.HorDistanceSqTo(startPos.X, startPos.Z) > 144f || npos.Y - startPos.Y >= maxy || !world.BlockAccessor.GetBlock(npos, 1).HasBehavior<BlockBehaviorUnstableRock>() || !(getNearestSupportDistance(supportPositions, npos) > 0f))
				{
					continue;
				}
				unstableBlocks.Add(npos);
				for (int dy = 1; dy < 4; dy++)
				{
					if (world.BlockAccessor.GetBlockBelow(npos, dy, 1).HasBehavior<BlockBehaviorUnstableRock>() && getVerticalSupportStrength(world, npos) == 0)
					{
						unstableBlocks.Add(npos.DownCopy(dy));
					}
				}
				if (unstableBlocks.Count > blocksToCollapse)
				{
					return unstableBlocks;
				}
				bfsQueue.Enqueue(npos);
			}
		}
		return unstableBlocks;
	}

	private CollapsibleSearchResult getNearestVerticalSupports(IWorldAccessor world, BlockPos startpos)
	{
		Queue<BlockPos> bfsQueue = new Queue<BlockPos>();
		bfsQueue.Enqueue(startpos);
		HashSet<BlockPos> visited = new HashSet<BlockPos>();
		CollapsibleSearchResult res = new CollapsibleSearchResult();
		res.SupportPositions = new List<Vec4i>();
		int str;
		if ((str = getVerticalSupportStrength(world, startpos)) > 0)
		{
			res.SupportPositions.Add(new Vec4i(startpos, str));
			return res;
		}
		res.Unconnected = true;
		IBlockAccessor blockAccessor = world.BlockAccessor;
		while (bfsQueue.Count > 0)
		{
			BlockPos ipos = bfsQueue.Dequeue();
			if (visited.Contains(ipos))
			{
				continue;
			}
			visited.Add(ipos);
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing face = BlockFacing.HORIZONTALS[i];
				BlockPos npos = ipos.AddCopy(face);
				float distSq = npos.HorDistanceSqTo(startpos.X, startpos.Z);
				Block block = blockAccessor.GetBlock(npos);
				if (block.SideIsSolid(blockAccessor, npos, i) && block.SideIsSolid(blockAccessor, npos, face.Opposite.Index))
				{
					if (distSq > maxSupportSearchDistanceSq)
					{
						res.Unconnected = !block.SideIsSolid(blockAccessor, npos, BlockFacing.DOWN.Index);
					}
					else if ((str = getVerticalSupportStrength(world, npos)) > 0)
					{
						res.Unconnected = false;
						res.SupportPositions.Add(new Vec4i(npos, str));
					}
					else
					{
						bfsQueue.Enqueue(npos);
					}
				}
			}
		}
		return res;
	}

	public static int getVerticalSupportStrength(IWorldAccessor world, BlockPos npos)
	{
		BlockPos tmppos = new BlockPos();
		IBlockAccessor blockAccessor = world.BlockAccessor;
		for (int i = 1; i < 5; i++)
		{
			int y = GameMath.Clamp(npos.Y - i, 0, npos.Y);
			tmppos.Set(npos.X, y, npos.Z);
			Block block = blockAccessor.GetBlock(tmppos);
			int stab = block.Attributes?["unstableRockStabilization"].AsInt() ?? 0;
			if (stab > 0)
			{
				return stab;
			}
			if (!block.SideIsSolid(blockAccessor, tmppos, BlockFacing.UP.Index) || !block.SideIsSolid(blockAccessor, tmppos, BlockFacing.DOWN.Index))
			{
				return 0;
			}
		}
		return 1;
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!Enabled)
		{
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		return Lang.Get("instability-percent", getInstability(pos) * 100.0);
	}

	public double getInstability(BlockPos pos)
	{
		return Math.Clamp(searchCollapsible(pos, ignoreBeams: false).NearestSupportDistance / maxSupportDistance, 0f, 1f);
	}

	public bool CanChisel(IWorldAccessor world, BlockPos pos, IPlayer player, out string errorCode)
	{
		errorCode = null;
		if (!Enabled)
		{
			return true;
		}
		if (getInstability(pos) >= 1.0 && player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			errorCode = "cantchisel-toounstable";
			return false;
		}
		return true;
	}
}
