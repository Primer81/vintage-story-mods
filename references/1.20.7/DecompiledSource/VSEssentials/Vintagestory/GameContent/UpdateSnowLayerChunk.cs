using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class UpdateSnowLayerChunk : IEquatable<UpdateSnowLayerChunk>
{
	public Vec2i Coords;

	public double LastSnowAccumUpdateTotalHours;

	public Dictionary<BlockPos, BlockIdAndSnowLevel> SetBlocks = new Dictionary<BlockPos, BlockIdAndSnowLevel>();

	public bool Equals(UpdateSnowLayerChunk other)
	{
		return other.Coords.Equals(Coords);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is UpdateSnowLayerChunk { Coords: var pos }))
		{
			return false;
		}
		if (Coords.X == pos.X)
		{
			return Coords.Y == pos.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 23 + Coords.X.GetHashCode()) * 23 + Coords.Y.GetHashCode();
	}
}
