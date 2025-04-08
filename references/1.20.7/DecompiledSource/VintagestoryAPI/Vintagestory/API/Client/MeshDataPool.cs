using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// This is a modeldata pool, which can hold 400k vertices, 300k indices but not more than 900 chunks
/// </summary>
public class MeshDataPool
{
	/// <summary>
	/// The maximum parts for this pool.
	/// </summary>
	public int MaxPartsPerPool;

	/// <summary>
	/// The current vertices for this pool.
	/// </summary>
	public int VerticesPoolSize;

	/// <summary>
	/// the amount of indicies for this pool.
	/// </summary>
	public int IndicesPoolSize;

	internal MeshRef modelRef;

	internal int poolId;

	internal List<ModelDataPoolLocation> poolLocations = new List<ModelDataPoolLocation>();

	/// <summary>
	/// The starting byte for each index.
	/// </summary>
	public int[] indicesStartsByte;

	/// <summary>
	/// The size of each index.
	/// </summary>
	public int[] indicesSizes;

	/// <summary>
	/// How many index groups are there.
	/// </summary>
	public int indicesGroupsCount;

	/// <summary>
	/// the position of the indices.
	/// </summary>
	public int indicesPosition;

	/// <summary>
	/// the position of the vertices.
	/// </summary>
	public int verticesPosition;

	/// <summary>
	/// The current fragmentaton.
	/// </summary>
	public float CurrentFragmentation;

	/// <summary>
	/// How many of the vertices are used.
	/// </summary>
	public int UsedVertices;

	internal Vec3i poolOrigin;

	internal int dimensionId;

	/// <summary>
	/// How many triangles are rendered.
	/// </summary>
	public int RenderedTriangles;

	/// <summary>
	/// How many triangles are allocated.
	/// </summary>
	public int AllocatedTris;

	private MeshDataPool(int verticesPoolSize, int indicesPoolSize, int maxPartsPerPool)
	{
		MaxPartsPerPool = maxPartsPerPool;
		IndicesPoolSize = indicesPoolSize;
		VerticesPoolSize = verticesPoolSize;
	}

	/// <summary>
	/// Allocates a new pool for mesh data.
	/// </summary>
	/// <param name="capi">The core client API</param>
	/// <param name="verticesPoolSize">The vertices pool size.</param>
	/// <param name="indicesPoolSize">The index pool size.</param>
	/// <param name="maxPartsPerPool">The maximum parts per pool.</param>
	/// <param name="customFloats">The custom floats of the pool.</param>
	/// <param name="customShorts"></param>
	/// <param name="customBytes">The custom bytes of the pool.</param>
	/// <param name="customInts">The custom ints of the pool.</param>
	/// <returns>The resulting mesh data pool.</returns>
	public static MeshDataPool AllocateNewPool(ICoreClientAPI capi, int verticesPoolSize, int indicesPoolSize, int maxPartsPerPool, CustomMeshDataPartFloat customFloats = null, CustomMeshDataPartShort customShorts = null, CustomMeshDataPartByte customBytes = null, CustomMeshDataPartInt customInts = null)
	{
		MeshDataPool obj = new MeshDataPool(verticesPoolSize, indicesPoolSize, maxPartsPerPool)
		{
			indicesStartsByte = new int[maxPartsPerPool * 2],
			indicesSizes = new int[maxPartsPerPool]
		};
		customFloats?.SetAllocationSize(verticesPoolSize * customFloats.InterleaveStride / 4);
		customShorts?.SetAllocationSize(verticesPoolSize * customShorts.InterleaveStride / 2);
		customBytes?.SetAllocationSize(verticesPoolSize * customBytes.InterleaveStride);
		customInts?.SetAllocationSize(verticesPoolSize * customInts.InterleaveStride / 4);
		obj.modelRef = capi.Render.AllocateEmptyMesh(12 * verticesPoolSize, 0, 8 * verticesPoolSize, 4 * verticesPoolSize, 4 * verticesPoolSize, 4 * indicesPoolSize, customFloats, customShorts, customBytes, customInts, EnumDrawMode.Triangles, staticDraw: false);
		return obj;
	}

	/// <summary>
	/// Attempts to add the new model.
	/// </summary>
	/// <param name="capi">The core client API</param>
	/// <param name="modeldata">The model to add</param>
	/// <param name="modelOrigin">The origin point of the model.</param>
	/// <param name="dimension"></param>
	/// <param name="frustumCullSphere">The culling sphere.</param>
	/// <returns>The location of the model (and the data) in the pool.</returns>
	public ModelDataPoolLocation TryAdd(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, int dimension, Sphere frustumCullSphere)
	{
		if (poolLocations.Count >= MaxPartsPerPool || dimension != dimensionId)
		{
			return null;
		}
		if (poolOrigin != null)
		{
			if (poolLocations.Count == 0)
			{
				poolOrigin.Set(modelOrigin);
			}
			if (modelOrigin.SquareDistanceTo(poolOrigin) > 25000000)
			{
				return null;
			}
		}
		if (CurrentFragmentation > 0.03f)
		{
			ModelDataPoolLocation location = TrySqueezeInbetween(capi, modeldata, modelOrigin, frustumCullSphere);
			if (location != null)
			{
				return location;
			}
		}
		return TryAppend(capi, modeldata, modelOrigin, frustumCullSphere);
	}

	private ModelDataPoolLocation TrySqueezeInbetween(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere)
	{
		int curVertexPos = 0;
		int curIndexPos = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation location = poolLocations[i];
			if (location.IndicesStart - curIndexPos > modeldata.IndicesCount && location.VerticesStart - curVertexPos > modeldata.VerticesCount)
			{
				return InsertAt(capi, modeldata, modelOrigin, frustumCullSphere, curIndexPos, curVertexPos, i);
			}
			curIndexPos = location.IndicesEnd + 1;
			curVertexPos = location.VerticesEnd + 1;
		}
		return null;
	}

	private ModelDataPoolLocation TryAppend(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere)
	{
		if (modeldata.IndicesCount + indicesPosition >= IndicesPoolSize || modeldata.VerticesCount + verticesPosition >= VerticesPoolSize)
		{
			return null;
		}
		ModelDataPoolLocation result = InsertAt(capi, modeldata, modelOrigin, frustumCullSphere, indicesPosition, verticesPosition, -1);
		indicesPosition += modeldata.IndicesCount;
		verticesPosition += modeldata.VerticesCount;
		return result;
	}

	private ModelDataPoolLocation InsertAt(ICoreClientAPI capi, MeshData modeldata, Vec3i modelOrigin, Sphere frustumCullSphere, int indexPosition, int vertexPosition, int listPosition)
	{
		if (vertexPosition > 0)
		{
			for (int j = 0; j < modeldata.IndicesCount; j++)
			{
				modeldata.Indices[j] += vertexPosition;
			}
		}
		if (poolOrigin != null)
		{
			int dx = modelOrigin.X - poolOrigin.X;
			int dy = modelOrigin.Y - poolOrigin.Y;
			int dz = modelOrigin.Z - poolOrigin.Z;
			for (int i = 0; i < modeldata.VerticesCount; i++)
			{
				modeldata.xyz[3 * i] += dx;
				modeldata.xyz[3 * i + 1] += dy;
				modeldata.xyz[3 * i + 2] += dz;
			}
		}
		modeldata.XyzOffset = vertexPosition * 12;
		modeldata.NormalsOffset = vertexPosition * 4;
		modeldata.RgbaOffset = vertexPosition * 4;
		modeldata.Rgba2Offset = vertexPosition * 4;
		modeldata.UvOffset = vertexPosition * 8;
		modeldata.FlagsOffset = vertexPosition * 4;
		modeldata.IndicesOffset = indexPosition * 4;
		if (modeldata.CustomFloats != null)
		{
			modeldata.CustomFloats.BaseOffset = vertexPosition * modeldata.CustomFloats.InterleaveStride;
		}
		if (modeldata.CustomBytes != null)
		{
			modeldata.CustomBytes.BaseOffset = vertexPosition * modeldata.CustomBytes.InterleaveStride;
		}
		if (modeldata.CustomInts != null)
		{
			modeldata.CustomInts.BaseOffset = vertexPosition * modeldata.CustomInts.InterleaveStride;
		}
		capi.Render.UpdateMesh(modelRef, modeldata);
		ModelDataPoolLocation poolLocation = new ModelDataPoolLocation
		{
			IndicesStart = indexPosition,
			IndicesEnd = indexPosition + modeldata.IndicesCount,
			VerticesStart = vertexPosition,
			VerticesEnd = vertexPosition + modeldata.VerticesCount,
			PoolId = poolId,
			FrustumCullSphere = frustumCullSphere
		};
		if (listPosition != -1)
		{
			poolLocations.Insert(listPosition, poolLocation);
		}
		else
		{
			poolLocations.Add(poolLocation);
		}
		CalcFragmentation();
		return poolLocation;
	}

	/// <summary>
	/// Attempts to remove the model from the pool if the model exists.  Will throw an invalid call or an InvalidOperationException if used improperly.
	/// </summary>
	/// <param name="location">The location of the model data.</param>
	public void RemoveLocation(ModelDataPoolLocation location)
	{
		if (location.PoolId != poolId)
		{
			throw new Exception("invalid call");
		}
		if (!poolLocations.Remove(location))
		{
			throw new InvalidOperationException("Tried to remove mesh that does not exist. This shouldn't happen");
		}
		if (poolLocations.Count == 0)
		{
			indicesPosition = 0;
			verticesPosition = 0;
		}
		else if (location.IndicesEnd == indicesPosition && location.VerticesEnd == verticesPosition)
		{
			indicesPosition = poolLocations[poolLocations.Count - 1].IndicesEnd;
			verticesPosition = poolLocations[poolLocations.Count - 1].VerticesEnd;
		}
		CalcFragmentation();
	}

	/// <summary>
	/// Draw the model.
	/// </summary>
	/// <param name="capi">The core client API</param>
	/// <param name="frustumCuller">The area where models can be viewed from the camera.</param>
	/// <param name="frustumCullMode">The mode of the culling.</param>
	public void Draw(ICoreClientAPI capi, FrustumCulling frustumCuller, EnumFrustumCullMode frustumCullMode)
	{
		FrustumCull(frustumCuller, frustumCullMode);
		capi.Render.RenderMesh(modelRef, indicesStartsByte, indicesSizes, indicesGroupsCount);
	}

	/// <summary>
	/// Cleans up the rendering view of the models.
	/// </summary>
	/// <param name="frustumCuller">The area where models can be viewed from the camera.</param>
	/// <param name="frustumCullMode">The mode of the culling.</param>
	public void FrustumCull(FrustumCulling frustumCuller, EnumFrustumCullMode frustumCullMode)
	{
		indicesGroupsCount = 0;
		RenderedTriangles = 0;
		AllocatedTris = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation location = poolLocations[i];
			int size = location.IndicesEnd - location.IndicesStart;
			if (location.IsVisible(frustumCullMode, frustumCuller))
			{
				indicesStartsByte[indicesGroupsCount * 2] = location.IndicesStart * 4;
				indicesSizes[indicesGroupsCount] = size;
				RenderedTriangles += size / 3;
				indicesGroupsCount++;
			}
			AllocatedTris += size / 3;
		}
	}

	public void SetFullyVisible()
	{
		indicesGroupsCount = 0;
		RenderedTriangles = 0;
		AllocatedTris = 0;
		for (int i = 0; i < poolLocations.Count; i++)
		{
			ModelDataPoolLocation location = poolLocations[i];
			int size = location.IndicesEnd - location.IndicesStart;
			indicesStartsByte[indicesGroupsCount * 2] = location.IndicesStart * 4;
			indicesSizes[indicesGroupsCount] = size;
			RenderedTriangles += size / 3;
			indicesGroupsCount++;
			AllocatedTris += size / 3;
		}
	}

	/// <summary>
	/// Is this an empty pool.
	/// </summary>
	/// <returns>true if the pool is empty.</returns>
	public bool IsEmpty()
	{
		return poolLocations.Count == 0;
	}

	/// <summary>
	/// Disposes of the current mesh pool.
	/// </summary>
	/// <param name="capi">The core client API</param>
	public void Dispose(ICoreClientAPI capi)
	{
		capi.Render.DeleteMesh(modelRef);
	}

	/// <summary>
	/// Calculates the current fragmentation of the mesh.
	/// </summary>
	public void CalcFragmentation()
	{
		int curPos = 0;
		int unusedVertices = 0;
		UsedVertices = 0;
		if (verticesPosition == 0)
		{
			CurrentFragmentation = 0f;
			return;
		}
		foreach (ModelDataPoolLocation location in poolLocations)
		{
			UsedVertices += location.VerticesEnd - location.VerticesStart;
			unusedVertices += Math.Max(0, location.VerticesStart - curPos);
			curPos = location.VerticesEnd + 1;
		}
		CurrentFragmentation = (float)unusedVertices / (float)verticesPosition;
	}

	/// <summary>
	/// Gets the current fragmentation of the pool.
	/// </summary>
	/// <returns></returns>
	public float GetFragmentation()
	{
		return CurrentFragmentation;
	}

	public void RenderMesh(IRenderAPI render)
	{
		render.RenderMesh(modelRef, indicesStartsByte, indicesSizes, indicesGroupsCount);
	}
}
