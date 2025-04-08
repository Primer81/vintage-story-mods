using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class TargetSet
{
	internal HashSet<AssetLocationAndSource> shapelocations = new HashSet<AssetLocationAndSource>();

	internal HashSet<AssetLocationAndSource> objlocations = new HashSet<AssetLocationAndSource>();

	internal HashSet<AssetLocationAndSource> gltflocations = new HashSet<AssetLocationAndSource>();

	internal volatile bool finished;

	public void Add(CompositeShape shape, string message, AssetLocation sourceLoc, int alternateNo = -1)
	{
		HashSet<AssetLocationAndSource> obj = ((shape.Format == EnumShapeFormat.Obj) ? objlocations : ((shape.Format == EnumShapeFormat.GltfEmbedded) ? gltflocations : shapelocations));
		AssetLocationAndSource loc = new AssetLocationAndSource(shape.Base, message, sourceLoc);
		obj.Add(loc);
	}
}
