using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class AStar
{
	protected ICoreServerAPI api;

	protected ICachingBlockAccessor blockAccess;

	public int NodesChecked;

	public double centerOffsetX = 0.5;

	public double centerOffsetZ = 0.5;

	public EnumAICreatureType creatureType;

	protected CollisionTester collTester;

	public PathNodeSet openSet = new PathNodeSet();

	public HashSet<PathNode> closedSet = new HashSet<PathNode>();

	protected readonly Vec3d tmpVec = new Vec3d();

	protected readonly BlockPos tmpPos = new BlockPos();

	protected Cuboidd tmpCub = new Cuboidd();

	public AStar(ICoreServerAPI api)
	{
		this.api = api;
		collTester = new CollisionTester();
		blockAccess = api.World.GetCachingBlockAccessor(synchronize: true, relight: true);
	}

	public virtual List<Vec3d> FindPathAsWaypoints(BlockPos start, BlockPos end, int maxFallHeight, float stepHeight, Cuboidf entityCollBox, int searchDepth = 9999, int mhdistanceTolerance = 0, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		List<PathNode> nodes = FindPath(start, end, maxFallHeight, stepHeight, entityCollBox, searchDepth, mhdistanceTolerance, creatureType);
		if (nodes != null)
		{
			return ToWaypoints(nodes);
		}
		return null;
	}

	public virtual List<PathNode> FindPath(BlockPos start, BlockPos end, int maxFallHeight, float stepHeight, Cuboidf entityCollBox, int searchDepth = 9999, int mhdistanceTolerance = 0, EnumAICreatureType creatureType = EnumAICreatureType.Default)
	{
		if (entityCollBox.XSize > 100f || entityCollBox.YSize > 100f || entityCollBox.ZSize > 100f)
		{
			api.Logger.Warning("AStar:FindPath() was called with a entity box larger than 100 ({0}). Algo not designed for such sizes, likely coding error. Will ignore.", entityCollBox);
			return null;
		}
		this.creatureType = creatureType;
		blockAccess.Begin();
		centerOffsetX = 0.3 + api.World.Rand.NextDouble() * 0.4;
		centerOffsetZ = 0.3 + api.World.Rand.NextDouble() * 0.4;
		NodesChecked = 0;
		PathNode startNode = new PathNode(start);
		PathNode targetNode = new PathNode(end);
		openSet.Clear();
		closedSet.Clear();
		openSet.Add(startNode);
		while (openSet.Count > 0)
		{
			if (NodesChecked++ > searchDepth)
			{
				return null;
			}
			PathNode nearestNode = openSet.RemoveNearest();
			closedSet.Add(nearestNode);
			if (nearestNode == targetNode || (mhdistanceTolerance > 0 && Math.Abs(nearestNode.X - targetNode.X) <= mhdistanceTolerance && Math.Abs(nearestNode.Z - targetNode.Z) <= mhdistanceTolerance && Math.Abs(nearestNode.Y - targetNode.Y) <= mhdistanceTolerance))
			{
				return retracePath(startNode, nearestNode);
			}
			for (int i = 0; i < Cardinal.ALL.Length; i++)
			{
				Cardinal card = Cardinal.ALL[i];
				PathNode neighbourNode = new PathNode(nearestNode, card);
				float extraCost = 0f;
				PathNode existingNeighbourNode = openSet.TryFindValue(neighbourNode);
				if ((object)existingNeighbourNode != null)
				{
					float baseCostToNeighbour = nearestNode.gCost + nearestNode.distanceTo(neighbourNode);
					if (existingNeighbourNode.gCost > baseCostToNeighbour + 0.0001f && traversable(neighbourNode, stepHeight, maxFallHeight, entityCollBox, card, ref extraCost) && existingNeighbourNode.gCost > baseCostToNeighbour + extraCost + 0.0001f)
					{
						UpdateNode(nearestNode, existingNeighbourNode, extraCost);
					}
				}
				else if (!closedSet.Contains(neighbourNode) && traversable(neighbourNode, stepHeight, maxFallHeight, entityCollBox, card, ref extraCost))
				{
					UpdateNode(nearestNode, neighbourNode, extraCost);
					neighbourNode.hCost = neighbourNode.distanceTo(targetNode);
					openSet.Add(neighbourNode);
				}
			}
		}
		return null;
	}

	protected void UpdateNode(PathNode nearestNode, PathNode neighbourNode, float extraCost)
	{
		neighbourNode.gCost = nearestNode.gCost + nearestNode.distanceTo(neighbourNode) + extraCost;
		neighbourNode.Parent = nearestNode;
		neighbourNode.pathLength = nearestNode.pathLength + 1;
	}

	[Obsolete("Deprecated, please use UpdateNode() instead")]
	protected void addIfNearer(PathNode nearestNode, PathNode neighbourNode, PathNode targetNode, HashSet<PathNode> openSet, float extraCost)
	{
		UpdateNode(nearestNode, neighbourNode, extraCost);
	}

	protected bool traversable(PathNode node, float stepHeight, int maxFallHeight, Cuboidf entityCollBox, Cardinal fromDir, ref float extraCost)
	{
		tmpVec.Set((double)node.X + centerOffsetX, node.Y, (double)node.Z + centerOffsetZ);
		Block block;
		if (!collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
		{
			int descended = 0;
			while (true)
			{
				tmpPos.Set(node.X, node.Y - 1, node.Z);
				block = blockAccess.GetBlock(tmpPos, 1);
				if (!block.CanStep)
				{
					return false;
				}
				Block fluidLayerBlock = blockAccess.GetBlock(tmpPos, 2);
				if (fluidLayerBlock.IsLiquid())
				{
					float cost = fluidLayerBlock.GetTraversalCost(tmpPos, creatureType);
					if (cost > 10000f)
					{
						return false;
					}
					extraCost += cost;
					break;
				}
				if (fluidLayerBlock.BlockMaterial == EnumBlockMaterial.Ice)
				{
					block = fluidLayerBlock;
				}
				Cuboidf[] hitboxBelow = block.GetCollisionBoxes(blockAccess, tmpPos);
				if (hitboxBelow != null && hitboxBelow.Length != 0)
				{
					float cost2 = block.GetTraversalCost(tmpPos, creatureType);
					if (cost2 > 10000f)
					{
						return false;
					}
					extraCost += cost2;
					break;
				}
				tmpVec.Y -= 1.0;
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
				descended++;
				node.Y--;
				maxFallHeight--;
				if (maxFallHeight < 0)
				{
					return false;
				}
			}
			if (fromDir.IsDiagnoal)
			{
				tmpVec.Add((float)(-fromDir.Normali.X) / 2f, 0.0, (float)(-fromDir.Normali.Z) / 2f);
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
			}
			tmpPos.Set(node.X, node.Y, node.Z);
			float ucost = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
			if (ucost > 10000f)
			{
				return false;
			}
			extraCost += ucost;
			if (fromDir.IsDiagnoal && creatureType == EnumAICreatureType.Humanoid)
			{
				tmpPos.Set(node.X - fromDir.Normali.X, node.Y, node.Z);
				ucost = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
				extraCost += ucost - 1f;
				if (ucost > 10000f)
				{
					return false;
				}
				tmpPos.Set(node.X, node.Y, node.Z - fromDir.Normali.Z);
				ucost = blockAccess.GetBlock(tmpPos, 2).GetTraversalCost(tmpPos, creatureType);
				extraCost += ucost - 1f;
				if (ucost > 10000f)
				{
					return false;
				}
			}
			return true;
		}
		tmpPos.Set(node.X, node.Y, node.Z);
		block = blockAccess.GetBlock(tmpPos, 4);
		if (!block.CanStep)
		{
			return false;
		}
		float upcost = block.GetTraversalCost(tmpPos, creatureType);
		if (upcost > 10000f)
		{
			return false;
		}
		if (block.Id != 0)
		{
			extraCost += upcost;
		}
		Block lblock = blockAccess.GetBlock(tmpPos, 2);
		upcost = lblock.GetTraversalCost(tmpPos, creatureType);
		if (upcost > 10000f)
		{
			return false;
		}
		if (lblock.Id != 0)
		{
			extraCost += upcost;
		}
		float steponHeightAdjust = -1f;
		Cuboidf[] collboxes = block.GetCollisionBoxes(blockAccess, tmpPos);
		if (collboxes != null && collboxes.Length != 0)
		{
			steponHeightAdjust += collboxes.Max((Cuboidf cuboid) => cuboid.Y2);
		}
		tmpVec.Set((double)node.X + centerOffsetX, (float)node.Y + stepHeight + steponHeightAdjust, (double)node.Z + centerOffsetZ);
		if (!collTester.GetCollidingCollisionBox(blockAccess, entityCollBox, tmpVec, ref tmpCub, alsoCheckTouch: false))
		{
			if (!fromDir.IsDiagnoal)
			{
				node.Y += (int)(1f + steponHeightAdjust);
				return true;
			}
			if (collboxes != null && collboxes.Length != 0)
			{
				tmpVec.Add((float)(-fromDir.Normali.X) / 2f, 0.0, (float)(-fromDir.Normali.Z) / 2f);
				if (collTester.IsColliding(blockAccess, entityCollBox, tmpVec, alsoCheckTouch: false))
				{
					return false;
				}
				node.Y += (int)(1f + steponHeightAdjust);
				return true;
			}
		}
		return false;
	}

	protected List<PathNode> retracePath(PathNode startNode, PathNode endNode)
	{
		int length = endNode.pathLength;
		List<PathNode> path = new List<PathNode>(length);
		for (int j = 0; j < length; j++)
		{
			path.Add(null);
		}
		PathNode currentNode = endNode;
		for (int i = length - 1; i >= 0; i--)
		{
			path[i] = currentNode;
			currentNode = currentNode.Parent;
		}
		return path;
	}

	public List<Vec3d> ToWaypoints(List<PathNode> path)
	{
		List<Vec3d> waypoints = new List<Vec3d>(path.Count + 1);
		for (int i = 1; i < path.Count; i++)
		{
			waypoints.Add(path[i].ToWaypoint().Add(centerOffsetX, 0.0, centerOffsetZ));
		}
		return waypoints;
	}

	public void Dispose()
	{
		blockAccess?.Dispose();
	}
}
