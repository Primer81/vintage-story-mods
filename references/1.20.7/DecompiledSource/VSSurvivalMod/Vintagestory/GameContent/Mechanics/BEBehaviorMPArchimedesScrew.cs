using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPArchimedesScrew : BEBehaviorMPBase
{
	private ICoreClientAPI capi;

	private float resistance;

	protected virtual bool AddStands => false;

	public BEBehaviorMPArchimedesScrew(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		if (api.Side == EnumAppSide.Client)
		{
			capi = api as ICoreClientAPI;
		}
		AxisSign = new int[3] { 0, 1, 0 };
		resistance = properties["resistance"].AsFloat(0.015f);
	}

	public override float GetResistance()
	{
		return resistance;
	}

	protected virtual MeshData getHullMesh()
	{
		CompositeShape cshape = properties["staticShapePart"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
		if (cshape == null)
		{
			return null;
		}
		cshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		return ObjectCacheUtil.GetOrCreate(Api, "archimedesscrew-mesh-" + cshape.Base.Path + "-" + cshape.rotateX + "-" + cshape.rotateY + "-" + cshape.rotateZ, delegate
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, cshape.Base);
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata);
			modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), cshape.rotateX * ((float)Math.PI / 180f), cshape.rotateY * ((float)Math.PI / 180f), cshape.rotateZ * ((float)Math.PI / 180f));
			return modeldata;
		});
	}

	public bool IsAttachedToBlock()
	{
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing face = BlockFacing.HORIZONTALS[i];
			Block block = Api.World.BlockAccessor.GetBlockOnSide(Position, face);
			if (base.Block != block && block.SideSolid[face.Opposite.Index])
			{
				return true;
			}
		}
		return false;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(getHullMesh());
		return base.OnTesselation(mesher, tesselator);
	}
}
