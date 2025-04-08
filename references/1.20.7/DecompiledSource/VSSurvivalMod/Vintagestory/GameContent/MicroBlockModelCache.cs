using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class MicroBlockModelCache : ModSystem
{
	private Dictionary<long, CachedModel> cachedModels = new Dictionary<long, CachedModel>();

	private long nextMeshId = 1L;

	private ICoreClientAPI capi;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.RegisterGameTickListener(OnSlowTick, 1000);
	}

	private void OnSlowTick(float dt)
	{
		List<long> toDelete = new List<long>();
		foreach (KeyValuePair<long, CachedModel> val in cachedModels)
		{
			val.Value.Age += 1f;
			if (val.Value.Age > 180f)
			{
				toDelete.Add(val.Key);
			}
		}
		foreach (long key in toDelete)
		{
			cachedModels[key].MeshRef.Dispose();
			cachedModels.Remove(key);
		}
	}

	public MultiTextureMeshRef GetOrCreateMeshRef(ItemStack forStack)
	{
		long meshid = forStack.Attributes.GetLong("meshId", 0L);
		if (!cachedModels.ContainsKey(meshid))
		{
			MultiTextureMeshRef meshref = CreateModel(forStack);
			forStack.Attributes.SetLong("meshId", nextMeshId);
			cachedModels[nextMeshId++] = new CachedModel
			{
				MeshRef = meshref,
				Age = 0f
			};
			return meshref;
		}
		cachedModels[meshid].Age = 0f;
		return cachedModels[meshid].MeshRef;
	}

	private MultiTextureMeshRef CreateModel(ItemStack forStack)
	{
		ITreeAttribute tree = forStack.Attributes;
		if (tree == null)
		{
			tree = new TreeAttribute();
		}
		int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, capi.World);
		uint[] cuboids = (tree["cuboids"] as IntArrayAttribute)?.AsUint;
		if (cuboids == null)
		{
			cuboids = (tree["cuboids"] as LongArrayAttribute)?.AsUint;
		}
		List<uint> voxelCuboids = ((cuboids == null) ? new List<uint>() : new List<uint>(cuboids));
		Block firstblock = capi.World.Blocks[materials[0]];
		bool num = firstblock.Attributes?.IsTrue("chiselShapeFromCollisionBox") ?? false;
		uint[] originalCuboids = null;
		if (num)
		{
			Cuboidf[] collboxes = firstblock.CollisionBoxes;
			originalCuboids = new uint[collboxes.Length];
			for (int i = 0; i < collboxes.Length; i++)
			{
				Cuboidf box = collboxes[i];
				uint uintbox = BlockEntityMicroBlock.ToUint((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2), 0);
				originalCuboids[i] = uintbox;
			}
		}
		MeshData mesh = BlockEntityMicroBlock.CreateMesh(capi, voxelCuboids, materials, null, null, originalCuboids);
		mesh.Rgba.Fill(byte.MaxValue);
		return capi.Render.UploadMultiTextureMesh(mesh);
	}

	private void Event_LeaveWorld()
	{
		foreach (KeyValuePair<long, CachedModel> cachedModel in cachedModels)
		{
			cachedModel.Value.MeshRef.Dispose();
		}
	}
}
