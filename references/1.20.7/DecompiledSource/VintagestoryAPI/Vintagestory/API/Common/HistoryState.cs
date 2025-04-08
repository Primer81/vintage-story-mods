using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class HistoryState
{
	public BlockUpdate[] BlockUpdates;

	public BlockPos OldStartMarker;

	public BlockPos OldEndMarker;

	public BlockPos NewStartMarker;

	public BlockPos NewEndMarker;

	public Vec3d OldStartMarkerExact;

	public Vec3d OldEndMarkerExact;

	public Vec3d NewStartMarkerExact;

	public Vec3d NewEndMarkerExact;

	public List<EntityUpdate> EntityUpdates;

	public static HistoryState Empty()
	{
		return new HistoryState
		{
			BlockUpdates = Array.Empty<BlockUpdate>()
		};
	}
}
