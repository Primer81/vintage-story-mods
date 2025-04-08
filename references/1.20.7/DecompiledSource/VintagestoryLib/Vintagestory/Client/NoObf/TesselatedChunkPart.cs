using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class TesselatedChunkPart
{
	internal int atlasNumber;

	internal MeshData modelDataLod0;

	internal MeshData modelDataLod1;

	internal MeshData modelDataNotLod2Far;

	internal MeshData modelDataLod2Far;

	internal EnumChunkRenderPass pass;

	internal void AddToPools(ChunkRenderer cr, List<ModelDataPoolLocation> locations, Vec3i chunkOrigin, int dimension, Sphere boundingSphere, Bools cullVisible)
	{
		bool isTransparentDimension = dimension == 1 && BlockAccessorMovable.IsTransparent(chunkOrigin);
		MeshDataPoolManager pools = cr.poolsByRenderPass[isTransparentDimension ? Math.Max(3, (int)pass) : ((int)pass)][atlasNumber];
		if (modelDataLod0 != null)
		{
			cr.SetInterleaveStrides(modelDataLod0, pass);
			ModelDataPoolLocation location = pools.AddModel(modelDataLod0, chunkOrigin, dimension, boundingSphere);
			if (location != null)
			{
				location.CullVisible = cullVisible;
				locations.Add(location);
			}
		}
		if (modelDataLod1 != null)
		{
			cr.SetInterleaveStrides(modelDataLod1, pass);
			ModelDataPoolLocation location = pools.AddModel(modelDataLod1, chunkOrigin, dimension, boundingSphere);
			if (location != null)
			{
				location.CullVisible = cullVisible;
				location.LodLevel = 1;
				locations.Add(location);
			}
		}
		if (modelDataNotLod2Far != null)
		{
			cr.SetInterleaveStrides(modelDataNotLod2Far, pass);
			ModelDataPoolLocation location = pools.AddModel(modelDataNotLod2Far, chunkOrigin, dimension, boundingSphere);
			if (location != null)
			{
				location.CullVisible = cullVisible;
				location.LodLevel = 2;
				locations.Add(location);
			}
		}
		if (modelDataLod2Far != null)
		{
			cr.SetInterleaveStrides(modelDataLod2Far, pass);
			ModelDataPoolLocation location = pools.AddModel(modelDataLod2Far, chunkOrigin, dimension, boundingSphere);
			if (location != null)
			{
				location.CullVisible = cullVisible;
				location.LodLevel = 3;
				locations.Add(location);
			}
		}
		Dispose();
	}

	internal void Dispose()
	{
		modelDataLod0?.Dispose();
		modelDataLod1?.Dispose();
		modelDataNotLod2Far?.Dispose();
		modelDataLod2Far?.Dispose();
	}
}
