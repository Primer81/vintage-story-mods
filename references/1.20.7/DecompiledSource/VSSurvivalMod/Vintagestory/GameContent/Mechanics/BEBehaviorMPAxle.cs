using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent.Mechanics;

public class BEBehaviorMPAxle : BEBehaviorMPBase
{
	private Vec3f center = new Vec3f(0.5f, 0.5f, 0.5f);

	private BlockFacing[] orients = new BlockFacing[2];

	private ICoreClientAPI capi;

	private string orientations;

	private AssetLocation axleStandLocWest;

	private AssetLocation axleStandLocEast;

	protected virtual bool AddStands => true;

	public BEBehaviorMPAxle(BlockEntity blockentity)
		: base(blockentity)
	{
	}

	public override void Initialize(ICoreAPI api, JsonObject properties)
	{
		base.Initialize(api, properties);
		axleStandLocWest = AssetLocation.Create("block/wood/mechanics/axle-stand-west", base.Block.Code?.Domain);
		axleStandLocEast = AssetLocation.Create("block/wood/mechanics/axle-stand-east", base.Block.Code?.Domain);
		JsonObject attributes = base.Block.Attributes;
		if (attributes != null && attributes["axleStandLocWest"].Exists)
		{
			axleStandLocWest = base.Block.Attributes["axleStandLocWest"].AsObject<AssetLocation>();
		}
		JsonObject attributes2 = base.Block.Attributes;
		if (attributes2 != null && attributes2["axleStandLocEast"].Exists)
		{
			axleStandLocEast = base.Block.Attributes["axleStandLocEast"].AsObject<AssetLocation>();
		}
		axleStandLocWest.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		axleStandLocEast.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		if (api.Side == EnumAppSide.Client)
		{
			capi = api as ICoreClientAPI;
		}
		orientations = base.Block.Variant["rotation"];
		switch (orientations)
		{
		case "ns":
			AxisSign = new int[3] { 0, 0, -1 };
			orients[0] = BlockFacing.NORTH;
			orients[1] = BlockFacing.SOUTH;
			break;
		case "we":
			AxisSign = new int[3] { -1, 0, 0 };
			orients[0] = BlockFacing.WEST;
			orients[1] = BlockFacing.EAST;
			break;
		case "ud":
			AxisSign = new int[3] { 0, 1, 0 };
			orients[0] = BlockFacing.DOWN;
			orients[1] = BlockFacing.UP;
			break;
		}
	}

	public override float GetResistance()
	{
		return 0.0005f;
	}

	protected virtual MeshData getStandMesh(string orient)
	{
		return ObjectCacheUtil.GetOrCreate(Api, string.Concat(base.Block.Code, "-", orient, "-stand"), delegate
		{
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, (orient == "west") ? axleStandLocWest : axleStandLocEast);
			capi.Tesselator.TesselateShape(base.Block, shape, out var modeldata);
			return modeldata;
		});
	}

	public static bool IsAttachedToBlock(IBlockAccessor blockaccessor, Block block, BlockPos Position)
	{
		string orientations = block.Variant["rotation"];
		if (orientations == "ns" || orientations == "we")
		{
			if (blockaccessor.GetBlockBelow(Position, 1, 1).SideSolid[BlockFacing.UP.Index] || blockaccessor.GetBlockAbove(Position, 1, 1).SideSolid[BlockFacing.DOWN.Index])
			{
				return true;
			}
			BlockFacing frontFacing = ((orientations == "ns") ? BlockFacing.WEST : BlockFacing.NORTH);
			if (!blockaccessor.GetBlockOnSide(Position, frontFacing, 1).SideSolid[frontFacing.Opposite.Index])
			{
				return blockaccessor.GetBlockOnSide(Position, frontFacing.Opposite, 1).SideSolid[frontFacing.Index];
			}
			return true;
		}
		for (int i = 0; i < 4; i++)
		{
			BlockFacing face = BlockFacing.HORIZONTALS[i];
			if (blockaccessor.GetBlockOnSide(Position, face, 1).SideSolid[face.Opposite.Index])
			{
				return true;
			}
		}
		return false;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (AddStands)
		{
			if (RequiresStand(Api.World, Position, orients[0].Normali))
			{
				MeshData mesh2 = getStandMesh("west");
				mesh2 = rotStand(mesh2);
				if (mesh2 != null)
				{
					mesher.AddMeshData(mesh2);
				}
			}
			if (RequiresStand(Api.World, Position, orients[1].Normali))
			{
				MeshData mesh = getStandMesh("east");
				mesh = rotStand(mesh);
				if (mesh != null)
				{
					mesher.AddMeshData(mesh);
				}
			}
		}
		return base.OnTesselation(mesher, tesselator);
	}

	private bool RequiresStand(IWorldAccessor world, BlockPos pos, Vec3i vector)
	{
		try
		{
			if (!(world.BlockAccessor.GetBlockRaw(pos.X + vector.X, pos.InternalY + vector.Y, pos.Z + vector.Z, 1) is BlockMPBase block))
			{
				return true;
			}
			BlockPos sidePos = new BlockPos(pos.X + vector.X, pos.Y + vector.Y, pos.Z + vector.Z, pos.dimension);
			BEBehaviorMPBase bemp = world.BlockAccessor.GetBlockEntity(sidePos)?.GetBehavior<BEBehaviorMPBase>();
			if (bemp == null)
			{
				return true;
			}
			if (!(bemp is BEBehaviorMPAxle bempaxle))
			{
				if (bemp is BEBehaviorMPBrake || bemp is BEBehaviorMPCreativeRotor)
				{
					BlockFacing side = BlockFacing.FromNormal(vector);
					if (side != null && block.HasMechPowerConnectorAt(world, sidePos, side.Opposite))
					{
						return false;
					}
				}
				return true;
			}
			if (bempaxle.orientations == orientations && IsAttachedToBlock(world.BlockAccessor, block, sidePos))
			{
				return false;
			}
			return bempaxle.RequiresStand(world, sidePos, vector);
		}
		catch (Exception e)
		{
			world.Logger.Error("Exception thrown in RequiresStand, will log exception but silently ignore it: at " + pos);
			world.Logger.Error(e);
			return false;
		}
	}

	private MeshData rotStand(MeshData mesh)
	{
		if (orientations == "ns" || orientations == "we")
		{
			mesh = mesh.Clone();
			if (orientations == "ns")
			{
				mesh = mesh.Rotate(center, 0f, -(float)Math.PI / 2f, 0f);
			}
			if (!Api.World.BlockAccessor.GetBlockBelow(Position, 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				if (Api.World.BlockAccessor.GetBlockAbove(Position, 1, 1).SideSolid[BlockFacing.DOWN.Index])
				{
					mesh = mesh.Rotate(center, (float)Math.PI, 0f, 0f);
				}
				else if (orientations == "ns")
				{
					BlockFacing face = BlockFacing.EAST;
					if (Api.World.BlockAccessor.GetBlockOnSide(Position, face, 1).SideSolid[face.Opposite.Index])
					{
						mesh = mesh.Rotate(center, 0f, 0f, (float)Math.PI / 2f);
					}
					else
					{
						face = BlockFacing.WEST;
						if (!Api.World.BlockAccessor.GetBlockOnSide(Position, face, 1).SideSolid[face.Opposite.Index])
						{
							return null;
						}
						mesh = mesh.Rotate(center, 0f, 0f, -(float)Math.PI / 2f);
					}
				}
				else
				{
					BlockFacing face2 = BlockFacing.NORTH;
					if (Api.World.BlockAccessor.GetBlockOnSide(Position, face2, 1).SideSolid[face2.Opposite.Index])
					{
						mesh = mesh.Rotate(center, (float)Math.PI / 2f, 0f, 0f);
					}
					else
					{
						face2 = BlockFacing.SOUTH;
						if (!Api.World.BlockAccessor.GetBlockOnSide(Position, face2, 1).SideSolid[face2.Opposite.Index])
						{
							return null;
						}
						mesh = mesh.Rotate(center, -(float)Math.PI / 2f, 0f, 0f);
					}
				}
			}
			return mesh;
		}
		BlockFacing attachFace = null;
		for (int i = 0; i < 4; i++)
		{
			BlockFacing face3 = BlockFacing.HORIZONTALS[i];
			if (Api.World.BlockAccessor.GetBlockOnSide(Position, face3, 1).SideSolid[face3.Opposite.Index])
			{
				attachFace = face3;
				break;
			}
		}
		if (attachFace != null)
		{
			mesh = mesh.Clone().Rotate(center, 0f, 0f, (float)Math.PI / 2f).Rotate(center, 0f, (float)(attachFace.HorizontalAngleIndex * 90) * ((float)Math.PI / 180f), 0f);
			return mesh;
		}
		return null;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
	{
		base.GetBlockInfo(forPlayer, sb);
		if (Api.World.EntityDebugMode)
		{
			string orientations = base.Block.Variant["orientation"];
			sb.AppendLine(string.Format(Lang.Get("Orientation: {0}", orientations)));
		}
	}
}
