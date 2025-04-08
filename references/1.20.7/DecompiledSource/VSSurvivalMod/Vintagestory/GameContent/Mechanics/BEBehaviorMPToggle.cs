using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPToggle : BEBehaviorMPBase
{
	protected readonly BlockFacing[] orients = new BlockFacing[2];

	protected readonly BlockPos[] sides = new BlockPos[2];

	private ICoreClientAPI capi;

	private string orientations;

	private AssetLocation toggleStandLoc;

	public BEBehaviorMPToggle(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		toggleStandLoc = AssetLocation.Create("block/wood/mechanics/toggle-stand.json", base.Block.Code?.Domain);
		JsonObject attributes = base.Block.Attributes;
		if (attributes != null && attributes["toggleStandLoc"].Exists)
		{
			toggleStandLoc = base.Block.Attributes?["toggleStandLoc"].AsObject<AssetLocation>();
		}
		toggleStandLoc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		if (api.Side == EnumAppSide.Client)
		{
			capi = api as ICoreClientAPI;
		}
		orientations = base.Block.Variant["orientation"];
		string text = orientations;
		if (!(text == "ns"))
		{
			if (text == "we")
			{
				AxisSign = new int[3] { -1, 0, 0 };
				orients[0] = BlockFacing.WEST;
				orients[1] = BlockFacing.EAST;
				sides[0] = Position.AddCopy(BlockFacing.NORTH);
				sides[1] = Position.AddCopy(BlockFacing.SOUTH);
			}
		}
		else
		{
			AxisSign = new int[3] { 0, 0, -1 };
			orients[0] = BlockFacing.NORTH;
			orients[1] = BlockFacing.SOUTH;
			sides[0] = Position.AddCopy(BlockFacing.WEST);
			sides[1] = Position.AddCopy(BlockFacing.EAST);
		}
	}

	public bool ValidHammerBase(BlockPos pos)
	{
		if (!(sides[0] == pos))
		{
			return sides[1] == pos;
		}
		return true;
	}

	public override float GetResistance()
	{
		bool hasHammer = false;
		if (Api.World.BlockAccessor.GetBlockEntity(sides[0]) is BEHelveHammer { HammerStack: not null })
		{
			hasHammer = true;
		}
		else if (Api.World.BlockAccessor.GetBlockEntity(sides[1]) is BEHelveHammer { HammerStack: not null })
		{
			hasHammer = true;
		}
		float speed = ((network == null) ? 0f : Math.Abs(network.Speed * base.GearedRatio));
		float speedLimiter = 5f * (float)Math.Exp((double)speed * 2.8 - 5.0);
		if (!hasHammer)
		{
			return 0.0005f;
		}
		return 0.125f + speedLimiter;
	}

	public override void JoinNetwork(MechanicalNetwork network)
	{
		base.JoinNetwork(network);
		float speed = ((network == null) ? 0f : (Math.Abs(network.Speed * base.GearedRatio) * 1.6f));
		if (speed > 1f)
		{
			network.Speed /= speed;
			network.clientSpeed /= speed;
		}
	}

	public bool IsAttachedToBlock()
	{
		if (orientations == "ns" || orientations == "we")
		{
			if (!Api.World.BlockAccessor.IsSideSolid(Position.X, Position.Y - 1, Position.Z, BlockFacing.UP))
			{
				return Api.World.BlockAccessor.IsSideSolid(Position.X, Position.Y + 1, Position.Z, BlockFacing.DOWN);
			}
			return true;
		}
		return false;
	}

	private MeshData getStandMesh(string orient)
	{
		return ObjectCacheUtil.GetOrCreate(Api, string.Concat(base.Block.Code, "-", orient, "-stand"), delegate
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, toggleStandLoc);
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata);
			if (orient == "ns")
			{
				modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)Math.PI / 2f, 0f);
			}
			return modeldata;
		});
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		MeshData mesh = getStandMesh(base.Block.Variant["orientation"]);
		mesher.AddMeshData(mesh);
		return base.OnTesselation(mesher, tesselator);
	}
}
