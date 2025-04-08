using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Essentials;

public class PathNode : BlockPos, IEquatable<PathNode>
{
	public float gCost;

	public float hCost;

	public PathNode Parent;

	public int pathLength;

	public EnumTraverseAction Action;

	public float fCost => gCost + hCost;

	public int HeapIndex { get; set; }

	public PathNode(PathNode nearestNode, Cardinal card)
		: base(nearestNode.X + card.Normali.X, nearestNode.Y + card.Normali.Y, nearestNode.Z + card.Normali.Z)
	{
	}

	public PathNode(BlockPos pos)
		: base(pos.X, pos.Y, pos.Z)
	{
	}

	public bool Equals(PathNode other)
	{
		if (other.X == X && other.Y == Y)
		{
			return other.Z == Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PathNode)
		{
			return Equals(obj as PathNode);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool operator ==(PathNode left, PathNode right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(PathNode left, PathNode right)
	{
		return !(left == right);
	}

	public float distanceTo(PathNode node)
	{
		int dx = Math.Abs(node.X - X);
		int dz = Math.Abs(node.Z - Z);
		if (dx <= dz)
		{
			return (float)(dz - dx) + 1.4142137f * (float)dx;
		}
		return (float)(dx - dz) + 1.4142137f * (float)dz;
	}

	public Vec3d ToWaypoint()
	{
		return new Vec3d(X, Y, Z);
	}

	public int CompareTo(PathNode other)
	{
		int compare = fCost.CompareTo(other.fCost);
		if (compare == 0)
		{
			compare = hCost.CompareTo(other.hCost);
		}
		return -compare;
	}
}
