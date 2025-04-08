using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBrake : BlockEntity
{
	private MeshData ownMesh;

	public bool Engaged { get; protected set; }

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>().animUtil;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
	}

	private void OnClientGameTick(float dt)
	{
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		base.OnTesselation(mesher, tessThreadTesselator);
		if (!Engaged)
		{
			if (ownMesh == null)
			{
				ownMesh = GenOpenedMesh(tessThreadTesselator, base.Block.Shape.rotateY);
				if (ownMesh == null)
				{
					return false;
				}
			}
			mesher.AddMeshData(ownMesh);
			return true;
		}
		return false;
	}

	private MeshData GenOpenedMesh(ITesselatorAPI tesselator, float rotY)
	{
		string key = "mechbrakeOpenedMesh";
		Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () => new Dictionary<string, MeshData>());
		if (meshes.TryGetValue(rotY.ToString() ?? "", out var mesh))
		{
			return mesh;
		}
		AssetLocation shapeloc = AssetLocation.Create("shapes/block/wood/mechanics/brake-stand-opened.json", base.Block.Code.Domain);
		Shape shape = Shape.TryGet(Api, shapeloc);
		tesselator.TesselateShape(base.Block, shape, out mesh, new Vec3f(0f, rotY, 0f));
		return meshes[rotY.ToString() ?? ""] = mesh;
	}

	public bool OnInteract(IPlayer byPlayer)
	{
		Engaged = !Engaged;
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/woodswitch.ogg"), Pos, 0.0, byPlayer);
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		Engaged = tree.GetBool("engaged");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("engaged", Engaged);
	}
}
